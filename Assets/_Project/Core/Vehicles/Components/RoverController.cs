using UnityEngine;

namespace SpaceSeries.Core.Vehicles
{
    /// <summary>
    /// Kinematic vehicle controller for planetary rovers.
    /// Simulates 4-wheel electric drive, authentic Apollo LRV 4-wheel counter-steering,
    /// and low lunar gravity suspension dynamics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RoverController : MonoBehaviour, IVehicle
    {
        [Header("Configuration")]
        [Tooltip("Rover specifications asset (speeds, motor torque, steering limits, gravity).")]
        [SerializeField] private RoverConfigSO _config;

        [Header("Wheel Pairs (Collider & Visual Mesh)")]
        [SerializeField] private RoverWheelPair _frontLeft;
        [SerializeField] private RoverWheelPair _frontRight;
        [SerializeField] private RoverWheelPair _rearLeft;
        [SerializeField] private RoverWheelPair _rearRight;

        [Header("Telemetry Broadcast")]
        [Tooltip("Optional event channel to broadcast speedometer and status data.")]
        [SerializeField] private VehicleEventChannelSO _eventChannel;

        private Rigidbody _rigidbody;
        private IVehicleDriver _currentDriver;
        private Vector2 _driveInput;
        private bool _isBraking;
        private bool _isHandbrakeEngaged;
        private float _currentSteerAngle;

        // IVehicle Properties
        public bool IsOccupied => _currentDriver != null;
        public float CurrentSpeedMs => _rigidbody != null ? Vector3.Dot(_rigidbody.linearVelocity, transform.forward) : 0f;
        public float CurrentSpeedKmh => Mathf.Abs(CurrentSpeedMs * 3.6f);
        public Rigidbody VehicleRigidbody => _rigidbody;
        public RoverConfigSO Config => _config;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            if (_config != null)
            {
                // Lower center of mass stabilizes low-gravity traversal on crater slopes
                _rigidbody.centerOfMass = _config.CenterOfMassOffset;
            }

            // Disable standard Earth gravity; custom planetary gravity is integrated in FixedUpdate
            _rigidbody.useGravity = false;
            _frontLeft = _frontLeft ?? new RoverWheelPair();
            _frontRight = _frontRight ?? new RoverWheelPair();
            _rearLeft = _rearLeft ?? new RoverWheelPair();
            _rearRight = _rearRight ?? new RoverWheelPair();
            if (_config == null || _frontLeft.PhysicalCollider == null || _frontRight.PhysicalCollider == null ||
                _rearLeft.PhysicalCollider == null || _rearRight.PhysicalCollider == null)
            {
                Debug.LogError("Rover requires a configuration and four WheelColliders.", this);
                _rigidbody.isKinematic = true;
                enabled = false;
                return;
            }
            ApplyHandbrake();
        }

        private void OnDisable()
        {
            _currentDriver = null;
            _driveInput = Vector2.zero;
            _isBraking = false;
            _isHandbrakeEngaged = true;
            if (_frontLeft != null && _frontRight != null && _rearLeft != null && _rearRight != null)
                ApplyHandbrake();
        }

        private void Update()
        {
            // Synchronize visual wheel meshes with physical WheelColliders
            _frontLeft.UpdatePose();
            _frontRight.UpdatePose();
            _rearLeft.UpdatePose();
            _rearRight.UpdatePose();

            if (_eventChannel != null && IsOccupied)
            {
                _eventChannel.RaiseSpeedChanged(CurrentSpeedKmh);
            }
        }

        private void FixedUpdate()
        {
            ApplyPlanetaryGravity();
            ApplySteering();
            ApplyMotorAndBraking();
        }

        private void ApplyPlanetaryGravity()
        {
            if (_config == null || _rigidbody == null)
                return;

            // Apply Moon downward gravitational acceleration (1.62 m/s^2)
            Vector3 gravityForce = Vector3.down * _config.PlanetaryGravity;
            _rigidbody.AddForce(gravityForce, ForceMode.Acceleration);

            // Add dynamic downforce at higher speeds to prevent floatiness
            if (_config.DownforceMultiplier > 0f)
            {
                float forwardSpeed = Mathf.Abs(CurrentSpeedMs);
                Vector3 downforce = -transform.up * (forwardSpeed * _config.DownforceMultiplier * 10f);
                _rigidbody.AddForce(downforce, ForceMode.Force);
            }
        }

        private void ApplySteering()
        {
            if (_config == null)
                return;

            float targetSteerAngle = _driveInput.x * _config.MaxSteerAngle;
            _currentSteerAngle = Mathf.MoveTowards(
                _currentSteerAngle,
                targetSteerAngle,
                _config.SteeringSpeed * _config.MaxSteerAngle * Time.fixedDeltaTime);

            // Front wheels turn in direction of input
            if (_frontLeft.PhysicalCollider != null) _frontLeft.PhysicalCollider.steerAngle = _currentSteerAngle;
            if (_frontRight.PhysicalCollider != null) _frontRight.PhysicalCollider.steerAngle = _currentSteerAngle;

            // Authentic Apollo 15 LRV 4-Wheel Steering: rear wheels counter-steer for tight turns
            if (_config.EnableFourWheelSteering)
            {
                if (_rearLeft.PhysicalCollider != null) _rearLeft.PhysicalCollider.steerAngle = -_currentSteerAngle;
                if (_rearRight.PhysicalCollider != null) _rearRight.PhysicalCollider.steerAngle = -_currentSteerAngle;
            }
            else
            {
                if (_rearLeft.PhysicalCollider != null) _rearLeft.PhysicalCollider.steerAngle = 0f;
                if (_rearRight.PhysicalCollider != null) _rearRight.PhysicalCollider.steerAngle = 0f;
            }
        }

        private void ApplyMotorAndBraking()
        {
            if (_config == null)
                return;

            float throttleInput = _driveInput.y;
            float currentSpeed = CurrentSpeedMs;

            // Determine if input is driving or braking
            bool wantsReverse = throttleInput < -0.05f;
            bool isMovingForward = currentSpeed > 0.5f;

            if (_isHandbrakeEngaged || _isBraking || (wantsReverse && isMovingForward))
            {
                // Apply active braking torque
                ApplyBrakeTorqueToAllWheels(_config.BrakeTorque);
                ApplyMotorTorqueToAllWheels(0f);
            }
            else if (Mathf.Abs(throttleInput) > 0.05f)
            {
                // Speed clamping
                bool atMaxForward = throttleInput > 0f && currentSpeed >= _config.MaxForwardSpeed;
                bool atMaxReverse = throttleInput < 0f && currentSpeed <= -_config.MaxReverseSpeed;

                if (atMaxForward || atMaxReverse)
                {
                    ApplyMotorTorqueToAllWheels(0f);
                    ApplyBrakeTorqueToAllWheels(_config.IdleDecelerationTorque);
                }
                else
                {
                    // Distribute motor torque to all 4 electric wheel motors (4WD)
                    float appliedTorque = throttleInput * (_config.MotorTorque / 4f);
                    ApplyMotorTorqueToAllWheels(appliedTorque);
                    ApplyBrakeTorqueToAllWheels(0f);
                }
            }
            else
            {
                // Throttle released: apply natural electric motor rolling resistance
                ApplyMotorTorqueToAllWheels(0f);
                ApplyBrakeTorqueToAllWheels(_config.IdleDecelerationTorque);
            }
        }

        private void ApplyMotorTorqueToAllWheels(float torque)
        {
            if (_frontLeft.PhysicalCollider != null) _frontLeft.PhysicalCollider.motorTorque = torque;
            if (_frontRight.PhysicalCollider != null) _frontRight.PhysicalCollider.motorTorque = torque;
            if (_rearLeft.PhysicalCollider != null) _rearLeft.PhysicalCollider.motorTorque = torque;
            if (_rearRight.PhysicalCollider != null) _rearRight.PhysicalCollider.motorTorque = torque;
        }

        private void ApplyBrakeTorqueToAllWheels(float torque)
        {
            if (_frontLeft.PhysicalCollider != null) _frontLeft.PhysicalCollider.brakeTorque = torque;
            if (_frontRight.PhysicalCollider != null) _frontRight.PhysicalCollider.brakeTorque = torque;
            if (_rearLeft.PhysicalCollider != null) _rearLeft.PhysicalCollider.brakeTorque = torque;
            if (_rearRight.PhysicalCollider != null) _rearRight.PhysicalCollider.brakeTorque = torque;
        }

        public bool Board(IVehicleDriver driver)
        {
            if (IsOccupied || driver == null)
                return false;

            _currentDriver = driver;
            _driveInput = Vector2.zero;
            _isBraking = false;
            _isHandbrakeEngaged = false;
            ReleaseHandbrake();

            _eventChannel?.RaiseVehicleBoarded(this);
            return true;
        }

        public void Dismount()
        {
            _currentDriver = null;
            _driveInput = Vector2.zero;
            ApplyHandbrake();

            _eventChannel?.RaiseVehicleDismounted(this);
        }

        public void SetDriveInput(Vector2 moveInput, bool isBraking)
        {
            _driveInput = moveInput;
            _isBraking = isBraking;
        }

        public void ApplyHandbrake()
        {
            _isHandbrakeEngaged = true;
            ApplyBrakeTorqueToAllWheels(_config != null ? _config.BrakeTorque : 1000f);
            ApplyMotorTorqueToAllWheels(0f);
        }

        public void ReleaseHandbrake()
        {
            _isHandbrakeEngaged = false;
            ApplyBrakeTorqueToAllWheels(0f);
        }
    }
}

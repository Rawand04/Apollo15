using UnityEngine;

namespace SpaceSeries.Core.Vehicles
{
    /// <summary>
    /// Configuration data asset for planetary rovers (e.g., Apollo 15 Boeing LRV or future Mars rovers).
    /// Stores physical drive parameters, steering capabilities, and low-gravity stabilization settings.
    /// </summary>
    [CreateAssetMenu(fileName = "RoverConfig_LRV", menuName = "SpaceSeries/Vehicles/Rover Configuration")]
    public class RoverConfigSO : ScriptableObject
    {
        [Header("Vehicle Identity")]
        [Tooltip("Display name of the vehicle (e.g. 'Lunar Roving Vehicle (LRV)').")]
        [SerializeField] private string _vehicleName = "Boeing Lunar Roving Vehicle";

        [Header("Speed & Torque")]
        [Tooltip("Maximum forward speed in m/s (Apollo LRV top cruise speed was ~3.6 m/s / 13 km/h).")]
        [Range(1f, 25f)]
        [SerializeField] private float _maxForwardSpeed = 3.8f;

        [Tooltip("Maximum reverse speed in m/s.")]
        [Range(1f, 10f)]
        [SerializeField] private float _maxReverseSpeed = 2.0f;

        [Tooltip("Motor torque applied to powered wheels (Nm).")]
        [Range(50f, 2000f)]
        [SerializeField] private float _motorTorque = 400f;

        [Tooltip("Brake torque applied when holding brake or parking (Nm).")]
        [Range(100f, 4000f)]
        [SerializeField] private float _brakeTorque = 800f;

        [Tooltip("Natural rolling drag torque applied when throttle is released.")]
        [Range(10f, 500f)]
        [SerializeField] private float _idleDecelerationTorque = 60f;

        [Header("Steering Dynamics")]
        [Tooltip("Maximum front wheel turn angle in degrees.")]
        [Range(10f, 45f)]
        [SerializeField] private float _maxSteerAngle = 26f;

        [Tooltip("Enable 4-wheel independent counter-steering (authentic Apollo LRV feature providing a tight 3.1m turning radius).")]
        [SerializeField] private bool _enableFourWheelSteering = true;

        [Tooltip("Steering response smoothing speed.")]
        [Range(1f, 20f)]
        [SerializeField] private float _steeringSpeed = 8f;

        [Header("Planetary Physics & Stability")]
        [Tooltip("Center of mass vertical offset. A lower center of mass prevents flipping on crater slopes in 1/6th gravity.")]
        [SerializeField] private Vector3 _centerOfMassOffset = new Vector3(0f, -0.35f, 0f);

        [Tooltip("Downward acceleration in m/s^2. Realistic Moon gravity = 1.62 m/s^2.")]
        [SerializeField] private float _planetaryGravity = 1.62f;

        [Tooltip("Extra stabilization downforce multiplier applied at speed to keep tires planted on regolith.")]
        [Range(0f, 5f)]
        [SerializeField] private float _downforceMultiplier = 1.2f;

        // Public Properties
        public string VehicleName => _vehicleName;
        public float MaxForwardSpeed => _maxForwardSpeed;
        public float MaxReverseSpeed => _maxReverseSpeed;
        public float MotorTorque => _motorTorque;
        public float BrakeTorque => _brakeTorque;
        public float IdleDecelerationTorque => _idleDecelerationTorque;
        public float MaxSteerAngle => _maxSteerAngle;
        public bool EnableFourWheelSteering => _enableFourWheelSteering;
        public float SteeringSpeed => _steeringSpeed;
        public Vector3 CenterOfMassOffset => _centerOfMassOffset;
        public float PlanetaryGravity => _planetaryGravity;
        public float DownforceMultiplier => _downforceMultiplier;
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Kinematic physics motor handling third-person camera-relative locomotion,
    /// smooth acceleration/deceleration, lunar low-gravity integration, and slope projection.
    /// </summary>
    public class AstronautMotor : MonoBehaviour, IAstronautMotor
    {
        private AstronautMovementSettingsSO _settings;
        private CharacterController _characterController;
        private IGroundDetector _groundDetector;
        private Transform _astronautTransform;
        private Transform _cameraTransform;

        // Locomotion state
        private float _currentPlanarSpeed;
        private float _verticalVelocity;
        private float _rotationSmoothVelocity;
        private Vector3 _calculatedVelocity;

        // Speed modifiers
        private readonly List<ISpeedModifier> _speedModifiers = new List<ISpeedModifier>(4);

        // IAstronautMotor Properties
        public Vector3 Velocity => _calculatedVelocity;
        public float CurrentPlanarSpeed => _currentPlanarSpeed;
        public float VerticalVelocity => _verticalVelocity;

        public void Initialize(
            AstronautMovementSettingsSO settings,
            CharacterController characterController,
            IGroundDetector groundDetector,
            Transform astronautTransform,
            Transform cameraTransform)
        {
            _settings = settings;
            _characterController = characterController;
            _groundDetector = groundDetector;
            _astronautTransform = astronautTransform;
            _cameraTransform = cameraTransform != null ? cameraTransform : Camera.main != null ? Camera.main.transform : null;
        }

        public void Tick(Vector2 inputDirection, bool isSprintRequested, float deltaTime)
        {
            if (_characterController == null || _settings == null || _astronautTransform == null)
                return;

            // 1. Evaluate surface ground state
            _groundDetector?.CheckGround(_astronautTransform.position);
            bool isGrounded = _groundDetector != null ? _groundDetector.IsGrounded : _characterController.isGrounded;

            // 2. Resolve target speed with acceleration / deceleration
            float targetSpeed = CalculateTargetSpeed(inputDirection, isSprintRequested);
            float rate = targetSpeed > _currentPlanarSpeed ? _settings.AccelerationRate : _settings.DecelerationRate;
            _currentPlanarSpeed = Mathf.MoveTowards(_currentPlanarSpeed, targetSpeed, rate * deltaTime);

            // 3. Camera-relative movement direction & character rotation
            Vector3 worldMoveDirection = Vector3.zero;

            if (inputDirection.sqrMagnitude > 0.001f)
            {
                worldMoveDirection = CalculateCameraRelativeDirection(inputDirection);

                // Smooth rotation towards movement trajectory
                float targetAngle = Mathf.Atan2(worldMoveDirection.x, worldMoveDirection.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(
                    _astronautTransform.eulerAngles.y,
                    targetAngle,
                    ref _rotationSmoothVelocity,
                    _settings.RotationSmoothTime);

                _astronautTransform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            }

            // 4. Slope handling
            Vector3 planarMotion = worldMoveDirection * _currentPlanarSpeed;
            if (isGrounded && _groundDetector != null)
            {
                if (_groundDetector.SurfaceSlopeAngle <= _settings.SlopeLimit)
                {
                    // Project along ground slope
                    planarMotion = Vector3.ProjectOnPlane(planarMotion, _groundDetector.GroundNormal);
                }
                else
                {
                    // Slide down steep slope
                    Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, _groundDetector.GroundNormal);
                    planarMotion += slideDirection * (_settings.LunarGravity * 2f);
                }
            }

            // 5. Lunar gravity & vertical physics
            if (isGrounded)
            {
                // Snap downward lightly to stick to rough crater terrain in low gravity
                _verticalVelocity = -_settings.GroundedSnapForce;
            }
            else
            {
                // Accelerate downward according to Moon's 1.62 m/s^2 gravity
                _verticalVelocity -= _settings.LunarGravity * deltaTime;
                _verticalVelocity = Mathf.Max(_verticalVelocity, -_settings.TerminalVelocity);
            }

            // 6. Execute CharacterController translation
            Vector3 finalMotion = planarMotion + Vector3.up * _verticalVelocity;
            _characterController.Move(finalMotion * deltaTime);

            _calculatedVelocity = _characterController.velocity;
        }

        private float CalculateTargetSpeed(Vector2 inputDirection, bool isSprintRequested)
        {
            if (inputDirection.sqrMagnitude < 0.001f)
                return 0f;

            float baseSpeed = isSprintRequested ? _settings.SprintSpeed : _settings.WalkSpeed;

            // Apply external speed modifiers (ALSEP carrying, life support, terrain)
            float compoundMultiplier = 1f;
            float maxClamp = float.MaxValue;

            for (int i = 0; i < _speedModifiers.Count; i++)
            {
                compoundMultiplier *= _speedModifiers[i].SpeedMultiplier;
                if (_speedModifiers[i].MaxSpeedClamp < maxClamp)
                {
                    maxClamp = _speedModifiers[i].MaxSpeedClamp;
                }
            }

            float finalSpeed = baseSpeed * compoundMultiplier;
            return Mathf.Min(finalSpeed, maxClamp);
        }

        private Vector3 CalculateCameraRelativeDirection(Vector2 input)
        {
            if (_cameraTransform == null)
            {
                if (Camera.main != null)
                    _cameraTransform = Camera.main.transform;
                else
                    return new Vector3(input.x, 0f, input.y).normalized;
            }

            Vector3 camForward = _cameraTransform.forward;
            Vector3 camRight = _cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            return (camForward * input.y + camRight * input.x).normalized;
        }

        public void AddSpeedModifier(ISpeedModifier modifier)
        {
            if (modifier != null && !_speedModifiers.Contains(modifier))
            {
                _speedModifiers.Add(modifier);
                _speedModifiers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
        }

        public void RemoveSpeedModifier(ISpeedModifier modifier)
        {
            if (modifier != null)
            {
                _speedModifiers.Remove(modifier);
            }
        }

        public void Teleport(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (_characterController != null)
            {
                _characterController.enabled = false;
                _astronautTransform.SetPositionAndRotation(worldPosition, worldRotation);
                _characterController.enabled = true;
            }
            else if (_astronautTransform != null)
            {
                _astronautTransform.SetPositionAndRotation(worldPosition, worldRotation);
            }

            ResetVelocity();
        }

        public void ResetVelocity()
        {
            _currentPlanarSpeed = 0f;
            _verticalVelocity = 0f;
            _calculatedVelocity = Vector3.zero;
        }
    }
}

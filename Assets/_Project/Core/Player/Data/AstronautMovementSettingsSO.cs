using UnityEngine;

namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Configuration data asset for astronaut locomotion, planetary gravity, and animation blending.
    /// ScriptableObject pattern allows tweaking physical attributes per planetary body (e.g., Moon vs Mars).
    /// </summary>
    [CreateAssetMenu(fileName = "AstronautMovementSettings_Moon", menuName = "SpaceSeries/Player/Movement Settings")]
    public class AstronautMovementSettingsSO : ScriptableObject
    {
        [Header("Locomotion Speeds (m/s)")]
        [Tooltip("Standard walking speed in the pressurized A7LB space suit.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _walkSpeed = 1.8f;

        [Tooltip("Sprint speed (lunar loping/trotting gait).")]
        [Range(1.5f, 8f)]
        [SerializeField] private float _sprintSpeed = 3.8f;

        [Header("Inertia & Acceleration")]
        [Tooltip("Acceleration rate in m/s^2. Heavy suit mass creates realistic momentum.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _accelerationRate = 3.5f;

        [Tooltip("Deceleration rate in m/s^2 when input is released.")]
        [Range(0.5f, 25f)]
        [SerializeField] private float _decelerationRate = 5.0f;

        [Tooltip("Time in seconds to smoothly rotate towards movement direction.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _rotationSmoothTime = 0.12f;

        [Header("Lunar Gravity & Vertical Physics")]
        [Tooltip("Planetary gravity acceleration in m/s^2. Realistic Moon gravity = 1.62 m/s^2.")]
        [SerializeField] private float _lunarGravity = 1.62f;

        [Tooltip("Constant downward velocity applied while grounded to stick to crater rims and slopes.")]
        [SerializeField] private float _groundedSnapForce = 1.5f;

        [Tooltip("Maximum falling velocity in m/s.")]
        [SerializeField] private float _terminalVelocity = 30.0f;

        [Header("Ground Detection")]
        [Tooltip("Radius of the ground detection spherecast.")]
        [SerializeField] private float _groundCheckRadius = 0.28f;

        [Tooltip("Vertical offset from the transform pivot for the ground check origin.")]
        [SerializeField] private float _groundCheckOffset = 0.15f;

        [Tooltip("Layers recognized as walkable planetary surface.")]
        [SerializeField] private LayerMask _groundLayers = ~0;

        [Tooltip("Maximum traversable slope angle in degrees.")]
        [Range(10f, 60f)]
        [SerializeField] private float _slopeLimit = 45f;

        [Header("Animation Blending")]
        [Tooltip("Damping time in seconds for Animator parameter smoothing.")]
        [Range(0.01f, 0.3f)]
        [SerializeField] private float _animatorDampTime = 0.1f;

        // Public Read-Only Properties
        public float WalkSpeed => _walkSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float AccelerationRate => _accelerationRate;
        public float DecelerationRate => _decelerationRate;
        public float RotationSmoothTime => _rotationSmoothTime;
        public float LunarGravity => _lunarGravity;
        public float GroundedSnapForce => _groundedSnapForce;
        public float TerminalVelocity => _terminalVelocity;
        public float GroundCheckRadius => _groundCheckRadius;
        public float GroundCheckOffset => _groundCheckOffset;
        public LayerMask GroundLayers => _groundLayers;
        public float SlopeLimit => _slopeLimit;
        public float AnimatorDampTime => _animatorDampTime;
    }
}

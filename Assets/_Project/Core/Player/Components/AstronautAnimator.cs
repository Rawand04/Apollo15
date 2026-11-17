using UnityEngine;

namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Bridges locomotion states to the Unity Animator using pre-hashed parameters
    /// and dampening to avoid sudden animation pops.
    /// </summary>
    public class AstronautAnimator : MonoBehaviour, IAstronautAnimator
    {
        [Header("Animator Parameter Names")]
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _normalizedSpeedParam = "NormalizedSpeed";
        [SerializeField] private string _isGroundedParam = "IsGrounded";
        [SerializeField] private string _isSprintingParam = "IsSprinting";
        [SerializeField] private string _isMovingParam = "IsMoving";
        [SerializeField] private string _verticalVelocityParam = "VerticalVelocity";
        [SerializeField] private string _waveParam = "Wave";
        [SerializeField] private string _pointParam = "Point";
        [SerializeField] private string _pickUpParam = "PickUp";

        private Animator _animator;
        private AstronautMovementSettingsSO _settings;

        // Pre-cached parameter hashes for zero per-frame string allocation
        private int _speedHash;
        private int _normalizedSpeedHash;
        private int _isGroundedHash;
        private int _isSprintingHash;
        private int _isMovingHash;
        private int _verticalVelocityHash;
        private int _waveHash;
        private int _pointHash;
        private int _pickUpHash;

        private bool _isInitialized;

        private void Awake()
        {
            CacheHashes();
        }

        public void Initialize(Animator animator, AstronautMovementSettingsSO settings)
        {
            _animator = animator;
            _settings = settings;
            CacheHashes();
            _isInitialized = _animator != null;
        }

        public void SetAnimator(Animator animator)
        {
            _animator = animator;
            _isInitialized = _animator != null;
        }

        private void CacheHashes()
        {
            _speedHash = Animator.StringToHash(_speedParam);
            _normalizedSpeedHash = Animator.StringToHash(_normalizedSpeedParam);
            _isGroundedHash = Animator.StringToHash(_isGroundedParam);
            _isSprintingHash = Animator.StringToHash(_isSprintingParam);
            _isMovingHash = Animator.StringToHash(_isMovingParam);
            _verticalVelocityHash = Animator.StringToHash(_verticalVelocityParam);
            _waveHash = Animator.StringToHash(_waveParam);
            _pointHash = Animator.StringToHash(_pointParam);
            _pickUpHash = Animator.StringToHash(_pickUpParam);
        }

        public void UpdateLocomotion(
            float currentPlanarSpeed,
            float targetSpeed,
            bool isSprinting,
            bool isGrounded,
            float verticalVelocity,
            float deltaTime)
        {
            if (!_isInitialized || _animator == null || !_animator.isActiveAndEnabled)
                return;

            float dampTime = _settings != null ? _settings.AnimatorDampTime : 0.1f;

            // 1. Raw planar speed in m/s
            _animator.SetFloat(_speedHash, currentPlanarSpeed, dampTime, deltaTime);

            // 2. Normalized speed: 0.0 = Idle, 1.0 = Walk, 2.0 = Sprint
            float normalizedSpeed = 0f;
            if (_settings != null && currentPlanarSpeed > 0.05f)
            {
                if (currentPlanarSpeed <= _settings.WalkSpeed)
                {
                    normalizedSpeed = Mathf.InverseLerp(0f, _settings.WalkSpeed, currentPlanarSpeed);
                }
                else
                {
                    normalizedSpeed = 1f + Mathf.InverseLerp(_settings.WalkSpeed, _settings.SprintSpeed, currentPlanarSpeed);
                }
            }
            _animator.SetFloat(_normalizedSpeedHash, normalizedSpeed, dampTime, deltaTime);

            // 3. Discrete booleans
            _animator.SetBool(_isGroundedHash, isGrounded);
            _animator.SetBool(_isSprintingHash, isSprinting && currentPlanarSpeed > (_settings != null ? _settings.WalkSpeed * 1.1f : 2.0f));
            _animator.SetBool(_isMovingHash, currentPlanarSpeed > 0.05f);

            // 4. Vertical velocity for airborne / falling blend
            _animator.SetFloat(_verticalVelocityHash, verticalVelocity, dampTime, deltaTime);
        }

        public void SetTrigger(int parameterHash)
        {
            if (_isInitialized && _animator != null)
            {
                _animator.SetTrigger(parameterHash);
            }
        }

        public void TriggerWave()
        {
            SetTrigger(_waveHash);
        }

        public void TriggerPoint()
        {
            SetTrigger(_pointHash);
        }

        public void TriggerPickUp()
        {
            SetTrigger(_pickUpHash);
        }
    }
}

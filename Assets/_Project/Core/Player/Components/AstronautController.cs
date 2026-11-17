using UnityEngine;

namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Master coordinator for the third-person astronaut.
    /// Follows SOLID principles: orchestrates input, motor, ground detector, and animator
    /// without tightly coupling their underlying implementations.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class AstronautController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Planetary movement settings asset defining speeds, gravity, and inertia.")]
        [SerializeField] private AstronautMovementSettingsSO _settings;

        [Header("Scene References")]
        [Tooltip("Reference to the main virtual or third-person camera. If null, Camera.main is used.")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Optional Animator reference. If null, searched on this GameObject or children.")]
        [SerializeField] private Animator _animator;

        // Subsystem interfaces (Dependency Inversion Principle)
        private IPlayerInputProvider _inputProvider;
        private IGroundDetector _groundDetector;
        private IAstronautMotor _motor;
        private IAstronautAnimator _astronautAnimator;
        private CharacterController _characterController;

        private bool _isLocomotionActive = true;

        // Public Telemetry & State Properties
        public bool IsLocomotionActive => _isLocomotionActive;
        public float CurrentPlanarSpeed => _motor != null ? _motor.CurrentPlanarSpeed : 0f;
        public Vector3 Velocity => _motor != null ? _motor.Velocity : Vector3.zero;
        public bool IsGrounded => _groundDetector != null ? _groundDetector.IsGrounded : (_characterController != null && _characterController.isGrounded);
        public AstronautMovementSettingsSO Settings => _settings;

        private void Awake()
        {
            ResolveDependencies();
            InitializeSubsystems();
        }

        private void Start()
        {
            // Auto-resolve main camera if not set in inspector
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (!_isLocomotionActive)
                return;

            float deltaTime = Time.deltaTime;

            // 1. Fetch input via interface
            Vector2 moveInput = _inputProvider != null ? _inputProvider.MoveInput : Vector2.zero;
            bool isSprint = _inputProvider != null && _inputProvider.IsSprintHeld;

            // 2. Tick motor physics
            if (_motor != null)
            {
                _motor.Tick(moveInput, isSprint, deltaTime);
            }

            // 3. Drive animation blend trees
            if (_astronautAnimator != null && _motor != null)
            {
                float targetSpeed = isSprint ? (_settings != null ? _settings.SprintSpeed : 3.8f) : (_settings != null ? _settings.WalkSpeed : 1.8f);
                if (moveInput.sqrMagnitude < 0.001f) targetSpeed = 0f;

                _astronautAnimator.UpdateLocomotion(
                    _motor.CurrentPlanarSpeed,
                    targetSpeed,
                    isSprint,
                    IsGrounded,
                    _motor.VerticalVelocity,
                    deltaTime);
            }
        }

        /// <summary>
        /// Locates and binds subsystem components on this GameObject or adds defaults.
        /// </summary>
        private void ResolveDependencies()
        {
            _characterController = GetComponent<CharacterController>();

            // Input Provider
            _inputProvider = GetComponent<IPlayerInputProvider>();
            if (_inputProvider == null)
            {
                _inputProvider = gameObject.AddComponent<PlayerInputReader>();
            }

            // Ground Detector
            _groundDetector = GetComponent<IGroundDetector>();
            if (_groundDetector == null)
            {
                _groundDetector = gameObject.AddComponent<AstronautGroundDetector>();
            }

            // Motor
            _motor = GetComponent<IAstronautMotor>();
            if (_motor == null)
            {
                _motor = gameObject.AddComponent<AstronautMotor>();
            }

            // Animator
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            _astronautAnimator = GetComponent<IAstronautAnimator>();
            if (_astronautAnimator == null && _animator != null)
            {
                _astronautAnimator = gameObject.AddComponent<AstronautAnimator>();
            }
        }

        /// <summary>
        /// Passes settings and cross-references into subsystems.
        /// </summary>
        private void InitializeSubsystems()
        {
            if (_groundDetector is AstronautGroundDetector groundDetectorComponent && _settings != null)
            {
                groundDetectorComponent.Initialize(_settings);
            }

            if (_motor != null)
            {
                _motor.Initialize(_settings, _characterController, _groundDetector, transform, _cameraTransform);
            }

            if (_astronautAnimator != null && _animator != null)
            {
                _astronautAnimator.Initialize(_animator, _settings);
            }
        }

        /// <summary>
        /// Enables or pauses locomotion (e.g. while boarding the rover, deploying ALSEP, or in menus).
        /// </summary>
        public void SetLocomotionActive(bool active)
        {
            _isLocomotionActive = active;

            if (!active)
            {
                _inputProvider?.DisableInput();
                _motor?.ResetVelocity();
                _astronautAnimator?.UpdateLocomotion(0f, 0f, false, true, 0f, Time.deltaTime);
            }
            else
            {
                _inputProvider?.EnableInput();
            }
        }

        /// <summary>
        /// Registers a dynamic speed modifier (e.g. carrying an ALSEP experiment, suit fatigue).
        /// </summary>
        public void AddSpeedModifier(ISpeedModifier modifier)
        {
            _motor?.AddSpeedModifier(modifier);
        }

        /// <summary>
        /// Removes an active speed modifier.
        /// </summary>
        public void RemoveSpeedModifier(ISpeedModifier modifier)
        {
            _motor?.RemoveSpeedModifier(modifier);
        }

        /// <summary>
        /// Teleports astronaut without physics velocity glitches.
        /// </summary>
        public void Teleport(Vector3 worldPosition, Quaternion worldRotation)
        {
            _motor?.Teleport(worldPosition, worldRotation);
        }

        /// <summary>
        /// Plays the wave greeting gesture animation.
        /// </summary>
        public void TriggerWave()
        {
            _astronautAnimator?.TriggerWave();
        }

        /// <summary>
        /// Plays the pointing gesture animation (useful when pointing at lunar landmarks).
        /// </summary>
        public void TriggerPoint()
        {
            _astronautAnimator?.TriggerPoint();
        }

        /// <summary>
        /// Plays the pickup / interaction gesture animation.
        /// </summary>
        public void TriggerPickUp()
        {
            _astronautAnimator?.TriggerPickUp();
        }

        /// <summary>
        /// Dynamically swaps or updates movement settings (e.g. entering a pressurized habitat or switching gravity).
        /// </summary>
        public void SetSettings(AstronautMovementSettingsSO newSettings)
        {
            _settings = newSettings;
            InitializeSubsystems();
        }
    }
}

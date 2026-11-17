using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Listens for gesture input shortcuts (Wave, Point) to trigger character animations.
    /// Demonstrates how gesture actions plug directly into the AstronautController.
    /// </summary>
    public class AstronautGestureInput : MonoBehaviour
    {
        [Header("Controller Reference")]
        [SerializeField] private AstronautController _astronautController;

        [Header("Input System Actions (Optional)")]
        [Tooltip("Optional Input Action for Wave gesture.")]
        [SerializeField] private InputActionReference _waveAction;

        [Tooltip("Optional Input Action for Point gesture.")]
        [SerializeField] private InputActionReference _pointAction;

        [Header("Keyboard Fallback Keys")]
        [Tooltip("Fallback key to trigger Wave (default G).")]
        [SerializeField] private Key _waveKey = Key.G;

        [Tooltip("Fallback key to trigger Point (default T).")]
        [SerializeField] private Key _pointKey = Key.T;

        private void Awake()
        {
            if (_astronautController == null)
            {
                _astronautController = GetComponent<AstronautController>();
            }
        }

        private void OnEnable()
        {
            if (_waveAction != null && _waveAction.action != null)
            {
                _waveAction.action.performed += HandleWavePerformed;
                if (!_waveAction.action.enabled)
                    _waveAction.action.Enable();
            }

            if (_pointAction != null && _pointAction.action != null)
            {
                _pointAction.action.performed += HandlePointPerformed;
                if (!_pointAction.action.enabled)
                    _pointAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (_waveAction != null && _waveAction.action != null)
            {
                _waveAction.action.performed -= HandleWavePerformed;
            }

            if (_pointAction != null && _pointAction.action != null)
            {
                _pointAction.action.performed -= HandlePointPerformed;
            }
        }

        private void Update()
        {
            if (_astronautController == null || !_astronautController.IsLocomotionActive)
                return;

            // Keyboard fallback if actions are not explicitly bound
            if (Keyboard.current != null)
            {
                if (_waveAction == null && Keyboard.current[_waveKey].wasPressedThisFrame)
                {
                    _astronautController.TriggerWave();
                }

                if (_pointAction == null && Keyboard.current[_pointKey].wasPressedThisFrame)
                {
                    _astronautController.TriggerPoint();
                }
            }
        }

        private void HandleWavePerformed(InputAction.CallbackContext context)
        {
            if (_astronautController != null && _astronautController.IsLocomotionActive)
            {
                _astronautController.TriggerWave();
            }
        }

        private void HandlePointPerformed(InputAction.CallbackContext context)
        {
            if (_astronautController != null && _astronautController.IsLocomotionActive)
            {
                _astronautController.TriggerPoint();
            }
        }
    }
}

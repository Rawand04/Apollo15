using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Reads and processes player locomotion inputs using the Unity Input System.
    /// Implements IPlayerInputProvider to allow clean decoupling and testing.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour, IPlayerInputProvider
    {
        [Header("Input Action References")]
        [Tooltip("Action reference for 2D movement (e.g., Player/Move).")]
        [SerializeField] private InputActionReference _moveAction;

        [Tooltip("Action reference for sprint (e.g., Player/Sprint).")]
        [SerializeField] private InputActionReference _sprintAction;

        [Header("Configuration")]
        [Tooltip("Deadzone below which input is treated as zero.")]
        [SerializeField] private float _inputDeadzone = 0.05f;

        private Vector2 _moveInput;
        private bool _isSprintHeld;
        private bool _isInputEnabled = true;

        // IPlayerInputProvider Properties
        public Vector2 MoveInput => _isInputEnabled ? _moveInput : Vector2.zero;
        public bool IsSprintHeld => _isInputEnabled && _isSprintHeld;
        public bool HasMoveInput => _isInputEnabled && _moveInput.sqrMagnitude > (_inputDeadzone * _inputDeadzone);

        private void OnEnable()
        {
            BindActions();
            EnableInput();
        }

        private void OnDisable()
        {
            UnbindActions();
            DisableInput();
        }

        public void EnableInput()
        {
            _isInputEnabled = true;
            if (_moveAction != null && _moveAction.action != null && !_moveAction.action.enabled)
                _moveAction.action.Enable();

            if (_sprintAction != null && _sprintAction.action != null && !_sprintAction.action.enabled)
                _sprintAction.action.Enable();
        }

        public void DisableInput()
        {
            _isInputEnabled = false;
            _moveInput = Vector2.zero;
            _isSprintHeld = false;
        }

        private void BindActions()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.performed += HandleMovePerformed;
                _moveAction.action.canceled += HandleMoveCanceled;
            }

            if (_sprintAction != null && _sprintAction.action != null)
            {
                _sprintAction.action.performed += HandleSprintPerformed;
                _sprintAction.action.canceled += HandleSprintCanceled;
            }
        }

        private void UnbindActions()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.performed -= HandleMovePerformed;
                _moveAction.action.canceled -= HandleMoveCanceled;
            }

            if (_sprintAction != null && _sprintAction.action != null)
            {
                _sprintAction.action.performed -= HandleSprintPerformed;
                _sprintAction.action.canceled -= HandleSprintCanceled;
            }
        }

        private void HandleMovePerformed(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
            if (_moveInput.sqrMagnitude < (_inputDeadzone * _inputDeadzone))
            {
                _moveInput = Vector2.zero;
            }
            else if (_moveInput.sqrMagnitude > 1f)
            {
                _moveInput.Normalize();
            }
        }

        private void HandleMoveCanceled(InputAction.CallbackContext context)
        {
            _moveInput = Vector2.zero;
        }

        private void HandleSprintPerformed(InputAction.CallbackContext context)
        {
            _isSprintHeld = context.ReadValueAsButton();
        }

        private void HandleSprintCanceled(InputAction.CallbackContext context)
        {
            _isSprintHeld = false;
        }
    }
}

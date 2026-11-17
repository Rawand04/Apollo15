using UnityEngine;

namespace SpaceSeries.Core.Player
{
    public sealed class ExperimentPlayerInputProvider : MonoBehaviour, IPlayerInputProvider
    {
        [SerializeField] private UnityEngine.InputSystem.InputActionAsset _controls;
        private UnityEngine.InputSystem.InputActionAsset _runtimeControls;
        private UnityEngine.InputSystem.InputActionMap _map;
        private Vector2 _move;
        private bool _sprint;
        private bool _inputEnabled = true;
        private bool _focused = true;
        public Vector2 MoveInput => _inputEnabled && _focused && isActiveAndEnabled ? _move : Vector2.zero;
        public bool IsSprintHeld => _inputEnabled && _focused && isActiveAndEnabled && _sprint;
        public bool HasMoveInput => MoveInput.sqrMagnitude > 0.001f;
        private void Awake()
        {
            if (_controls == null) { Debug.LogError("Astronaut action asset missing.", this); return; }
            _runtimeControls = Instantiate(_controls);
            _map = _runtimeControls.FindActionMap("OnFoot", true);
            var move = _map.FindAction("Move", true); move.performed += Move; move.canceled += Move;
            var sprint = _map.FindAction("Sprint", true); sprint.performed += Sprint; sprint.canceled += Sprint;
        }
        private void OnEnable() { if (_inputEnabled && _focused) _map?.Enable(); }
        private void OnDisable() { _map?.Disable(); _move = Vector2.zero; _sprint = false; }
        private void OnDestroy() { if (_runtimeControls != null) Destroy(_runtimeControls); }
        private void OnApplicationFocus(bool focused)
        {
            _focused = focused;
            if (!focused) { _map?.Disable(); _move = Vector2.zero; _sprint = false; }
            else if (_inputEnabled && isActiveAndEnabled) _map?.Enable();
        }
        private void Move(UnityEngine.InputSystem.InputAction.CallbackContext context) { _move = context.ReadValue<Vector2>(); }
        private void Sprint(UnityEngine.InputSystem.InputAction.CallbackContext context) { _sprint = context.ReadValueAsButton(); }
        public void EnableInput() { _inputEnabled = true; if (_focused && isActiveAndEnabled) _map?.Enable(); }
        public void DisableInput() { _inputEnabled = false; _map?.Disable(); _move = Vector2.zero; _sprint = false; }
    }
}

using UnityEngine;
using SpaceSeries.Core.Cameras;
using SpaceSeries.Core.Interaction;
namespace SpaceSeries.Core.Vehicles
{
    public sealed class VehicleControlSelector : MonoBehaviour, IInteractor, IRoverBoardingControl
    {
        [SerializeField] private MonoBehaviour _passengerSeat;
        private IRoverPassengerSeat PassengerSeat => _passengerSeat as IRoverPassengerSeat;
        public bool IsControlLocked => IsSelectionLocked;
        public bool TryAcquireRoverControl(SpaceSeries.Core.Player.AstronautController astronaut)
        {
            if (!isActiveAndEnabled || IsSelectionLocked || astronaut != _astronaut || _driver == null || _camera == null || _roverViewAnchor == null || !_driver.BeginDriving()) return false;
            PauseOnFoot(); _roverSelected = true;
            _driveMap?.Enable(); _driver.SetMove(Vector2.zero); _driver.SetBrake(false);
            _camera.SelectTarget(_roverViewAnchor, _driver.transform, 6f);
            return true;
        }
        public void ReleaseRoverControl()
        {
            _driveMap?.Disable(); if (_driver != null) _driver.StopDriving(); _roverSelected = false;
        }
        public void ParkRoverControl()
        {
            _driveMap?.Disable();
            if (_driver != null) { _driver.SetMove(Vector2.zero); _driver.SetBrake(true); }
        }
        public void RestoreOnFootControl()
        {
            _roverSelected = false; _onFootSelected = true;
            bool active = isActiveAndEnabled && _astronaut != null && _astronaut.isActiveAndEnabled;
            if (_deployer != null) _deployer.SetControlActive(active);
            if (_astronaut != null) _astronaut.SetLocomotionActive(active);
            if (active && _camera != null && _onFootViewAnchor != null) _camera.SelectTarget(_onFootViewAnchor, _astronaut.transform, 4.5f);
        }
        [SerializeField] private UnityEngine.InputSystem.InputActionAsset _controls;
[SerializeField] private DirectRoverDriver _driver;
        [SerializeField] private LunarModuleController _lunarModule;
        [SerializeField] private Transform _roverViewAnchor;
        [SerializeField] private Transform _inspectionOrigin;
        [SerializeField] private VehicleFollowCamera _camera;
        [Header("On-foot Control")]
        [SerializeField] private SpaceSeries.Core.Player.AstronautController _astronaut;
        [SerializeField] private SpaceSeries.Core.Experiments.ExperimentDeployer _deployer;
        [SerializeField] private Transform _onFootViewAnchor;
        [SerializeField] private bool _startOnFoot;
        private bool _onFootSelected;
        public bool OnFootSelected => _onFootSelected;
        private bool IsSelectionLocked => _deployer != null && _deployer.IsInputLocked;
        private UnityEngine.InputSystem.InputActionAsset _runtimeControls;
private UnityEngine.InputSystem.InputActionMap _driveMap;
        private bool _roverSelected;
        public bool RoverSelected => _roverSelected;
        public Transform InteractorTransform => _inspectionOrigin == null ? transform : _inspectionOrigin;
        public GameObject InteractorGameObject => gameObject;
        public IInteractable CurrentTarget => _roverSelected ? null : _lunarModule;
        public void TriggerInteractionAnimation(string triggerParamName) { }
        private void Awake()
        {
            if (_controls == null) { Debug.LogError("Vehicle controls asset is missing.", this); enabled = false; return; }
            _runtimeControls = Instantiate(_controls);
            _driveMap = _runtimeControls.FindActionMap("Rover", true);
            Bind("Rover/Move", Move);
            Bind("Rover/Brake", Brake);
            Bind("Selection/Base", Base);
            Bind("Selection/Rover", Rover);
            Bind("Selection/Cycle", Cycle);
            Bind("Selection/Inspect", Inspect);
            var onFoot = _runtimeControls.FindAction("Selection/OnFoot");
            if (onFoot != null) onFoot.performed += OnFoot;
            Bind("Camera/Look", Look);
}
        private void Bind(string path, System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> handler)
        {
            var action = _runtimeControls.FindAction(path, true);
            action.performed += handler; action.canceled += handler;
        }
        private void OnEnable()
        {
            if (_runtimeControls == null) return;
            _runtimeControls.FindActionMap("Selection").Enable();
            _runtimeControls.FindActionMap("Camera").Enable();
            if (_hasStarted)
            {
                if (_onFootSelected) SelectOnFoot(); else if (_roverSelected) SelectRover(); else SelectBase();
            }
        }
        private bool _hasStarted;
        private void Start() { _hasStarted = true; if (!_startOnFoot || !SelectOnFoot()) SelectRover(); }
        private void OnDisable()
        {
            if (_runtimeControls != null) _runtimeControls.Disable();
            if (_driver != null) _driver.StopDriving();
            if (_deployer != null) _deployer.SetControlActive(false);
            if (_astronaut != null) _astronaut.SetLocomotionActive(false);
            if (_camera != null) _camera.ClearLook();
        }
private void OnDestroy() { if (_runtimeControls != null) Destroy(_runtimeControls); }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                if (_runtimeControls != null) _runtimeControls.Disable();
                if (PassengerSeat != null && PassengerSeat.IsRiding) ParkRoverControl();
                else if (_driver != null) _driver.StopDriving();
                if (_camera != null) _camera.ClearLook();
}
            else if (isActiveAndEnabled && _runtimeControls != null)
            {
                OnEnable();
                if (_roverSelected) SelectRover();
            }
        }
        private void PauseOnFoot()
        {
            if (_deployer != null) _deployer.SetControlActive(false);
            if (_astronaut != null) _astronaut.SetLocomotionActive(false);
            _onFootSelected = false;
        }
        public bool SelectOnFoot()
        {
            if (IsSelectionLocked || _astronaut == null || _deployer == null || _camera == null || _onFootViewAnchor == null) return false;
            if (PassengerSeat != null && PassengerSeat.IsRiding) return PassengerSeat.TryDismount();
            ReleaseRoverControl(); RestoreOnFootControl(); return true;
        }
        private void OnFoot(UnityEngine.InputSystem.InputAction.CallbackContext context) { if (context.performed) SelectOnFoot(); }
        public bool SelectRover()
        {
            if (IsSelectionLocked) return false;
            if (PassengerSeat != null)
            {
                if (PassengerSeat.IsRiding) return PassengerSeat.Rider == _astronaut && TryAcquireRoverControl(_astronaut);
                return PassengerSeat.TryBoardAstronaut(_astronaut);
            }
            if (_driver == null || _camera == null || _roverViewAnchor == null || !_driver.BeginDriving()) return false;
            PauseOnFoot(); _roverSelected = true; _driveMap?.Enable();
            _camera.SelectTarget(_roverViewAnchor, _driver.transform, 6f); return true;
        }
        public void SelectBase()
        {
            if (IsSelectionLocked || _lunarModule == null || _camera == null || _lunarModule.ViewAnchor == null) return;
            PauseOnFoot();
            if (PassengerSeat != null && PassengerSeat.IsRiding) ParkRoverControl(); else ReleaseRoverControl();
            _roverSelected = false;
            _camera.SelectTarget(_lunarModule.ViewAnchor, _lunarModule.transform, 16f);
        }
private void Move(UnityEngine.InputSystem.InputAction.CallbackContext context) { if (_driver != null) _driver.SetMove(context.ReadValue<Vector2>()); }
        private void Brake(UnityEngine.InputSystem.InputAction.CallbackContext context) { if (_driver != null) _driver.SetBrake(context.ReadValueAsButton()); }
        private void Base(UnityEngine.InputSystem.InputAction.CallbackContext context) { if (context.performed) SelectBase(); }
        private void Rover(UnityEngine.InputSystem.InputAction.CallbackContext context) { if (context.performed) SelectRover(); }
        private void Cycle(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (_onFootSelected) SelectBase();
            else if (_roverSelected) { SelectOnFoot(); }
            else SelectRover();
        }
        private void Inspect(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (context.performed && !_roverSelected && !_onFootSelected && !IsSelectionLocked &&
                !(PassengerSeat != null && PassengerSeat.IsRiding) && _lunarModule != null) _lunarModule.Interact(this);
        }
private void Look(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (_camera != null) _camera.SetLook(context.ReadValue<Vector2>(), context.control.device is UnityEngine.InputSystem.Pointer);
        }
    }
}

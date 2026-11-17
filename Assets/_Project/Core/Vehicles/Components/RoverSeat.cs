using SpaceSeries.Core.Interaction;
using SpaceSeries.Core.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceSeries.Core.Vehicles
{
    /// <summary>
    /// Interactive driver seat on the Lunar Roving Vehicle.
    /// Implements IInteractable for boarding ('E'), manages camera switching,
    /// routes driving inputs, and handles safe dismounting back to foot locomotion.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RoverSeat : MonoBehaviour, IInteractable, IRoverPassengerSeat
    {
        [Header("Vehicle Reference")]
        [Tooltip("The RoverController associated with this seat.")]
        [SerializeField] private RoverController _rover;

        [Header("Mount & Dismount Transforms")]
        [Tooltip("Local transform position where the astronaut sits while driving.")]
        [SerializeField] private Transform _mountPoint;

        [Tooltip("World transform position beside the rover where the astronaut appears upon exiting.")]
        [SerializeField] private Transform _dismountPoint;

        [Header("Camera Switching")]
        [Tooltip("Camera or virtual camera activated while driving the rover.")]
        [SerializeField] private GameObject _roverCamera;

        [Tooltip("Default third-person astronaut camera (deactivated while driving). If null, auto-searches Camera.main.")]
        [SerializeField] private GameObject _astronautCamera;

        [Header("Input System")]
        [Tooltip("Action for steering and throttle (Vector2). E.g. 'Player/Move'.")]
        [SerializeField] private InputActionReference _driveAction;

        [Tooltip("Action for exiting the rover (Button, bound to 'E'). E.g. 'Player/Interact'.")]
        [SerializeField] private InputActionReference _exitAction;

        [Header("Interaction Settings")]
        [Tooltip("HUD prompt text shown when approaching the rover.")]
        [SerializeField] private string _promptText = "Lunar Roving Vehicle (LRV)";

        [Tooltip("Action verb.")]
        [SerializeField] private string _actionVerb = "Board";

        [Tooltip("Maximum distance in meters to board.")]
        [SerializeField] private float _interactionDistance = 2.8f;

        [Header("Mission Integration")]
        [Tooltip("Optional quest payload fired upon boarding the rover.")]
        [SerializeField] private QuestTriggerPayload _onBoardQuestPayload;

        [Tooltip("Event channel for quest events.")]
        [SerializeField] private QuestTriggerEventChannelSO _questChannel;

        [Header("Boarding Control")]
        [SerializeField] private MonoBehaviour _controlBridge;
        [SerializeField] private Transform[] _exitCandidates;
        [SerializeField, Min(0.01f)] private float _maximumBoardExitSpeed = 0.5f;
        [SerializeField] private LayerMask _groundLayers = ~0;
        [SerializeField] private LayerMask _obstacleLayers = ~0;
        [SerializeField, Min(1f)] private float _groundProbeHeight = 4f;
        private IRoverBoardingControl _controls;
        private AstronautController _seatedAstronaut;
        private CharacterController _character;
        private AstronautMotor _motor;
        private StylizedAstronautAnimatorAdapter _animationAdapter;
        private Transform _originalParent;
        private Renderer[] _renderers;
        private bool[] _rendererStates;
        private bool _colliderWasEnabled;
        private bool _transitioning;
        private Vector3 _lastSafePosition;
        private Quaternion _lastSafeRotation;
        private readonly Collider[] _clearance = new Collider[64];
        public bool IsRiding => _seatedAstronaut != null;
        public AstronautController Rider => _seatedAstronaut;
        public string LastFailure { get; private set; }
        public string PromptText => _promptText;
        public string ActionVerb => "Board";
        public bool IsInteractable => isActiveAndEnabled && !IsRiding && _rover != null && _rover.isActiveAndEnabled && !_rover.IsOccupied;
        public Vector3 InteractionPosition => transform.position;
        private bool IsStationary => _rover != null && _rover.VehicleRigidbody != null && _rover.VehicleRigidbody.linearVelocity.magnitude <= _maximumBoardExitSpeed;
        private void Awake()
        {
            if (_rover == null) _rover = GetComponentInParent<RoverController>();
            _controls = _controlBridge as IRoverBoardingControl;
        }
        public bool CanInteract(IInteractor actor)
        {
            return actor != null && IsInteractable && IsStationary && _controls != null && !_controls.IsControlLocked &&
                actor.InteractorTransform != null && (actor.InteractorTransform.position - InteractionPosition).sqrMagnitude <= _interactionDistance * _interactionDistance;
        }
        public void OnFocusGained(IInteractor actor) { }
        public void OnFocusLost(IInteractor actor) { }
        public InteractionResult Interact(IInteractor actor)
        {
            if (!CanInteract(actor)) return InteractionResult.Failed();
            var astronaut = actor.InteractorGameObject.GetComponent<AstronautController>();
            return TryBoardAstronaut(astronaut) ? InteractionResult.Success(null, default, default) : InteractionResult.Failed();
        }
        public bool TryBoardAstronaut(AstronautController astronaut)
        {
            if (_transitioning || astronaut == null || !astronaut.isActiveAndEnabled || !astronaut.IsLocomotionActive || !IsInteractable ||
                !IsStationary || _controls == null || _controls.IsControlLocked || _mountPoint == null ||
                Vector3.Distance(astronaut.transform.position, InteractionPosition) > _interactionDistance) return Reject("Move close to the stopped rover to board.");
            var character = astronaut.GetComponent<CharacterController>();
            if (character == null || !character.enabled) return Reject("Astronaut collider is unavailable.");
            if (!TrySafePosition(astronaut.transform.position, astronaut, character, out var safePosition)) return Reject("Boarding location has no safe ground.");
            _transitioning = true;
            var oldParent = astronaut.transform.parent;
            var oldPosition = astronaut.transform.position;
            var oldRotation = astronaut.transform.rotation;
            try
            {
                if (!_controls.TryAcquireRoverControl(astronaut)) return Reject("Rover ownership was not available.");
                _seatedAstronaut = astronaut; _character = character; _colliderWasEnabled = character.enabled;
                _motor = astronaut.GetComponent<AstronautMotor>(); _animationAdapter = astronaut.GetComponent<StylizedAstronautAnimatorAdapter>();
                _originalParent = oldParent; _lastSafePosition = safePosition; _lastSafeRotation = oldRotation;
                _renderers = astronaut.GetComponentsInChildren<Renderer>(true); _rendererStates = new bool[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++) _rendererStates[i] = _renderers[i].enabled;
                astronaut.SetLocomotionActive(false); character.enabled = false;
                astronaut.transform.SetParent(_mountPoint, true);
                astronaut.transform.SetPositionAndRotation(_mountPoint.position, _mountPoint.rotation);
                _animationAdapter?.SetRiding(true);
                for (int i = 0; i < _renderers.Length; i++) _renderers[i].enabled = false;
                LastFailure = null;
                return true;
            }
            catch (System.Exception exception)
            {
                _controls.ReleaseRoverControl();
                astronaut.transform.SetParent(oldParent, true); astronaut.transform.SetPositionAndRotation(oldPosition, oldRotation);
                RestoreVisuals(); character.enabled = true; _animationAdapter?.SetRiding(false); _seatedAstronaut = null;
                _controls.RestoreOnFootControl(); return Reject(exception.Message);
            }
            finally { _transitioning = false; }
        }
        public bool TryDismount()
        {
            if (_transitioning || !IsRiding || _controls == null || _controls.IsControlLocked) return false;
            if (!IsStationary) { _controls.ParkRoverControl(); return Reject("Stop rover before exiting."); }
            if (!FindExit(out var position, out var rotation)) { _controls.ParkRoverControl(); return Reject("Exit area blocked or no walkable ground."); }
            Detach(position, rotation, true); LastFailure = null; return true;
        }
        public void DismountAstronaut() { TryDismount(); }
        private bool FindExit(out Vector3 position, out Quaternion rotation)
        {
            if (_exitCandidates != null)
            foreach (var candidate in _exitCandidates)
                if (candidate != null && TrySafePosition(candidate.position, _seatedAstronaut, _character, out position))
                { rotation = Quaternion.Euler(0f, candidate.eulerAngles.y, 0f); return true; }
            if (_dismountPoint != null && TrySafePosition(_dismountPoint.position, _seatedAstronaut, _character, out position))
            { rotation = Quaternion.Euler(0f, _dismountPoint.eulerAngles.y, 0f); return true; }
            position = default; rotation = default; return false;
        }
        private bool TrySafePosition(Vector3 candidate, AstronautController actor, CharacterController character, out Vector3 position)
        {
            position = default;
            if (actor == null || character == null || _rover == null || !Physics.Raycast(candidate + Vector3.up * _groundProbeHeight, Vector3.down,
                out var hit, _groundProbeHeight * 2f, _groundLayers, QueryTriggerInteraction.Ignore)) return false;
            if (hit.transform.IsChildOf(_rover.transform) || hit.transform.IsChildOf(actor.transform) || Vector3.Angle(hit.normal, Vector3.up) > character.slopeLimit) return false;
            position = hit.point + Vector3.up * (character.skinWidth + 0.02f);
            float radius = Mathf.Max(0.01f, character.radius - character.skinWidth * 0.5f);
            float segment = Mathf.Max(0f, character.height * 0.5f - radius);
            Vector3 center = position + character.center;
            int count = Physics.OverlapCapsuleNonAlloc(center + Vector3.up * segment, center - Vector3.up * segment, radius,
                _clearance, _obstacleLayers, QueryTriggerInteraction.Ignore);
            if (count == _clearance.Length) return false;
            for (int i = 0; i < count; i++) if (_clearance[i] != null && !_clearance[i].transform.IsChildOf(actor.transform)) return false;
            return true;
        }
        private void Detach(Vector3 position, Quaternion rotation, bool restoreControl)
        {
            _transitioning = true;
            var actor = _seatedAstronaut;
            _controls?.ReleaseRoverControl();
            if (actor != null)
            {
                actor.transform.SetParent(_originalParent, true); actor.transform.SetPositionAndRotation(position, rotation);
                _motor?.ResetVelocity(); _animationAdapter?.SetRiding(false); RestoreVisuals();
                if (_character != null) _character.enabled = _colliderWasEnabled && restoreControl;
            }
            _seatedAstronaut = null; _character = null; _motor = null; _animationAdapter = null;
            _transitioning = false;
            if (restoreControl) _controls?.RestoreOnFootControl();
        }
        private void RestoreVisuals()
        { if (_renderers == null || _rendererStates == null) return; for (int i = 0; i < _renderers.Length; i++) if (_renderers[i] != null) _renderers[i].enabled = _rendererStates[i]; }
        private void Update()
        {
            if (IsRiding && (_rover == null || !_rover.isActiveAndEnabled || !_seatedAstronaut.isActiveAndEnabled || _controlBridge == null || !_controlBridge.isActiveAndEnabled)) EmergencyDetach();
        }
        private void OnDisable() { if (IsRiding) EmergencyDetach(); }
        private void EmergencyDetach()
        {
            if (_transitioning) return;
            if (FindExit(out var position, out var rotation)) { Detach(position, rotation, true); return; }
            if (TrySafePosition(_lastSafePosition, _seatedAstronaut, _character, out position)) { Detach(position, _lastSafeRotation, true); return; }
            var currentPosition = _seatedAstronaut.transform.position;
            Detach(currentPosition, _lastSafeRotation, false);
            Debug.LogError("No safe emergency exit. Rider is visible and detached with collider/movement paused; placement requires attention.", this);
        }
        private bool Reject(string reason) { LastFailure = reason; return false; }
    }
}

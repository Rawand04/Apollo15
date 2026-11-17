using UnityEngine;
using SpaceSeries.Core.Experiments;
using SpaceSeries.Core.Vehicles;

namespace SpaceSeries.Core.Interaction
{
    [DisallowMultipleComponent]
    public sealed class AstronautInteractionRouter : MonoBehaviour, IInteractor
    {
        [Header("Explicit Scene References")]
        [SerializeField] private ExperimentDeployer _deployer;
        [SerializeField] private RoverSeat _seat;
        [SerializeField] private VehicleControlSelector _selector;
        [SerializeField] private UnityEngine.InputSystem.InputActionReference _interactAction;
        [SerializeField] private InteractionPromptEventChannelSO _promptChannel;
        [SerializeField, Min(0.1f)] private float _feedbackDuration = 2f;
        private UnityEngine.InputSystem.InputAction _runtimeAction;
        private IInteractable _selected;
        private string _lastPrompt;
        private string _feedback;
        private float _feedbackUntil;
        private bool _focused = true;
        private int _lastInteractionFrame = -1;
        public Transform InteractorTransform => transform;
        public GameObject InteractorGameObject => gameObject;
        public IInteractable CurrentTarget => _selected;
        private void Awake()
        {
            if (_interactAction != null && _interactAction.action != null)
            { _runtimeAction = _interactAction.action.Clone(); _runtimeAction.performed += InteractPerformed; }
        }
        private void OnEnable() { if (_focused) _runtimeAction?.Enable(); }
        private void OnDisable() { _runtimeAction?.Disable(); Publish(null); }
        private void OnDestroy() { _runtimeAction?.Dispose(); }
        private void OnApplicationFocus(bool focused)
        {
            _focused = focused;
            if (focused && isActiveAndEnabled) _runtimeAction?.Enable();
            else { _runtimeAction?.Disable(); Publish(null); }
        }
        private void Update() { SelectTarget(); }
        private void InteractPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context) { ExecuteInteraction(); }
        private void SelectTarget()
        {
            _selected = null;
            if (!_focused || _deployer == null || _selector == null || _deployer.IsInputLocked) { Publish(null); return; }
            if (_seat != null && _seat.IsRiding)
            {
                _selected = _seat;
                Publish(Time.unscaledTime < _feedbackUntil ? _feedback : "Exit Rover");
                return;
}
            if (!_selector.OnFootSelected || !_deployer.ControlActive) { Publish(null); return; }
            var zone = _deployer.GetEligibleZone();
            bool canBoard = _seat != null && _seat.CanInteract(this);
            if (zone != null) _selected = zone;
            if (canBoard && (zone == null || (transform.position - _seat.InteractionPosition).sqrMagnitude <
                (transform.position - zone.InteractionPosition).sqrMagnitude)) _selected = _seat;
            string prompt = _selected == null ? null : _selected.ActionVerb + " " + _selected.PromptText;

            // Fallback: Check for other general interactables (e.g., Crewmate DialogueTrigger, consoles)
            if (_selected == null)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up, 2.5f, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform.root == transform.root) continue;
                    if (hits[i].TryGetComponent(out IInteractable interactable) ||
                        hits[i].GetComponentInParent<IInteractable>() is { } parentInter && (interactable = parentInter) != null)
                    {
                        if (interactable.CanInteract(this))
                        {
                            _selected = interactable;
                            prompt = _selected.ActionVerb + " " + _selected.PromptText;
                            break;
                        }
                    }
                }
            }

            Publish(Time.unscaledTime < _feedbackUntil ? _feedback : prompt);
        }
public bool ExecuteInteraction()
        {
            if (!isActiveAndEnabled || !_focused || Time.frameCount == _lastInteractionFrame || _deployer == null || _deployer.IsInputLocked) return false;
            _lastInteractionFrame = Time.frameCount;
            SelectTarget();
            bool success;
            if (_seat != null && _seat.IsRiding) success = _seat.TryDismount();
            else if (_selected is ExperimentDeploymentZone zone) success = _deployer.TryDeploy(zone);
            else if (_selected == _seat && _seat != null) success = _seat.Interact(this).IsSuccess;
            else if (_selected != null) success = _selected.Interact(this).IsSuccess;
            else return false;
            if (!success)
            {
                _feedback = _selected == _seat ? _seat.LastFailure : _deployer.LastFailure;
                _feedbackUntil = Time.unscaledTime + _feedbackDuration;
            }
            else _feedbackUntil = 0f;
            SelectTarget();
            return success;
        }
        private void Publish(string prompt)
        {
            if (_lastPrompt == prompt) return;
            _lastPrompt = prompt;
            if (_promptChannel == null) return;
            if (string.IsNullOrEmpty(prompt)) _promptChannel.HidePrompt();
            else _promptChannel.ShowPrompt(prompt, string.Empty, "E");
}
        public void TriggerInteractionAnimation(string parameterName) { }
    }
}

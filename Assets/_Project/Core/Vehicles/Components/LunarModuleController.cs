using UnityEngine;
using UnityEngine.Events;
using SpaceSeries.Core.Interaction;
namespace SpaceSeries.Core.Vehicles
{
    public sealed class LunarModuleController : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform _inspectionPoint;
        [SerializeField] private Transform _viewAnchor;
        [SerializeField, Min(0.1f)] private float _interactionDistance = 3f;
        [SerializeField] private UnityEvent _onInspected = new UnityEvent();
        public Transform ViewAnchor => _viewAnchor;
        public Transform InspectionPoint => _inspectionPoint;
        public int InspectionCount { get; private set; }
        public string PromptText => "Falcon Lunar Module";
        public string ActionVerb => "Inspect";
        public bool IsInteractable => enabled;
        public Vector3 InteractionPosition => _inspectionPoint == null ? transform.position : _inspectionPoint.position;
        public bool CanInteract(IInteractor interactor) => IsInteractable && interactor != null &&
            (interactor.InteractorTransform.position - InteractionPosition).sqrMagnitude <= _interactionDistance * _interactionDistance;
        public void OnFocusGained(IInteractor interactor) { }
        public void OnFocusLost(IInteractor interactor) { }
        public InteractionResult Interact(IInteractor interactor)
        {
            if (!CanInteract(interactor)) return InteractionResult.Failed();
            InspectionCount++;
            _onInspected.Invoke();
            Debug.Log("Falcon LM inspected. Mission-base event invoked.", this);
            return InteractionResult.Success(string.Empty, default, default);
        }
    }
}

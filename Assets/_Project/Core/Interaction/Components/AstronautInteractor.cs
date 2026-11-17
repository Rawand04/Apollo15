using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceSeries.Core.Interaction
{
    /// <summary>
    /// Player interactor component.
    /// Detects interactables via non-allocating physical queries, listens for 'E' input,
    /// and dispatches event-driven prompt, dialogue, animation, and quest payloads.
    /// </summary>
    [DisallowMultipleComponent]
    public class AstronautInteractor : MonoBehaviour, IInteractor
    {
        [Header("Input System")]
        [Tooltip("Action reference for interaction (bound to 'E' on keyboard, 'Y / North' on gamepad).")]
        [SerializeField] private InputActionReference _interactAction;

        [Header("Detection Parameters")]
        [Tooltip("Maximum search distance for interactables in front of the astronaut/camera.")]
        [Range(1f, 6f)]
        [SerializeField] private float _detectionRange = 3.0f;

        [Tooltip("Radius of the detection spherecast.")]
        [Range(0.1f, 1.5f)]
        [SerializeField] private float _spherecastRadius = 0.4f;

        [Tooltip("Layer mask filtering interactable objects.")]
        [SerializeField] private LayerMask _interactableLayers = ~0;

        [Tooltip("How frequently in seconds to query for interactables (0 = every frame).")]
        [SerializeField] private float _queryInterval = 0.05f;

        [Tooltip("If true, pressing E plays the pickup animation even when no interactable object is targeted.")]
        [SerializeField] private bool _playPickupIfNoTarget = true;

        [Header("Event Channels")]
        [Tooltip("Event channel for displaying HUD prompts.")]
        [SerializeField] private InteractionPromptEventChannelSO _promptEventChannel;

        [Tooltip("Event channel for triggering radio dialogue/CAPCOM.")]
        [SerializeField] private DialogueTriggerEventChannelSO _dialogueEventChannel;

        [Tooltip("Event channel for advancing NASA mission flight plan objectives.")]
        [SerializeField] private QuestTriggerEventChannelSO _questEventChannel;

        [Tooltip("Event channel for triggering astronaut animations.")]
        [SerializeField] private AnimationTriggerEventChannelSO _animationEventChannel;

        [Header("Component References")]
        [Tooltip("Optional animator reference. If null, searched on this GameObject or children.")]
        [SerializeField] private Animator _animator;

        [Tooltip("Transform used as viewpoint origin (Camera or Astronaut Eye/Head).")]
        [SerializeField] private Transform _viewOrigin;

        // Non-allocating physics buffer
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[8];
        private readonly Collider[] _overlapBuffer = new Collider[8];

        private IInteractable _currentTarget;
        private float _lastQueryTime;
        private bool _isInputBound;

        // IInteractor Properties
        public Transform InteractorTransform => transform;
        public GameObject InteractorGameObject => gameObject;
        public IInteractable CurrentTarget => _currentTarget;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_viewOrigin == null && Camera.main != null)
            {
                _viewOrigin = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            BindInput();
        }

        private void OnDisable()
        {
            UnbindInput();
            ClearCurrentFocus();
        }

        private void Update()
        {
            // Throttle detection frequency slightly to save CPU cycles
            if (Time.time >= _lastQueryTime + _queryInterval)
            {
                _lastQueryTime = Time.time;
                DetectInteractables();
            }
        }

        private void BindInput()
        {
            if (_interactAction != null && _interactAction.action != null)
            {
                _interactAction.action.performed += HandleInteractPerformed;
                if (!_interactAction.action.enabled)
                {
                    _interactAction.action.Enable();
                }
                _isInputBound = true;
            }
        }

        private void UnbindInput()
        {
            if (_isInputBound && _interactAction != null && _interactAction.action != null)
            {
                _interactAction.action.performed -= HandleInteractPerformed;
                _isInputBound = false;
            }
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            ExecuteInteraction();
        }

        /// <summary>
        /// Queries the physical environment in front of the astronaut for IInteractable targets.
        /// </summary>
        private void DetectInteractables()
        {
            Transform rayOriginTransform = _viewOrigin != null ? _viewOrigin : transform;
            Vector3 rayOrigin = rayOriginTransform.position;
            Vector3 rayDirection = rayOriginTransform.forward;

            IInteractable bestTarget = null;
            float closestDistance = float.MaxValue;

            // 1. Primary Directional Query: SphereCast along forward view vector
            int hitCount = Physics.SphereCastNonAlloc(
                rayOrigin,
                _spherecastRadius,
                rayDirection,
                _hitBuffer,
                _detectionRange,
                _interactableLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _hitBuffer[i];
                if (hit.collider.transform.root == transform.root)
                    continue;

                if (hit.collider.TryGetComponent(out IInteractable interactable) ||
                    hit.collider.GetComponentInParent<IInteractable>() is { } parentInteractable && (interactable = parentInteractable) != null)
                {
                    if (interactable.CanInteract(this) && hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        bestTarget = interactable;
                    }
                }
            }

            // 2. Fallback Proximity Query: OverlapSphere around astronaut feet/chest (for dropped rocks or floor consoles)
            if (bestTarget == null)
            {
                int overlapCount = Physics.OverlapSphereNonAlloc(
                    transform.position + Vector3.up * 0.8f,
                    _spherecastRadius * 2f,
                    _overlapBuffer,
                    _interactableLayers,
                    QueryTriggerInteraction.Collide);

                for (int i = 0; i < overlapCount; i++)
                {
                    Collider col = _overlapBuffer[i];
                    if (col.transform.root == transform.root)
                        continue;

                    if (col.TryGetComponent(out IInteractable interactable) ||
                        col.GetComponentInParent<IInteractable>() is { } parentInteractable && (interactable = parentInteractable) != null)
                    {
                        if (interactable.CanInteract(this))
                        {
                            bestTarget = interactable;
                            break;
                        }
                    }
                }
            }

            // 3. Resolve Target Transition
            if (bestTarget != _currentTarget)
            {
                SetCurrentFocus(bestTarget);
            }
            else if (_currentTarget != null && !_currentTarget.CanInteract(this))
            {
                ClearCurrentFocus();
            }
        }

        private void SetCurrentFocus(IInteractable newTarget)
        {
            if (_currentTarget != null)
            {
                _currentTarget.OnFocusLost(this);
            }

            _currentTarget = newTarget;

            if (_currentTarget != null)
            {
                _currentTarget.OnFocusGained(this);

                if (_promptEventChannel != null)
                {
                    _promptEventChannel.ShowPrompt(_currentTarget.PromptText, _currentTarget.ActionVerb, "E");
                }
            }
            else
            {
                _promptEventChannel?.HidePrompt();
            }
        }

        private void ClearCurrentFocus()
        {
            if (_currentTarget != null)
            {
                _currentTarget.OnFocusLost(this);
                _currentTarget = null;
            }

            _promptEventChannel?.HidePrompt();
        }

        /// <summary>
        /// Executes interaction on the currently focused target.
        /// </summary>
        public void ExecuteInteraction()
        {
            if (_currentTarget == null || !_currentTarget.CanInteract(this))
            {
                if (_playPickupIfNoTarget)
                {
                    TriggerInteractionAnimation("PickUp");
                }
                return;
            }

            InteractionResult result = _currentTarget.Interact(this);

            if (result.IsSuccess)
            {
                // 1. Dispatch Character Animation
                if (!string.IsNullOrEmpty(result.AnimationTrigger))
                {
                    TriggerInteractionAnimation(result.AnimationTrigger);
                }

                // 2. Dispatch Radio Dialogue / CAPCOM Event
                if (result.DialoguePayload.HasContent && _dialogueEventChannel != null)
                {
                    _dialogueEventChannel.RaiseEvent(result.DialoguePayload);
                }

                // 3. Dispatch Quest / NASA Flight Plan Event
                if (result.QuestPayload.HasContent && _questEventChannel != null)
                {
                    _questEventChannel.RaiseEvent(result.QuestPayload);
                }

                // 4. Refresh focus if object was one-shot and became non-interactable
                if (!_currentTarget.IsInteractable)
                {
                    ClearCurrentFocus();
                }
            }
        }

        public void TriggerInteractionAnimation(string triggerParamName)
        {
            if (string.IsNullOrEmpty(triggerParamName))
                return;

            // Broadcast via Event Channel
            _animationEventChannel?.RaiseEvent(triggerParamName);

            // Directly trigger local animator if attached
            if (_animator != null && _animator.isActiveAndEnabled)
            {
                int hash = Animator.StringToHash(triggerParamName);
                if (_animator.HasState(0, hash))
                {
                    _animator.SetTrigger(hash);
                }
                else
                {
                    _animator.SetTrigger(triggerParamName);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform rayOriginTransform = _viewOrigin != null ? _viewOrigin : transform;
            Gizmos.color = _currentTarget != null ? Color.green : Color.cyan;
            Gizmos.DrawRay(rayOriginTransform.position, rayOriginTransform.forward * _detectionRange);
            Gizmos.DrawWireSphere(rayOriginTransform.position + rayOriginTransform.forward * _detectionRange, _spherecastRadius);
        }
    }
}

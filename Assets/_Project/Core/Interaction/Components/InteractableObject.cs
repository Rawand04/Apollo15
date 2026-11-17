using System;
using UnityEngine;
using UnityEngine.Events;

namespace SpaceSeries.Core.Interaction
{
    /// <summary>
    /// Versatile, inspector-configurable interactable component implementing IInteractable.
    /// Drives dialogue, character animation, and quest events without hardcoded dependencies.
    /// </summary>
    [SelectionBase]
    public class InteractableObject : MonoBehaviour, IInteractable
    {
        [Header("Prompt Configuration")]
        [Tooltip("Display name of the target object (e.g. 'Passive Seismic Experiment').")]
        [SerializeField] private string _promptText = "Equipment";

        [Tooltip("Action verb displayed in prompt (e.g. 'Deploy', 'Inspect', 'Collect', 'Board').")]
        [SerializeField] private string _actionVerb = "Interact";

        [Tooltip("Maximum distance in meters from which the astronaut can interact.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _maxInteractionDistance = 2.5f;

        [Header("Interaction Behavior")]
        [Tooltip("If true, the object can only be interacted with once (e.g., collecting a rock or deploying ALSEP).")]
        [SerializeField] private bool _oneShotInteraction = false;

        [Tooltip("Whether this object is initially active and available for interaction.")]
        [SerializeField] private bool _isInteractable = true;

        [Header("Animation Trigger")]
        [Tooltip("Trigger parameter name sent to the astronaut's Animator (e.g. 'PickUp', 'Wave', 'Point').")]
        [SerializeField] private string _animationTrigger = "PickUp";

        [Header("Narrative Dialogue Event")]
        [Tooltip("Radio transmission triggered upon interaction.")]
        [SerializeField] private DialogueTriggerPayload _dialoguePayload;

        [Header("NASA Flight Plan / Quest Event")]
        [Tooltip("Mission objective progression triggered upon interaction.")]
        [SerializeField] private QuestTriggerPayload _questPayload;

        [Header("Custom Unity Events (Optional Designer Hooks)")]
        [SerializeField] private UnityEvent<IInteractor> _onInteracted;
        [SerializeField] private UnityEvent _onFocusGained;
        [SerializeField] private UnityEvent _onFocusLost;

        private bool _hasBeenInteractedWith;

        // IInteractable Properties
        public string PromptText => _promptText;
        public string ActionVerb => _actionVerb;
        public bool IsInteractable => _isInteractable && (!_oneShotInteraction || !_hasBeenInteractedWith);
        public Vector3 InteractionPosition => transform.position;
        public float MaxInteractionDistance => _maxInteractionDistance;

        public virtual bool CanInteract(IInteractor interactor)
        {
            if (!IsInteractable || interactor == null)
                return false;

            float sqrDist = (interactor.InteractorTransform.position - InteractionPosition).sqrMagnitude;
            return sqrDist <= (_maxInteractionDistance * _maxInteractionDistance);
        }

        public virtual void OnFocusGained(IInteractor interactor)
        {
            _onFocusGained?.Invoke();
        }

        public virtual void OnFocusLost(IInteractor interactor)
        {
            _onFocusLost?.Invoke();
        }

        public virtual InteractionResult Interact(IInteractor interactor)
        {
            if (!CanInteract(interactor))
                return InteractionResult.Failed();

            _hasBeenInteractedWith = true;

            // Execute custom inspector callbacks (e.g. opening doors, toggling lights)
            _onInteracted?.Invoke(interactor);

            return InteractionResult.Success(
                _animationTrigger,
                _dialoguePayload,
                _questPayload);
        }

        /// <summary>
        /// Allows external systems (e.g., mission progression) to dynamically re-enable or disable interaction.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
        }

        /// <summary>
        /// Dynamically updates the dialogue payload (e.g., if dialogue advances based on mission phase).
        /// </summary>
        public void SetDialoguePayload(DialogueTriggerPayload newDialogue)
        {
            _dialoguePayload = newDialogue;
        }

        /// <summary>
        /// Dynamically updates the quest payload.
        /// </summary>
        public void SetQuestPayload(QuestTriggerPayload newQuest)
        {
            _questPayload = newQuest;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(InteractionPosition, _maxInteractionDistance);
        }
    }
}

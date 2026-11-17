using SpaceSeries.Core.Interaction;
using UnityEngine;

namespace SpaceSeries.Core.Dialogue
{
    /// <summary>
    /// Scene trigger component for starting conversations with NPCs, radios, or consoles.
    /// Implements IInteractable so astronauts can walk up to crewmates and press 'E' to talk.
    /// Also supports trigger zones (OnTriggerEnter) and programmatic triggering.
    /// </summary>
    public class DialogueTrigger : MonoBehaviour, IInteractable
    {
        [Header("Dialogue Configuration")]
        [Tooltip("The conversation sequence to start.")]
        [SerializeField] private DialogueSequenceSO _dialogueSequence;

        [Tooltip("Event channel used to request dialogue initiation.")]
        [SerializeField] private DialogueChannelSO _dialogueChannel;

        [Header("Interaction Settings")]
        [Tooltip("Prompt text displayed in the HUD (e.g., 'Jim Irwin', 'Mission Radio').")]
        [SerializeField] private string _promptText = "Crewmate";

        [Tooltip("Action verb (e.g., 'Talk to', 'Hail', 'Listen to').")]
        [SerializeField] private string _actionVerb = "Talk to";

        [Tooltip("Maximum interaction distance in meters.")]
        [SerializeField] private float _interactionDistance = 2.5f;

        [Tooltip("If true, dialogue can only be initiated once from this trigger.")]
        [SerializeField] private bool _oneShotOnly = false;

        [Header("Trigger Zone Settings")]
        [Tooltip("If true, entering the collider trigger initiates dialogue automatically.")]
        [SerializeField] private bool _triggerOnCollision = false;

        private bool _hasTriggered;

        // IInteractable Properties
        public string PromptText => _promptText;
        public string ActionVerb => _actionVerb;
        public bool IsInteractable => (!_oneShotOnly || !_hasTriggered) && _dialogueSequence != null;
        public Vector3 InteractionPosition => transform.position;

        public bool CanInteract(IInteractor interactor)
        {
            if (!IsInteractable || interactor == null)
                return false;

            float sqrDist = (interactor.InteractorTransform.position - InteractionPosition).sqrMagnitude;
            return sqrDist <= (_interactionDistance * _interactionDistance);
        }

        public void OnFocusGained(IInteractor interactor) { }
        public void OnFocusLost(IInteractor interactor) { }

        public InteractionResult Interact(IInteractor interactor)
        {
            if (!CanInteract(interactor))
                return InteractionResult.Failed();

            TriggerDialogue();
            return InteractionResult.Success(string.Empty, default, default);
        }

        public void TriggerDialogue()
        {
            if (!IsInteractable)
                return;

            _hasTriggered = true;

            if (_dialogueChannel != null && _dialogueSequence != null)
            {
                _dialogueChannel.RequestDialogue(_dialogueSequence);
            }
            else
            {
                Debug.LogWarning($"[DialogueTrigger] Missing DialogueChannel or DialogueSequence on {name}");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggerOnCollision && IsInteractable && other.CompareTag("Player"))
            {
                TriggerDialogue();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, _interactionDistance);
        }
    }
}

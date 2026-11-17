using SpaceSeries.Core.Interaction;
using UnityEngine;

namespace SpaceSeries.Core.Dialogue
{
    /// <summary>
    /// Post-dialogue action that advances or completes a NASA mission objective via QuestTriggerEventChannelSO.
    /// Demonstrates system interoperability between Dialogue and Mission systems.
    /// </summary>
    [CreateAssetMenu(fileName = "Action_Quest_Advance", menuName = "SpaceSeries/Dialogue/Actions/Quest Trigger")]
    public class QuestDialogueActionSO : DialogueActionSO
    {
        [Header("Event Channel")]
        [Tooltip("Quest channel to broadcast to.")]
        [SerializeField] private QuestTriggerEventChannelSO _questChannel;

        [Header("Objective Payload")]
        [Tooltip("Parent mission ID (e.g. 'Apollo15_EVA1').")]
        [SerializeField] private string _questId = "Apollo15_EVA1";

        [Tooltip("Objective ID to advance or complete (e.g. 'Talk_To_Irwin_About_ALSEP').")]
        [SerializeField] private string _objectiveId;

        [Tooltip("Specific target identifier if required.")]
        [SerializeField] private string _targetId;

        [Tooltip("Objective progress increment amount.")]
        [SerializeField] private int _progressDelta = 1;

        [Tooltip("True if this dialogue instantly marks the objective completed.")]
        [SerializeField] private bool _isCompletingStep = true;

        public override void Execute(DialogueExecutionContext context)
        {
            if (_questChannel == null)
            {
                Debug.LogWarning($"[QuestDialogueActionSO] Cannot dispatch quest event: QuestChannel is null on {name}");
                return;
            }

            QuestTriggerPayload payload = new QuestTriggerPayload
            {
                QuestId = _questId,
                ObjectiveId = _objectiveId,
                TargetId = _targetId,
                ProgressDelta = _progressDelta,
                IsCompletingStep = _isCompletingStep
            };

            _questChannel.RaiseEvent(payload);
        }
    }
}

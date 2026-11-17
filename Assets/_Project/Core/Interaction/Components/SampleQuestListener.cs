using UnityEngine;

namespace SpaceSeries.Core.Interaction
{
    /// <summary>
    /// Example system listener demonstrating how the MissionManager
    /// subscribes to QuestTriggerEventChannelSO to advance NASA flight plan objectives
    /// without coupling to interactable GameObjects.
    /// </summary>
    public class SampleQuestListener : MonoBehaviour
    {
        [SerializeField] private QuestTriggerEventChannelSO _questChannel;

        private void OnEnable()
        {
            if (_questChannel != null)
            {
                _questChannel.OnEventRaised += HandleQuestTriggered;
            }
        }

        private void OnDisable()
        {
            if (_questChannel != null)
            {
                _questChannel.OnEventRaised -= HandleQuestTriggered;
            }
        }

        private void HandleQuestTriggered(QuestTriggerPayload payload)
        {
            Debug.Log($"[Mission System] Objective Event: Mission='{payload.QuestId}', Objective='{payload.ObjectiveId}', ProgressDelta=+{payload.ProgressDelta}, IsComplete={payload.IsCompletingStep}");
            // In full production: Update NASA checklist, trigger HUD telemetry checkmark, serialize progress.
        }
    }
}

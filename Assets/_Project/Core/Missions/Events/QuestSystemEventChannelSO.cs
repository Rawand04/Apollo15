using System;
using UnityEngine;

namespace SpaceSeries.Core.Missions
{
    /// <summary>
    /// Event Channel ScriptableObject broadcasting high-level mission and objective lifecycle events.
    /// Drives HUD checklist displays, radio comms triggers, telemetry markers, and save systems.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestSystemEventChannel", menuName = "SpaceSeries/Missions/Quest System Event Channel")]
    public class QuestSystemEventChannelSO : ScriptableObject
    {
        // Requests
        public event Action<QuestDataSO> OnStartQuestRequested;

        // Lifecycle Events
        public event Action<QuestRuntimeState> OnQuestStarted;
        public event Action<ObjectiveRuntimeState> OnObjectiveUpdated;
        public event Action<ObjectiveRuntimeState> OnObjectiveCompleted;
        public event Action<QuestRuntimeState> OnQuestCompleted;

        public void RequestStartQuest(QuestDataSO questData)
        {
            if (questData != null)
            {
                OnStartQuestRequested?.Invoke(questData);
            }
        }

        public void RaiseQuestStarted(QuestRuntimeState state)
        {
            OnQuestStarted?.Invoke(state);
        }

        public void RaiseObjectiveUpdated(ObjectiveRuntimeState objective)
        {
            OnObjectiveUpdated?.Invoke(objective);
        }

        public void RaiseObjectiveCompleted(ObjectiveRuntimeState objective)
        {
            OnObjectiveCompleted?.Invoke(objective);
        }

        public void RaiseQuestCompleted(QuestRuntimeState state)
        {
            OnQuestCompleted?.Invoke(state);
        }
    }
}

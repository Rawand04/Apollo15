using System;
using UnityEngine;

namespace SpaceSeries.Core.Interaction
{
    /// <summary>
    /// Event Channel ScriptableObject for broadcasting quest and NASA flight plan objective triggers.
    /// Notifies the MissionManager and UI mission log when an objective progresses.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestTriggerChannel", menuName = "SpaceSeries/Events/Quest Trigger Channel")]
    public class QuestTriggerEventChannelSO : ScriptableObject
    {
        public event Action<QuestTriggerPayload> OnEventRaised;

        public void RaiseEvent(QuestTriggerPayload payload)
        {
            if (payload.HasContent)
            {
                OnEventRaised?.Invoke(payload);
            }
        }
    }
}

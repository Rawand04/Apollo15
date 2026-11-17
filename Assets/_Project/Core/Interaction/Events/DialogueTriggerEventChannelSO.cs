using System;
using UnityEngine;

namespace SpaceSeries.Core.Interaction
{
    /// <summary>
    /// Event Channel ScriptableObject for broadcasting dialogue triggers to the Comms/Radio Manager.
    /// Dispatches NASA archival voice lines, CAPCOM transmissions, and visor subtitles.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueTriggerChannel", menuName = "SpaceSeries/Events/Dialogue Trigger Channel")]
    public class DialogueTriggerEventChannelSO : ScriptableObject
    {
        public event Action<DialogueTriggerPayload> OnEventRaised;

        public void RaiseEvent(DialogueTriggerPayload payload)
        {
            if (payload.HasContent)
            {
                OnEventRaised?.Invoke(payload);
            }
        }
    }
}

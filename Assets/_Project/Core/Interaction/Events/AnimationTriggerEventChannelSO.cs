using System;
using UnityEngine;

namespace SpaceSeries.Core.Interaction
{
    /// <summary>
    /// Event Channel ScriptableObject for requesting character animation triggers.
    /// Used when interactions trigger player astronaut animations (e.g., reaching, sampling, deploying).
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationTriggerChannel", menuName = "SpaceSeries/Events/Animation Trigger Channel")]
    public class AnimationTriggerEventChannelSO : ScriptableObject
    {
        public event Action<string> OnEventRaised;

        public void RaiseEvent(string triggerParameterName)
        {
            if (!string.IsNullOrEmpty(triggerParameterName))
            {
                OnEventRaised?.Invoke(triggerParameterName);
            }
        }
    }
}

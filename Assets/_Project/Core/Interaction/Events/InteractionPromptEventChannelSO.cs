using System;
using UnityEngine;

namespace SpaceSeries.Core.Interaction
{
    /// <summary>
    /// Event Channel ScriptableObject for broadcasting interaction prompt updates to the Visor HUD.
    /// Eliminates direct coupling between the player, interactable objects, and the UI.
    /// </summary>
    [CreateAssetMenu(fileName = "InteractionPromptChannel", menuName = "SpaceSeries/Events/Interaction Prompt Channel")]
    public class InteractionPromptEventChannelSO : ScriptableObject
    {
        public event Action<InteractionPromptData> OnEventRaised;

        public void RaiseEvent(InteractionPromptData promptData)
        {
            OnEventRaised?.Invoke(promptData);
        }

        public void ShowPrompt(string promptText, string actionVerb = "Interact", string keyBinding = "E")
        {
            RaiseEvent(new InteractionPromptData(true, promptText, actionVerb, keyBinding));
        }

        public void HidePrompt()
        {
            RaiseEvent(new InteractionPromptData(false));
        }
    }
}

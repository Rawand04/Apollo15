using System;
using UnityEngine;

namespace SpaceSeries.Core.Interaction
{
    /// <summary>
    /// Event data dispatched when the interactor focuses on or leaves an interactable object.
    /// </summary>
    [Serializable]
    public struct InteractionPromptData
    {
        public bool IsVisible;
        public string PromptMessage;
        public string ActionVerb;
        public string KeyBindingDisplay;

        public InteractionPromptData(bool isVisible, string promptMessage = "", string actionVerb = "Interact", string keyBindingDisplay = "E")
        {
            IsVisible = isVisible;
            PromptMessage = promptMessage;
            ActionVerb = actionVerb;
            KeyBindingDisplay = keyBindingDisplay;
        }

        public readonly string FormattedPrompt => IsVisible ? $"[{KeyBindingDisplay}] {ActionVerb} {PromptMessage}".Trim() : string.Empty;
    }

    /// <summary>
    /// Event payload dispatched when an interaction triggers NASA archival or narrative radio comms.
    /// </summary>
    [Serializable]
    public struct DialogueTriggerPayload
    {
        [Tooltip("Unique identifier matching the mission transcript tree.")]
        public string DialogueId;

        [Tooltip("Speaker name (e.g., 'Dave Scott', 'Jim Irwin', 'CAPCOM [Joe Allen]').")]
        public string SpeakerName;

        [Tooltip("Subtitled transcript text displayed on the astronaut's visor HUD.")]
        [TextArea(2, 4)]
        public string Transcript;

        [Tooltip("Archival NASA radio transmission audio clip.")]
        public AudioClip VoiceClip;

        [Tooltip("Priority for radio transmission queuing (Emergency, MissionCritical, AmbientChatter).")]
        public int Priority;

        public readonly bool HasContent => !string.IsNullOrEmpty(DialogueId) || !string.IsNullOrEmpty(Transcript) || VoiceClip != null;
    }

    /// <summary>
    /// Event payload dispatched when an interaction advances or completes a NASA mission objective.
    /// </summary>
    [Serializable]
    public struct QuestTriggerPayload
    {
        [Tooltip("Identifier of the parent mission (e.g., 'Mission_Apollo15_EVA1').")]
        public string QuestId;

        [Tooltip("Identifier of the specific objective (e.g., 'Deploy_PSEP_Station').")]
        public string ObjectiveId;

        [Tooltip("Specific target or item identifier (e.g., 'PSEP_Hardware').")]
        public string TargetId;

        [Tooltip("Progress contribution amount (e.g., 1 for single deploy, or rock sample count).")]
        public int ProgressDelta;

        [Tooltip("True if this interaction instantly marks the objective as fully accomplished.")]
        public bool IsCompletingStep;

        public readonly bool HasContent => !string.IsNullOrEmpty(ObjectiveId);
    }

    /// <summary>
    /// Detailed outcome package returned after executing an interaction.
    /// </summary>
    public struct InteractionResult
    {
        public bool IsSuccess;
        public string AnimationTrigger;
        public DialogueTriggerPayload DialoguePayload;
        public QuestTriggerPayload QuestPayload;

        public static InteractionResult Success(string animationTrigger, DialogueTriggerPayload dialogue, QuestTriggerPayload quest)
        {
            return new InteractionResult
            {
                IsSuccess = true,
                AnimationTrigger = animationTrigger,
                DialoguePayload = dialogue,
                QuestPayload = quest
            };
        }

        public static InteractionResult Failed()
        {
            return new InteractionResult
            {
                IsSuccess = false,
                AnimationTrigger = string.Empty,
                DialoguePayload = default,
                QuestPayload = default
            };
        }
    }
}

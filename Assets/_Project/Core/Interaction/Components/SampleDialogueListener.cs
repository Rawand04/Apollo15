using UnityEngine;

namespace SpaceSeries.Core.Interaction
{
    /// <summary>
    /// Example system listener demonstrating how the Audio/Comms Manager
    /// subscribes to DialogueTriggerEventChannelSO to play historical NASA voice lines
    /// and display radio subtitles on the visor HUD without coupling to interactables.
    /// </summary>
    public class SampleDialogueListener : MonoBehaviour
    {
        [SerializeField] private DialogueTriggerEventChannelSO _dialogueChannel;

        private void OnEnable()
        {
            if (_dialogueChannel != null)
            {
                _dialogueChannel.OnEventRaised += HandleDialogueTriggered;
            }
        }

        private void OnDisable()
        {
            if (_dialogueChannel != null)
            {
                _dialogueChannel.OnEventRaised -= HandleDialogueTriggered;
            }
        }

        private void HandleDialogueTriggered(DialogueTriggerPayload payload)
        {
            Debug.Log($"[Radio Comms] <{payload.SpeakerName}>: \"{payload.Transcript}\" (Clip: {payload.VoiceClip?.name ?? "None"})");
            // In full production: Play Quindar Tone, route clip to AudioSource with suit radio filter, display HUD subtitle.
        }
    }
}

using System;
using UnityEngine;

namespace SpaceSeries.Core.Dialogue
{
    /// <summary>
    /// Event Channel ScriptableObject connecting dialogue requests, runners, and UI views.
    /// Completely decouples scene triggers and game managers from the dialogue interface.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueChannel", menuName = "SpaceSeries/Dialogue/Dialogue Channel")]
    public class DialogueChannelSO : ScriptableObject
    {
        // Requests
        public event Action<DialogueSequenceSO> OnDialogueRequested;

        // State Notifications
        public event Action<DialogueSequenceSO> OnDialogueStarted;
        public event Action<DialogueLine, int, int> OnLineStarted; // line, lineIndex, totalLines
        public event Action<DialogueLine> OnLineTypingCompleted;
        public event Action<DialogueSequenceSO> OnDialogueCompleted;

        public void RequestDialogue(DialogueSequenceSO sequence)
        {
            if (sequence != null)
            {
                OnDialogueRequested?.Invoke(sequence);
            }
        }

        public void RaiseDialogueStarted(DialogueSequenceSO sequence)
        {
            OnDialogueStarted?.Invoke(sequence);
        }

        public void RaiseLineStarted(DialogueLine line, int lineIndex, int totalLines)
        {
            OnLineStarted?.Invoke(line, lineIndex, totalLines);
        }

        public void RaiseLineTypingCompleted(DialogueLine line)
        {
            OnLineTypingCompleted?.Invoke(line);
        }

        public void RaiseDialogueCompleted(DialogueSequenceSO sequence)
        {
            OnDialogueCompleted?.Invoke(sequence);
        }
    }
}

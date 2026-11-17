namespace SpaceSeries.Core.Dialogue
{
    /// <summary>
    /// Contract for running and controlling dialogue execution flow.
    /// </summary>
    public interface IDialogueRunner
    {
        bool IsDialogueActive { get; }
        bool IsTyping { get; }
        DialogueSequenceSO CurrentSequence { get; }
        DialogueLine CurrentLine { get; }
        int CurrentLineIndex { get; }

        void StartDialogue(DialogueSequenceSO sequence);
        void Advance(); // Skips typing if in progress, or advances to next line if typing complete
        void SkipTyping();
        void SkipEntireDialogue();
        void StopDialogue();
    }
}

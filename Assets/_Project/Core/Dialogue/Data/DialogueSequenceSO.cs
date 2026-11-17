using UnityEngine;

namespace SpaceSeries.Core.Dialogue
{
    /// <summary>
    /// ScriptableObject defining an entire dialogue conversation, radio exchange, or debrief.
    /// Can be chained to other sequences or trigger modular actions upon completion.
    /// </summary>
    [CreateAssetMenu(fileName = "Dialogue_NewSequence", menuName = "SpaceSeries/Dialogue/Dialogue Sequence")]
    public class DialogueSequenceSO : ScriptableObject
    {
        [Header("Sequence Identity")]
        [Tooltip("Unique identifier for this conversation.")]
        [SerializeField] private string _dialogueId = "Dialogue_Apollo15_01";

        [Tooltip("Conversation title or historical log entry name.")]
        [SerializeField] private string _title = "Lunar Surface Comms";

        [Header("Content")]
        [Tooltip("Ordered lines comprising this dialogue sequence.")]
        [SerializeField] private DialogueLine[] _lines;

        [Header("Chaining & Flow")]
        [Tooltip("Optional subsequent dialogue sequence to automatically trigger upon completion.")]
        [SerializeField] private DialogueSequenceSO _nextSequence;

        [Tooltip("Whether the player is allowed to fast-forward skip the entire conversation.")]
        [SerializeField] private bool _allowSkipEntireSequence = true;

        [Header("Events & Actions")]
        [Tooltip("Actions executed when this dialogue sequence begins.")]
        [SerializeField] private DialogueActionSO[] _onStartActions;

        [Tooltip("Actions executed when this dialogue sequence finishes (advancing quests, unlocking tools, etc.).")]
        [SerializeField] private DialogueActionSO[] _onEndActions;

        // Public Properties
        public string DialogueId => _dialogueId;
        public string Title => _title;
        public DialogueLine[] Lines => _lines;
        public int LineCount => _lines != null ? _lines.Length : 0;
        public DialogueSequenceSO NextSequence => _nextSequence;
        public bool AllowSkipEntireSequence => _allowSkipEntireSequence;
        public DialogueActionSO[] OnStartActions => _onStartActions;
        public DialogueActionSO[] OnEndActions => _onEndActions;

        public DialogueLine GetLine(int index)
        {
            if (_lines != null && index >= 0 && index < _lines.Length)
                return _lines[index];

            return null;
        }
    }
}

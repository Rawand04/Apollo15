using UnityEngine;

namespace SpaceSeries.Core.Dialogue
{
    /// <summary>
    /// Context passed to DialogueActionSO during execution.
    /// Provides access to the dialogue runner and current sequence.
    /// </summary>
    public struct DialogueExecutionContext
    {
        public DialogueSequenceSO Sequence;
        public int TotalLines;
        public GameObject Instigator;

        public DialogueExecutionContext(DialogueSequenceSO sequence, int totalLines, GameObject instigator = null)
        {
            Sequence = sequence;
            TotalLines = totalLines;
            Instigator = instigator;
        }
    }

    /// <summary>
    /// Abstract ScriptableObject base class for actions executed during or after dialogue.
    /// Enables modular post-dialogue events (e.g., advancing NASA missions, unlocking equipment, triggering alarms).
    /// </summary>
    public abstract class DialogueActionSO : ScriptableObject
    {
        [Tooltip("Description of what this action accomplishes.")]
        [SerializeField] private string _description;

        public abstract void Execute(DialogueExecutionContext context);
    }
}

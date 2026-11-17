using System;
using UnityEngine;

namespace SpaceSeries.Core.Dialogue
{
    /// <summary>
    /// Represents a single spoken or radio-transmitted line in a dialogue sequence.
    /// Supports speaker metadata, portrait moods, voice recordings, and custom typing speeds.
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        [Header("Speaker & Portrait")]
        [Tooltip("The character or radio entity speaking this line.")]
        [SerializeField] private SpeakerDataSO _speaker;

        [Tooltip("Mood key matching a portrait defined in SpeakerDataSO (e.g. 'Normal', 'Concerned', 'RadioStatic').")]
        [SerializeField] private string _portraitMood = "Normal";

        [Tooltip("Optional one-off portrait override for this specific line.")]
        [SerializeField] private Sprite _customPortraitOverride;

        [Header("Dialogue Content")]
        [Tooltip("The text displayed on the screen. Supports rich text tags (e.g., <color=yellow>ALSEP</color>).")]
        [TextArea(3, 6)]
        [SerializeField] private string _text;

        [Tooltip("Optional full-line voice audio clip (e.g., historical NASA transmission recording).")]
        [SerializeField] private AudioClip _voiceClip;

        [Header("Timing & Progression")]
        [Tooltip("Typing speed in characters per second. Default is 35.")]
        [Range(10f, 120f)]
        [SerializeField] private float _charactersPerSecond = 35f;

        [Tooltip("If greater than 0, automatically advances to the next line this many seconds after typing finishes without requiring input.")]
        [SerializeField] private float _autoAdvanceDelay = -1f;

        [Header("Post-Line Actions")]
        [Tooltip("Optional actions executed when this specific line completes.")]
        [SerializeField] private DialogueActionSO[] _onLineCompletedActions;

        // Public Properties
        public SpeakerDataSO Speaker => _speaker;
        public string PortraitMood => _portraitMood;
        public Sprite CustomPortraitOverride => _customPortraitOverride;
        public string Text => _text;
        public AudioClip VoiceClip => _voiceClip;
        public float CharactersPerSecond => _charactersPerSecond;
        public float AutoAdvanceDelay => _autoAdvanceDelay;
        public bool IsAutoAdvance => _autoAdvanceDelay > 0f;
        public DialogueActionSO[] OnLineCompletedActions => _onLineCompletedActions;

        /// <summary>
        /// Resolves the final portrait sprite to display for this line.
        /// </summary>
        public Sprite ResolvePortrait()
        {
            if (_customPortraitOverride != null)
                return _customPortraitOverride;

            if (_speaker != null)
                return _speaker.GetPortrait(_portraitMood);

            return null;
        }
    }
}

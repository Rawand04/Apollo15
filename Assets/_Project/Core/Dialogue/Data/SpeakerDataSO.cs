using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceSeries.Core.Dialogue
{
    /// <summary>
    /// Definition of an NPC or astronaut speaker in the dialogue system.
    /// Reusable across any title in the Moon & Mars series (e.g., astronauts, CAPCOM, Martian colonists).
    /// </summary>
    [CreateAssetMenu(fileName = "Speaker_New", menuName = "SpaceSeries/Dialogue/Speaker Data")]
    public class SpeakerDataSO : ScriptableObject
    {
        [Serializable]
        public struct MoodPortrait
        {
            [Tooltip("Emotion or mood key (e.g., 'Normal', 'VisorClosed', 'Concerned', 'RadioStatic', 'Alert').")]
            public string MoodKey;

            [Tooltip("Portrait sprite corresponding to this mood.")]
            public Sprite PortraitSprite;
        }

        [Header("Identity")]
        [Tooltip("Display name of the speaker (e.g., 'Dave Scott', 'CAPCOM [Joe Allen]').")]
        [SerializeField] private string _speakerName = "Astronaut";

        [Tooltip("Optional organizational title or role (e.g., 'Apollo 15 Commander', 'Houston Flight Control').")]
        [SerializeField] private string _title = "Flight Crew";

        [Header("Portraits")]
        [Tooltip("Default portrait shown if no specific mood is requested.")]
        [SerializeField] private Sprite _defaultPortrait;

        [Tooltip("Mood-based portrait variants.")]
        [SerializeField] private List<MoodPortrait> _moodPortraits = new List<MoodPortrait>();

        [Header("Visual & Audio Identity")]
        [Tooltip("Color tint applied to the speaker's name text on the HUD.")]
        [SerializeField] private Color _nameColor = Color.white;

        [Tooltip("Audio blip played during typewriter text progression.")]
        [SerializeField] private AudioClip _typewriterAudioBlip;

        [Tooltip("Audio pitch for the typewriter blip (0.8 - 1.2 creates distinctive character voices).")]
        [Range(0.5f, 2.0f)]
        [SerializeField] private float _voicePitch = 1.0f;

        // Public Properties
        public string SpeakerName => _speakerName;
        public string Title => _title;
        public Sprite DefaultPortrait => _defaultPortrait;
        public Color NameColor => _nameColor;
        public AudioClip TypewriterAudioBlip => _typewriterAudioBlip;
        public float VoicePitch => _voicePitch;

        /// <summary>
        /// Retrieves the portrait sprite matching the requested mood key, or falls back to default.
        /// </summary>
        public Sprite GetPortrait(string moodKey)
        {
            if (!string.IsNullOrEmpty(moodKey))
            {
                for (int i = 0; i < _moodPortraits.Count; i++)
                {
                    if (string.Equals(_moodPortraits[i].MoodKey, moodKey, StringComparison.OrdinalIgnoreCase))
                    {
                        if (_moodPortraits[i].PortraitSprite != null)
                            return _moodPortraits[i].PortraitSprite;
                    }
                }
            }

            return _defaultPortrait;
        }
    }
}

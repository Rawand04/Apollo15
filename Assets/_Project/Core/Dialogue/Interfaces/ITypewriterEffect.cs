using System;
using TMPro;
using UnityEngine;

namespace SpaceSeries.Core.Dialogue
{
    /// <summary>
    /// Contract for typewriter text animation on TextMeshPro components.
    /// Supports skip functionality, punctuation pauses, and audio blips.
    /// </summary>
    public interface ITypewriterEffect
    {
        bool IsTyping { get; }

        void StartTyping(
            TextMeshProUGUI textComponent,
            string fullText,
            float charactersPerSecond,
            AudioClip audioBlip,
            float pitch,
            Action onComplete);

        void SkipTyping();
        void StopTyping();
    }
}

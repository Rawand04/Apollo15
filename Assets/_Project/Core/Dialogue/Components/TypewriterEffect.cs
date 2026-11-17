using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace SpaceSeries.Core.Dialogue
{
    /// <summary>
    /// High-performance typewriter text effect using TextMeshPro's maxVisibleCharacters property.
    /// Preserves Rich Text tags without breaking formatting or allocating new strings.
    /// Supports punctuation rhythm pauses, randomized audio blips, and instant skipping.
    /// </summary>
    public class TypewriterEffect : MonoBehaviour, ITypewriterEffect
    {
        [Header("Audio Configuration")]
        [Tooltip("AudioSource used to play typing blips. If null, searched on this GameObject.")]
        [SerializeField] private AudioSource _audioSource;

        [Tooltip("Play typing sound every N characters to avoid audio clutter.")]
        [Range(1, 5)]
        [SerializeField] private int _audioCharacterInterval = 2;

        [Header("Punctuation Pauses (seconds)")]
        [Tooltip("Pause added after commas, colons, and semicolons.")]
        [SerializeField] private float _commaPause = 0.12f;

        [Tooltip("Pause added after periods, exclamation points, and question marks.")]
        [SerializeField] private float _sentencePause = 0.28f;

        private Coroutine _typingCoroutine;
        private TextMeshProUGUI _activeTextComponent;
        private Action _onCompleteCallback;
        private bool _isTyping;

        public bool IsTyping => _isTyping;

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        public void StartTyping(
            TextMeshProUGUI textComponent,
            string fullText,
            float charactersPerSecond,
            AudioClip audioBlip,
            float pitch,
            Action onComplete)
        {
            StopTyping();

            _activeTextComponent = textComponent;
            _onCompleteCallback = onComplete;

            if (_activeTextComponent == null)
            {
                _onCompleteCallback?.Invoke();
                return;
            }

            _typingCoroutine = StartCoroutine(TypeRoutine(fullText, charactersPerSecond, audioBlip, pitch));
        }

        private IEnumerator TypeRoutine(string fullText, float charactersPerSecond, AudioClip audioBlip, float pitch)
        {
            _isTyping = true;

            // Set full text into TMP so layout and rich text tags are pre-parsed
            _activeTextComponent.text = fullText;
            _activeTextComponent.maxVisibleCharacters = 0;
            _activeTextComponent.ForceMeshUpdate();

            TMP_TextInfo textInfo = _activeTextComponent.textInfo;
            int totalCharacters = textInfo.characterCount;

            if (totalCharacters == 0)
            {
                _isTyping = false;
                _onCompleteCallback?.Invoke();
                yield break;
            }

            float delayPerChar = 1f / Mathf.Max(charactersPerSecond, 1f);
            int visibleCount = 0;

            if (_audioSource != null && audioBlip != null)
            {
                _audioSource.pitch = pitch;
            }

            while (visibleCount < totalCharacters)
            {
                visibleCount++;
                _activeTextComponent.maxVisibleCharacters = visibleCount;

                // Play typing sound periodically
                if (audioBlip != null && _audioSource != null && (visibleCount % _audioCharacterInterval == 0))
                {
                    _audioSource.pitch = pitch + UnityEngine.Random.Range(-0.04f, 0.04f);
                    _audioSource.PlayOneShot(audioBlip, 0.6f);
                }

                // Check for punctuation pauses
                char currentChar = textInfo.characterInfo[visibleCount - 1].character;
                float extraPause = GetPunctuationPause(currentChar);

                if (extraPause > 0f)
                {
                    yield return new WaitForSeconds(extraPause);
                }
                else
                {
                    yield return new WaitForSeconds(delayPerChar);
                }
            }

            _isTyping = false;
            _typingCoroutine = null;
            _onCompleteCallback?.Invoke();
        }

        private float GetPunctuationPause(char c)
        {
            switch (c)
            {
                case ',':
                case ';':
                case ':':
                    return _commaPause;
                case '.':
                case '!':
                case '?':
                    return _sentencePause;
                default:
                    return 0f;
            }
        }

        public void SkipTyping()
        {
            if (!_isTyping)
                return;

            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }

            if (_activeTextComponent != null)
            {
                _activeTextComponent.maxVisibleCharacters = _activeTextComponent.textInfo.characterCount;
            }

            _isTyping = false;
            _onCompleteCallback?.Invoke();
        }

        public void StopTyping()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }

            _isTyping = false;
        }
    }
}

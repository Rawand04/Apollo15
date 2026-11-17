using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSeries.Core.Dialogue
{
    /// <summary>
    /// View component responsible for rendering dialogue on the astronaut visor HUD or comms monitor.
    /// Drives portraits, speaker names, titles, typewriter text, and continue prompts.
    /// </summary>
    [RequireComponent(typeof(TypewriterEffect))]
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI Visual Elements")]
        [Tooltip("Text component displaying the speaker's name.")]
        [SerializeField] private TextMeshProUGUI _speakerNameText;

        [Tooltip("Optional text component displaying the speaker's rank/title/role.")]
        [SerializeField] private TextMeshProUGUI _speakerTitleText;

        [Tooltip("Image component displaying the speaker's portrait.")]
        [SerializeField] private Image _portraitImage;

        [Tooltip("Text component displaying the typed dialogue line.")]
        [SerializeField] private TextMeshProUGUI _dialogueText;

        [Tooltip("Visual indicator shown when typing finishes to prompt player to continue.")]
        [SerializeField] private GameObject _continueIndicator;

        [Tooltip("CanvasGroup controlling the dialogue box visibility and fade transitions.")]
        [SerializeField] private CanvasGroup _dialogueCanvasGroup;

        [Header("Audio")]
        [Tooltip("AudioSource used to play spoken voice clips.")]
        [SerializeField] private AudioSource _voiceAudioSource;

        [Header("Transition Settings")]
        [Tooltip("Fade speed for opening and closing the dialogue window.")]
        [SerializeField] private float _fadeSpeed = 8f;

        private TypewriterEffect _typewriter;
        private bool _isOpen;
        private float _targetAlpha;

        public bool IsTyping => _typewriter != null && _typewriter.IsTyping;

        private void Awake()
        {
            _typewriter = GetComponent<TypewriterEffect>();

            if (_dialogueCanvasGroup == null)
            {
                _dialogueCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (_voiceAudioSource == null)
            {
                _voiceAudioSource = GetComponent<AudioSource>();
            }

            SetVisibilityImmediate(false);
        }

        private void Update()
        {
            if (_dialogueCanvasGroup != null)
            {
                _dialogueCanvasGroup.alpha = Mathf.MoveTowards(_dialogueCanvasGroup.alpha, _targetAlpha, _fadeSpeed * Time.deltaTime);

                if (_dialogueCanvasGroup.alpha <= 0.001f && !_isOpen && gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void Show()
        {
            _isOpen = true;
            _targetAlpha = 1f;
            gameObject.SetActive(true);

            if (_dialogueCanvasGroup != null)
            {
                _dialogueCanvasGroup.interactable = true;
                _dialogueCanvasGroup.blocksRaycasts = true;
            }
        }

        public void Hide()
        {
            _isOpen = false;
            _targetAlpha = 0f;

            if (_continueIndicator != null)
                _continueIndicator.SetActive(false);

            if (_typewriter != null)
                _typewriter.StopTyping();

            if (_voiceAudioSource != null && _voiceAudioSource.isPlaying)
                _voiceAudioSource.Stop();

            if (_dialogueCanvasGroup != null)
            {
                _dialogueCanvasGroup.interactable = false;
                _dialogueCanvasGroup.blocksRaycasts = false;
            }
        }

        public void SetVisibilityImmediate(bool visible)
        {
            _isOpen = visible;
            _targetAlpha = visible ? 1f : 0f;

            if (_dialogueCanvasGroup != null)
            {
                _dialogueCanvasGroup.alpha = _targetAlpha;
                _dialogueCanvasGroup.interactable = visible;
                _dialogueCanvasGroup.blocksRaycasts = visible;
            }

            if (!visible)
            {
                if (_continueIndicator != null)
                    _continueIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Populates and types out a new dialogue line.
        /// </summary>
        public void DisplayLine(DialogueLine line, Action onTypingComplete)
        {
            Show();

            if (_continueIndicator != null)
            {
                _continueIndicator.SetActive(false);
            }

            // 1. Set Speaker Name & Color
            if (_speakerNameText != null)
            {
                if (line.Speaker != null)
                {
                    _speakerNameText.text = line.Speaker.SpeakerName;
                    _speakerNameText.color = line.Speaker.NameColor;
                }
                else
                {
                    _speakerNameText.text = string.Empty;
                }
            }

            // 2. Set Speaker Title
            if (_speakerTitleText != null)
            {
                _speakerTitleText.text = line.Speaker != null ? line.Speaker.Title : string.Empty;
                _speakerTitleText.gameObject.SetActive(!string.IsNullOrEmpty(_speakerTitleText.text));
            }

            // 3. Set Portrait
            if (_portraitImage != null)
            {
                Sprite portrait = line.ResolvePortrait();
                if (portrait != null)
                {
                    _portraitImage.sprite = portrait;
                    _portraitImage.gameObject.SetActive(true);
                }
                else
                {
                    _portraitImage.gameObject.SetActive(false);
                }
            }

            // 4. Play Spoken Voice Clip (Archival transmission)
            if (_voiceAudioSource != null && line.VoiceClip != null)
            {
                _voiceAudioSource.Stop();
                _voiceAudioSource.clip = line.VoiceClip;
                _voiceAudioSource.Play();
            }

            // 5. Begin Typewriter Effect
            AudioClip audioBlip = line.Speaker != null ? line.Speaker.TypewriterAudioBlip : null;
            float pitch = line.Speaker != null ? line.Speaker.VoicePitch : 1.0f;

            _typewriter.StartTyping(
                _dialogueText,
                line.Text,
                line.CharactersPerSecond,
                audioBlip,
                pitch,
                () =>
                {
                    if (_continueIndicator != null)
                    {
                        _continueIndicator.SetActive(true);
                    }
                    onTypingComplete?.Invoke();
                });
        }

        public void SkipTyping()
        {
            _typewriter?.SkipTyping();
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSeries.Core.Interaction
{
    /// <summary>
    /// Event-driven UI controller for displaying interaction prompts on the astronaut's visor HUD.
    /// Completely decoupled: only listens to InteractionPromptEventChannelSO.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("Event Channel")]
        [Tooltip("The prompt channel to listen to.")]
        [SerializeField] private InteractionPromptEventChannelSO _promptChannel;

        [Header("UI Elements")]
        [Tooltip("TextMeshPro component displaying the action prompt (e.g. '[E] Deploy ALSEP Station').")]
        [SerializeField] private TextMeshProUGUI _promptText;

        [Tooltip("Optional CanvasGroup for fading the prompt in and out.")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Tooltip("Optional root GameObject for the prompt panel.")]
        [SerializeField] private GameObject _promptPanel;

        [Header("Animation")]
        [Tooltip("Speed of the fade in/out animation.")]
        [SerializeField] private float _fadeSpeed = 10f;

        private bool _isTargetVisible;
        private float _targetAlpha;

        private void Awake()
        {
            if (_promptPanel == null && _canvasGroup != null)
            {
                _promptPanel = _canvasGroup.gameObject;
            }

            if (_promptPanel != null)
            {
                _promptPanel.SetActive(false);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        private void OnEnable()
        {
            if (_promptChannel != null)
            {
                _promptChannel.OnEventRaised += HandlePromptChanged;
            }
        }

        private void OnDisable()
        {
            if (_promptChannel != null)
            {
                _promptChannel.OnEventRaised -= HandlePromptChanged;
            }
        }

        private void Update()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, _fadeSpeed * Time.deltaTime);

                if (_promptPanel != null && _canvasGroup.alpha <= 0.01f && !_isTargetVisible)
                {
                    _promptPanel.SetActive(false);
                }
            }
        }

        private void HandlePromptChanged(InteractionPromptData promptData)
        {
            _isTargetVisible = promptData.IsVisible;
            _targetAlpha = _isTargetVisible ? 1f : 0f;

            if (_isTargetVisible)
            {
                if (_promptText != null)
                {
                    _promptText.text = promptData.FormattedPrompt;
                }

                if (_promptPanel != null && !_promptPanel.activeSelf)
                {
                    _promptPanel.SetActive(true);
                }
            }
            else
            {
                if (_canvasGroup == null && _promptPanel != null)
                {
                    _promptPanel.SetActive(false);
                }
            }
        }
    }
}

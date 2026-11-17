using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSeries.Core.Missions
{
    /// <summary>
    /// Event-driven Visor HUD component displaying the current active Apollo flight plan objective.
    /// Features completion flash animations, checklist counters, and smooth fades.
    /// </summary>
    public class CurrentObjectiveUI : MonoBehaviour
    {
        [Header("Event Channel")]
        [Tooltip("The quest system event channel to listen to.")]
        [SerializeField] private QuestSystemEventChannelSO _questChannel;

        [Header("UI Visual Elements")]
        [Tooltip("Header text displaying the active flight plan title (e.g. 'FLIGHT PLAN: APOLLO 15 - EVA 1').")]
        [SerializeField] private TextMeshProUGUI _flightPlanHeaderText;

        [Tooltip("Main title text displaying the active objective headline.")]
        [SerializeField] private TextMeshProUGUI _objectiveTitleText;

        [Tooltip("Optional text displaying detailed checklist instructions.")]
        [SerializeField] private TextMeshProUGUI _objectiveDescriptionText;

        [Tooltip("Progress counter text (e.g., '[1/3]').")]
        [SerializeField] private TextMeshProUGUI _progressCounterText;

        [Tooltip("Checkmark or status icon toggled when an objective is accomplished.")]
        [SerializeField] private Image _statusIcon;

        [Tooltip("Optional panel CanvasGroup for fading.")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Visual Feedback Settings")]
        [Tooltip("Sprite shown when objective is active/incomplete.")]
        [SerializeField] private Sprite _activeObjectiveIcon;

        [Tooltip("Sprite shown when objective is accomplished.")]
        [SerializeField] private Sprite _completedObjectiveIcon;

        [Tooltip("Highlight flash color on completion.")]
        [SerializeField] private Color _completionFlashColor = Color.green;

        private Color _originalTitleColor = Color.white;
        private Coroutine _flashCoroutine;

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_objectiveTitleText != null)
            {
                _originalTitleColor = _objectiveTitleText.color;
            }

            SetVisibility(false);
        }

        private void OnEnable()
        {
            if (_questChannel != null)
            {
                _questChannel.OnQuestStarted += HandleQuestStarted;
                _questChannel.OnObjectiveUpdated += HandleObjectiveUpdated;
                _questChannel.OnObjectiveCompleted += HandleObjectiveCompleted;
                _questChannel.OnQuestCompleted += HandleQuestCompleted;
            }
        }

        private void OnDisable()
        {
            if (_questChannel != null)
            {
                _questChannel.OnQuestStarted -= HandleQuestStarted;
                _questChannel.OnObjectiveUpdated -= HandleObjectiveUpdated;
                _questChannel.OnObjectiveCompleted -= HandleObjectiveCompleted;
                _questChannel.OnQuestCompleted -= HandleQuestCompleted;
            }
        }

        private void HandleQuestStarted(QuestRuntimeState state)
        {
            SetVisibility(true);

            if (_flightPlanHeaderText != null && state.Data != null)
            {
                _flightPlanHeaderText.text = state.Data.FlightPlanTitle.ToUpperInvariant();
            }

            if (state.CurrentObjective != null)
            {
                UpdateObjectiveDisplay(state.CurrentObjective);
            }
        }

        private void HandleObjectiveUpdated(ObjectiveRuntimeState objective)
        {
            SetVisibility(true);
            UpdateObjectiveDisplay(objective);
        }

        private void HandleObjectiveCompleted(ObjectiveRuntimeState objective)
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }

            _flashCoroutine = StartCoroutine(CompletionFlashRoutine(objective));
        }

        private void HandleQuestCompleted(QuestRuntimeState state)
        {
            if (_objectiveTitleText != null)
            {
                _objectiveTitleText.text = "EVA FLIGHT PLAN ACCOMPLISHED";
                _objectiveTitleText.color = _completionFlashColor;
            }

            if (_progressCounterText != null)
            {
                _progressCounterText.text = string.Empty;
            }

            if (_statusIcon != null && _completedObjectiveIcon != null)
            {
                _statusIcon.sprite = _completedObjectiveIcon;
            }
        }

        private void UpdateObjectiveDisplay(ObjectiveRuntimeState objective)
        {
            if (objective == null || objective.Data == null)
                return;

            if (_objectiveTitleText != null)
            {
                _objectiveTitleText.text = objective.Data.Title;
                _objectiveTitleText.color = _originalTitleColor;
            }

            if (_objectiveDescriptionText != null)
            {
                _objectiveDescriptionText.text = objective.Data.Description;
            }

            if (_progressCounterText != null)
            {
                if (objective.Data.TargetAmount > 1)
                {
                    _progressCounterText.text = $"[{objective.CurrentAmount}/{objective.Data.TargetAmount}]";
                }
                else
                {
                    _progressCounterText.text = string.Empty;
                }
            }

            if (_statusIcon != null)
            {
                _statusIcon.sprite = objective.IsCompleted ? _completedObjectiveIcon : _activeObjectiveIcon;
            }
        }

        private IEnumerator CompletionFlashRoutine(ObjectiveRuntimeState objective)
        {
            if (_statusIcon != null && _completedObjectiveIcon != null)
            {
                _statusIcon.sprite = _completedObjectiveIcon;
            }

            if (_objectiveTitleText != null)
            {
                _objectiveTitleText.color = _completionFlashColor;
                _objectiveTitleText.text = $"✓ {objective.Data.Title}";
            }

            yield return new WaitForSeconds(1.8f);

            _flashCoroutine = null;
        }

        private void SetVisibility(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
        }
    }
}

using SpaceSeries.Core.Interaction;
using UnityEngine;

namespace SpaceSeries.Core.Missions
{
    /// <summary>
    /// Master manager for Apollo missions and flight plan execution.
    /// Evaluates objective progression, listens to interaction/dialogue quest triggers,
    /// and broadcasts updates to UI and game systems.
    /// </summary>
    public class MissionManager : MonoBehaviour, IQuestManager
    {
        [Header("Configuration")]
        [Tooltip("Optional initial flight plan to start upon scene load (e.g., Apollo15_EVA1).")]
        [SerializeField] private QuestDataSO _defaultStartingFlightPlan;

        [Header("Event Channels")]
        [Tooltip("Event channel for high-level quest lifecycle broadcasts.")]
        [SerializeField] private QuestSystemEventChannelSO _questSystemChannel;

        [Tooltip("Event channel listening for interaction and dialogue triggers.")]
        [SerializeField] private QuestTriggerEventChannelSO _incomingTriggerChannel;

        [Header("Audio Feedback")]
        [Tooltip("AudioSource used for telemetry and mission chimes.")]
        [SerializeField] private AudioSource _audioSource;

        private QuestRuntimeState _activeQuest;

        // IQuestManager Properties
        public QuestRuntimeState ActiveQuest => _activeQuest;
        public ObjectiveRuntimeState CurrentObjective => _activeQuest != null ? _activeQuest.CurrentObjective : null;
        public bool IsQuestActive => _activeQuest != null && !_activeQuest.IsCompleted;

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        private void OnEnable()
        {
            if (_questSystemChannel != null)
            {
                _questSystemChannel.OnStartQuestRequested += StartQuest;
            }

            if (_incomingTriggerChannel != null)
            {
                _incomingTriggerChannel.OnEventRaised += HandleIncomingTrigger;
            }
        }

        private void OnDisable()
        {
            if (_questSystemChannel != null)
            {
                _questSystemChannel.OnStartQuestRequested -= StartQuest;
            }

            if (_incomingTriggerChannel != null)
            {
                _incomingTriggerChannel.OnEventRaised -= HandleIncomingTrigger;
            }
        }

        private void Start()
        {
            if (_activeQuest == null && _defaultStartingFlightPlan != null)
            {
                StartQuest(_defaultStartingFlightPlan);
            }
        }

        public void StartQuest(QuestDataSO questData)
        {
            if (questData == null)
                return;

            _activeQuest = new QuestRuntimeState(questData);
            _questSystemChannel?.RaiseQuestStarted(_activeQuest);

            if (_activeQuest.CurrentObjective != null)
            {
                _questSystemChannel?.RaiseObjectiveUpdated(_activeQuest.CurrentObjective);
            }
        }

        public void AdvanceProgress(string objectiveId, int delta)
        {
            if (!IsQuestActive || string.IsNullOrEmpty(objectiveId))
                return;

            ObjectiveRuntimeState targetObjective = ResolveTargetObjective(objectiveId);
            if (targetObjective == null || targetObjective.IsCompleted)
                return;

            bool completed = targetObjective.AddProgress(delta);

            if (completed)
            {
                OnObjectiveCompleted(targetObjective);
            }
            else
            {
                _questSystemChannel?.RaiseObjectiveUpdated(targetObjective);
            }
        }

        public void CompleteObjective(string objectiveId)
        {
            if (!IsQuestActive || string.IsNullOrEmpty(objectiveId))
                return;

            ObjectiveRuntimeState targetObjective = ResolveTargetObjective(objectiveId);
            if (targetObjective == null || targetObjective.IsCompleted)
                return;

            targetObjective.ForceComplete();
            OnObjectiveCompleted(targetObjective);
        }

        private void OnObjectiveCompleted(ObjectiveRuntimeState objective)
        {
            // Play telemetry completion chime
            if (_audioSource != null && objective.Data.CompletionChime != null)
            {
                _audioSource.PlayOneShot(objective.Data.CompletionChime);
            }

            _questSystemChannel?.RaiseObjectiveCompleted(objective);

            // In sequential mode, advance to next objective in checklist
            if (_activeQuest.Data.RequireSequential)
            {
                _activeQuest.AdvanceToNextObjective();
            }

            // Check if full mission flight plan is accomplished
            if (_activeQuest.CheckAllObjectivesCompleted())
            {
                CompleteMission();
            }
            else if (_activeQuest.CurrentObjective != null)
            {
                _questSystemChannel?.RaiseObjectiveUpdated(_activeQuest.CurrentObjective);
            }
        }

        private void CompleteMission()
        {
            if (_activeQuest == null)
                return;

            if (_audioSource != null && _activeQuest.Data.MissionCompleteChime != null)
            {
                _audioSource.PlayOneShot(_activeQuest.Data.MissionCompleteChime);
            }

            _questSystemChannel?.RaiseQuestCompleted(_activeQuest);

            // Handle transition to next flight plan (e.g. EVA-1 -> EVA-2)
            QuestDataSO nextPlan = _activeQuest.Data.NextFlightPlan;
            if (nextPlan != null)
            {
                StartQuest(nextPlan);
            }
        }

        public void AbandonQuest()
        {
            _activeQuest = null;
        }

        private ObjectiveRuntimeState ResolveTargetObjective(string objectiveId)
        {
            if (_activeQuest == null)
                return null;

            // In sequential mode, verify if this matches the current active step or open checklist
            if (_activeQuest.Data.RequireSequential)
            {
                if (_activeQuest.CurrentObjective != null && _activeQuest.CurrentObjective.Data.ObjectiveId == objectiveId)
                {
                    return _activeQuest.CurrentObjective;
                }
            }

            return _activeQuest.FindObjectiveState(objectiveId);
        }

        private void HandleIncomingTrigger(QuestTriggerPayload payload)
        {
            if (!IsQuestActive || !payload.HasContent)
                return;

            // Verify mission ID match if specified in payload
            if (!string.IsNullOrEmpty(payload.QuestId) && payload.QuestId != _activeQuest.Data.QuestId)
                return;

            if (payload.IsCompletingStep)
            {
                CompleteObjective(payload.ObjectiveId);
            }
            else
            {
                AdvanceProgress(payload.ObjectiveId, payload.ProgressDelta > 0 ? payload.ProgressDelta : 1);
            }
        }
    }
}

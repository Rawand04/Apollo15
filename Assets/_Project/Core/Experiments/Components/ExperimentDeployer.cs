using System;
using UnityEngine;
using SpaceSeries.Core.Interaction;
using SpaceSeries.Core.Dialogue;
using SpaceSeries.Core.Missions;
using SpaceSeries.Core.Player;

namespace SpaceSeries.Core.Experiments
{
    [DisallowMultipleComponent]
    public sealed class ExperimentDeployer : MonoBehaviour, IInteractor, IExperimentDeploymentRequester
    {
        [Header("Actor and Zones")]
        [SerializeField] private AstronautController _astronaut;
        [SerializeField] private ExperimentDeploymentZone[] _zones;
        [SerializeField] private UnityEngine.InputSystem.InputActionReference _deployAction;
        [Tooltip("Let a scene interaction router own E and prompts; standalone behavior remains the default.")]
        [SerializeField] private bool _externalInteraction;
        public ExperimentDeploymentZone GetEligibleZone() { RefreshTarget(); return _selected; }
        [SerializeField] private MonoBehaviour _animationPlayer;
        [SerializeField] private Transform _deployedRoot;
        [Header("Scene Services")]
[SerializeField] private MissionManager _missions;
        [SerializeField] private DialogueRunner _dialogueRunner;
        [SerializeField] private DialogueChannelSO _dialogueChannel;
        [SerializeField] private QuestTriggerEventChannelSO _questChannel;
        [SerializeField] private InteractionPromptEventChannelSO _promptChannel;
        [SerializeField, Min(1f)] private float _animationTimeout = 15f;
        private UnityEngine.InputSystem.InputAction _runtimeAction;
        private IDeploymentAnimationPlayer _animation;
        private ExperimentDeploymentZone _selected;
        private ExperimentDeploymentZone _reserved;
        private int _runId;
        private float _deadline;
        private bool _controlActive = true;
        private bool _focused = true;
        private bool _restoreMovement;
        private string _lastError;
        private bool _reservationActive;
        public bool IsDeploying => _reservationActive;
        public bool IsInputLocked => IsDeploying || (_dialogueRunner != null && _dialogueRunner.IsDialogueActive);
public bool ControlActive => _controlActive;
        public string LastFailure => _lastError;
        public event Action<ExperimentDeploymentZone, string> StageChanged;
        public Transform InteractorTransform => transform;
        public GameObject InteractorGameObject => gameObject;
        public IInteractable CurrentTarget => _selected;
        private void Awake()
        {
            _animation = _animationPlayer as IDeploymentAnimationPlayer;
            if (!_externalInteraction && _deployAction != null && _deployAction.action != null)
            { _runtimeAction = _deployAction.action.Clone(); _runtimeAction.performed += DeployPerformed; }
}
        private void OnEnable() { UpdateInput(); }
        private void OnDisable()
        {
            _runtimeAction?.Disable(); CancelDeployment();
            if (_dialogueRunner != null && _dialogueRunner.IsDialogueActive) _dialogueRunner.CancelDialogue();
            _promptChannel?.HidePrompt();
        }
        private void OnDestroy() { _runtimeAction?.Dispose(); }
        private void OnApplicationFocus(bool focused)
        {
            _focused = focused;
            if (!focused)
            { CancelDeployment(); if (_dialogueRunner != null && _dialogueRunner.IsDialogueActive) _dialogueRunner.CancelDialogue(); }
            UpdateInput();
        }
        public void SetControlActive(bool active)
        {
            if (_controlActive == active) return;
            if (!active) { CancelDeployment(); _restoreMovement = false; _promptChannel?.HidePrompt(); }
            _controlActive = active; UpdateInput();
        }
        private void UpdateInput()
        {
            if (_runtimeAction == null) return;
            if (_controlActive && _focused && isActiveAndEnabled) _runtimeAction.Enable(); else _runtimeAction.Disable();
        }
        private void Update()
        {
            if (_reservationActive && (_reserved == null || !_reserved.Contains(this) || !_reserved.isActiveAndEnabled ||
                _animationPlayer == null || !_animationPlayer.isActiveAndEnabled || Time.unscaledTime >= _deadline))
            { Fail("Deployment interrupted or animation completion timed out."); CancelDeployment(); }
if (_restoreMovement && !IsInputLocked) RestoreMovement();
            RefreshTarget();
        }
        private void DeployPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        { RefreshTarget(); if (_selected != null) TryDeploy(_selected); }
        private void RefreshTarget()
        {
            ExperimentDeploymentZone next = null;
            float closest = float.MaxValue;
            if (_controlActive && _focused && !IsInputLocked && _zones != null)
            foreach (var zone in _zones)
            {
                if (zone == null || !zone.CanInteract(this)) continue;
                float distance = (zone.InteractionPosition - transform.position).sqrMagnitude;
                // Equal priority/distance keeps the first serialized zone, a stable authoring tie-break.
                if (next == null || zone.Priority > next.Priority || (zone.Priority == next.Priority && distance < closest))
                { next = zone; closest = distance; }
}
            if (next == _selected) return;
            _selected = next;
            if (_selected == null) _promptChannel?.HidePrompt();
            else _promptChannel?.ShowPrompt(_selected.PromptText, "Deploy", "E");
        }
        public bool RequestDeployment(IInteractable zone) => zone is ExperimentDeploymentZone deploymentZone && TryDeploy(deploymentZone);
        public bool TryDeploy(ExperimentDeploymentZone zone)
        {
            if (!isActiveAndEnabled || !_controlActive || !_focused || IsInputLocked || zone == null || !zone.CanInteract(this)) return false;
            if (!Validate(zone, out _, out string error)) { Fail(error); return false; }
            if (_animation == null || !_animation.IsReady) { Fail("Placement animation adapter is missing or not ready."); return false; }
            if (!zone.TryReserve(this)) return false;
            _reserved = zone; _reservationActive = true;
            _runId = _runId == int.MaxValue ? 1 : _runId + 1;
_deadline = Time.unscaledTime + _animationTimeout;
            _restoreMovement = _astronaut != null && _astronaut.IsLocomotionActive;
            if (_astronaut != null) _astronaut.SetLocomotionActive(false);
            _promptChannel?.HidePrompt();
            try
            {
                if (!_animation.TryPlay(zone.SpawnAnchor, _runId, AnimationCompleted))
                { Fail("Placement animation failed to start."); CancelDeployment(); return false; }
                StageChanged?.Invoke(zone, "AnimationStarted");
                return true;
            }
            catch (Exception exception) { Fail(exception.Message); CancelDeployment(); return false; }
        }
        private bool Validate(ExperimentDeploymentZone zone, out ObjectiveRuntimeState objective, out string error)
        {
            objective = null; error = null;
            if (zone == null || zone.Definition == null || !zone.Definition.IsValid || zone.SpawnAnchor == null)
            { error = "Experiment definition, prefab, dialogue, IDs or spawn anchor is invalid."; return false; }
            if (_deployedRoot == null || _dialogueChannel == null || _questChannel == null || _missions == null || !_missions.isActiveAndEnabled ||
                _dialogueRunner == null || !_dialogueRunner.isActiveAndEnabled || !_dialogueRunner.IsConfigured || _dialogueRunner.IsDialogueActive)
            { error = "Deployment scene services are missing, disabled or busy."; return false; }
            var quest = _missions.ActiveQuest;
            if (quest == null || quest.IsCompleted || quest.Data.QuestId != zone.Definition.QuestId)
            { error = "Experiment quest does not match the active mission."; return false; }
            objective = quest.FindObjectiveState(zone.Definition.ObjectiveId);
            if (objective == null || objective.IsCompleted || (quest.Data.RequireSequential && quest.CurrentObjective != objective))
            { error = "Experiment objective is absent, completed or not the current sequential step."; return false; }
            if (!zone.IsPlacementClear()) { error = "Experiment spawn area is obstructed."; return false; }
            return true;
        }
        private void AnimationCompleted(int runId)
        {
            var zone = _reserved;
            if (runId != _runId || zone == null) return;
            StageChanged?.Invoke(zone, "AnimationCompleted");
            if (!isActiveAndEnabled || !_controlActive || !_focused || !zone.Contains(this) || !Validate(zone, out var objective, out var error))
            { Fail("Deployment no longer eligible at animation completion."); CancelDeployment(); return; }
            GameObject instance = null;
            try
            {
                instance = Instantiate(zone.Definition.Prefab, zone.SpawnAnchor.TransformPoint(zone.Definition.SpawnLocalOffset), zone.SpawnAnchor.rotation, _deployedRoot);
                if (!zone.Commit(this, instance)) { Destroy(instance); CancelDeployment(); return; }
                _reserved = null; _reservationActive = false;
                StageChanged?.Invoke(zone, "Spawned");
_dialogueChannel.RequestDialogue(zone.Definition.Dialogue);
                if (!_dialogueRunner.IsDialogueActive || _dialogueRunner.CurrentSequence != zone.Definition.Dialogue)
                    throw new InvalidOperationException("Equipment spawned but dialogue request was not accepted. Check channel wiring.");
                StageChanged?.Invoke(zone, "DialogueStarted");
                _questChannel.RaiseEvent(new QuestTriggerPayload { QuestId = zone.Definition.QuestId, ObjectiveId = zone.Definition.ObjectiveId, IsCompletingStep = true, ProgressDelta = 1 });
                if (!objective.IsCompleted) throw new InvalidOperationException("Equipment spawned but objective did not complete. Check incoming quest channel wiring.");
                StageChanged?.Invoke(zone, "ObjectiveCompleted");
                _lastError = null;
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
                if (instance == null) CancelDeployment();
                else if (zone.State != ExperimentDeploymentZone.DeploymentState.Deployed) { Destroy(instance); CancelDeployment(); }
            }
            RefreshTarget();
        }
        public void CancelDeployment()
        {
            _animation?.Cancel(_runId);
            var zone = _reserved;
            _reserved = null; _reservationActive = false;
            if (zone != null) { zone.Release(this); StageChanged?.Invoke(zone, "Cancelled"); }
if (!IsInputLocked) RestoreMovement();
        }
        private void RestoreMovement()
        {
            if (_restoreMovement && _controlActive && _focused && isActiveAndEnabled && _astronaut != null && _astronaut.isActiveAndEnabled)
                _astronaut.SetLocomotionActive(true);
            if (_controlActive && _focused && isActiveAndEnabled) _restoreMovement = false;
        }
        private void Fail(string error)
        {
            if (_lastError != error) Debug.LogWarning("[ExperimentDeployer] " + error, this);
            _lastError = error;
        }
        public void TriggerInteractionAnimation(string triggerParamName) { }
    }
}

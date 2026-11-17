using System;
using UnityEngine;

namespace SpaceSeries.Core.Experiments
{
    public sealed class EquipmentPlacementAnimationPlayer : MonoBehaviour, IDeploymentAnimationPlayer
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private DeploymentAnimationRelay _relay;
        [SerializeField] private AnimationClip _placementClip;
        [SerializeField] private GameObject _visuals;
        [SerializeField] private string _idleState = "Base Layer.Idle";
        [SerializeField] private string _placementState = "Base Layer.Place";
        private Action<int> _completed;
        private int _runId;
        public bool IsReady
        {
            get
            {
                if (!isActiveAndEnabled || _animator == null || !_animator.isActiveAndEnabled || _relay == null || !_relay.isActiveAndEnabled ||
                    _animator.runtimeAnimatorController == null || _placementClip == null || _animator.speed <= 0f || _runId != 0) return false;
                bool hasEvent = false;
                foreach (var e in _placementClip.events) if (e.functionName == nameof(DeploymentAnimationRelay.PlacementFinished)) hasEvent = true;
                return hasEvent && _animator.HasState(0, Animator.StringToHash(_placementState)) && _animator.HasState(0, Animator.StringToHash(_idleState));
            }
        }
        private void OnEnable() { if (_relay != null) _relay.Completed += HandleCompleted; if (_visuals != null) _visuals.SetActive(false); }
        private void OnDisable() { Cancel(_runId); if (_relay != null) _relay.Completed -= HandleCompleted; }
        public bool TryPlay(Transform anchor, int runId, Action<int> completed)
        {
            if (!IsReady || anchor == null || runId == 0 || completed == null) return false;
            ResetAnimator();
            transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            _runId = runId; _completed = completed;
            if (_visuals != null) _visuals.SetActive(true);
            _relay.Arm(runId);
            _animator.Play(Animator.StringToHash(_placementState), 0, 0f);
            return true;
        }
        private void HandleCompleted(int runId, AnimationEvent e)
        {
            if (runId != _runId || _runId == 0 || e == null || e.animatorClipInfo.clip != _placementClip ||
                e.animatorStateInfo.fullPathHash != Animator.StringToHash(_placementState)) return;
            var callback = _completed;
            _completed = null; _runId = 0;
            if (_visuals != null) _visuals.SetActive(false);
            callback?.Invoke(runId);
        }
        public void Cancel(int runId)
        {
            if (runId == 0 || runId != _runId) return;
            _relay?.Disarm(); _runId = 0; _completed = null;
            ResetAnimator();
            if (_visuals != null) _visuals.SetActive(false);
        }
        private void ResetAnimator()
        {
            if (_animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null)
            { _animator.Play(Animator.StringToHash(_idleState), 0, 0f); _animator.Update(0f); }
        }
    }
}

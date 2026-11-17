using System;
using UnityEngine;

namespace SpaceSeries.Core.Experiments
{
    public sealed class DeploymentAnimationRelay : MonoBehaviour
    {
        public event Action<int, AnimationEvent> Completed;
        private int _runId;
        public void Arm(int runId) { _runId = runId; }
        public void Disarm() { _runId = 0; }
        public void PlacementFinished(AnimationEvent animationEvent)
        {
            int runId = _runId;
            if (runId == 0) return;
            _runId = 0;
            Completed?.Invoke(runId, animationEvent);
        }
    }
}

using System;
using UnityEngine;

namespace SpaceSeries.Core.Experiments
{
    public interface IExperimentDeploymentRequester
    {
        bool RequestDeployment(SpaceSeries.Core.Interaction.IInteractable zone);
    }

    public interface IDeploymentAnimationPlayer
    {
        bool IsReady { get; }
bool TryPlay(Transform anchor, int runId, Action<int> completed);
        void Cancel(int runId);
    }
}

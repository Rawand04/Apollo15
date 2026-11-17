using SpaceSeries.Core.Interaction;
using UnityEngine;

namespace SpaceSeries.Core.Missions
{
    /// <summary>
    /// Spatial trigger volume placed on Apollo geographical landmarks (e.g. Hadley Rille rim, Elbow Crater).
    /// Advances navigation or arrival flight plan objectives without requiring an inventory system.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WaypointTrigger : MonoBehaviour
    {
        [Header("Mission Objective Trigger")]
        [Tooltip("The parent flight plan ID (e.g. 'Apollo15_EVA1').")]
        [SerializeField] private string _questId = "Apollo15_EVA1";

        [Tooltip("The objective ID to advance or complete upon reaching this landmark.")]
        [SerializeField] private string _objectiveId = "Reach_HadleyRille_Rim";

        [Tooltip("Whether entering this trigger volume completely finishes the objective.")]
        [SerializeField] private bool _isCompletingStep = true;

        [Tooltip("Progress contribution amount if counting multiple waypoints.")]
        [SerializeField] private int _progressDelta = 1;

        [Header("Event Channel")]
        [Tooltip("Quest trigger event channel to notify.")]
        [SerializeField] private QuestTriggerEventChannelSO _questTriggerChannel;

        [Header("Trigger Settings")]
        [Tooltip("If true, this waypoint triggers only once.")]
        [SerializeField] private bool _oneShot = true;

        [Tooltip("Tag required to trigger (defaults to 'Player').")]
        [SerializeField] private string _requiredTag = "Player";

        private bool _hasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (_oneShot && _hasTriggered)
                return;

            if (other.CompareTag(_requiredTag) || other.transform.root.CompareTag(_requiredTag))
            {
                _hasTriggered = true;

                if (_questTriggerChannel != null)
                {
                    QuestTriggerPayload payload = new QuestTriggerPayload
                    {
                        QuestId = _questId,
                        ObjectiveId = _objectiveId,
                        TargetId = gameObject.name,
                        ProgressDelta = _progressDelta,
                        IsCompletingStep = _isCompletingStep
                    };

                    _questTriggerChannel.RaiseEvent(payload);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            if (TryGetComponent(out BoxCollider box))
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (TryGetComponent(out SphereCollider sphere))
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }
}

using UnityEngine;

namespace SpaceSeries.Core.Missions
{
    /// <summary>
    /// ScriptableObject definition of an individual Apollo flight plan objective.
    /// Pure data asset; runtime state is tracked separately to ensure immutability.
    /// </summary>
    [CreateAssetMenu(fileName = "Objective_New", menuName = "SpaceSeries/Missions/Objective Data")]
    public class ObjectiveDataSO : ScriptableObject
    {
        [Header("Objective Identity")]
        [Tooltip("Unique objective identifier (e.g. 'Deploy_PSEP_Station', 'Inspect_Falcon_Landing_Gear').")]
        [SerializeField] private string _objectiveId = "Objective_01";

        [Tooltip("Brief headline shown in the HUD checklist (e.g., 'Deploy Passive Seismic Experiment').")]
        [SerializeField] private string _title = "Deploy ALSEP";

        [Tooltip("Detailed NASA flight plan instructions.")]
        [TextArea(2, 4)]
        [SerializeField] private string _description = "Position the PSEP station 100 meters northwest of the Falcon LM and level the casing.";

        [Tooltip("Historical NASA context displayed in mission debriefs or encyclopedias.")]
        [TextArea(2, 4)]
        [SerializeField] private string _historicalNotes = "Apollo 15 ALSEP experiments were powered by the SNAP-27 radioisotope thermoelectric generator (RTG).";

        [Header("Progress Metrics")]
        [Tooltip("Target count required for completion (e.g., 1 for single deployment, 3 for core sample tubes).")]
        [Min(1)]
        [SerializeField] private int _targetAmount = 1;

        [Tooltip("Whether this objective is optional (secondary science objective).")]
        [SerializeField] private bool _isOptional = false;

        [Header("Waypoint / Spatial Target")]
        [Tooltip("Optional spatial identifier for compass and waypoint markers (e.g., 'HadleyRille_Rim', 'Falcon_LM').")]
        [SerializeField] private string _targetIdentifier = "Falcon_LM";

        [Header("Audio Feedback")]
        [Tooltip("Audio chime played upon objective accomplishment.")]
        [SerializeField] private AudioClip _completionChime;

        // Public Properties
        public string ObjectiveId => _objectiveId;
        public string Title => _title;
        public string Description => _description;
        public string HistoricalNotes => _historicalNotes;
        public int TargetAmount => _targetAmount;
        public bool IsOptional => _isOptional;
        public string TargetIdentifier => _targetIdentifier;
        public AudioClip CompletionChime => _completionChime;
    }
}

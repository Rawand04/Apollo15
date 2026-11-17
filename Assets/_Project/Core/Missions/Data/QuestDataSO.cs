using UnityEngine;

namespace SpaceSeries.Core.Missions
{
    /// <summary>
    /// ScriptableObject defining an entire Apollo EVA flight plan or planetary mission.
    /// Manages ordered objectives, flight plan metadata, and mission transitions.
    /// </summary>
    [CreateAssetMenu(fileName = "FlightPlan_New", menuName = "SpaceSeries/Missions/Flight Plan Data")]
    public class QuestDataSO : ScriptableObject
    {
        [Header("Flight Plan Identity")]
        [Tooltip("Unique mission identifier (e.g. 'Apollo15_EVA1', 'Apollo15_EVA2').")]
        [SerializeField] private string _questId = "Apollo15_EVA1";

        [Tooltip("Full NASA flight plan title.")]
        [SerializeField] private string _flightPlanTitle = "Apollo 15 - EVA 1";

        [Tooltip("Historical mission date.")]
        [SerializeField] private string _historicalDate = "July 31, 1971";

        [Tooltip("Mission crew assignments.")]
        [SerializeField] private string _crew = "Dave Scott (CDR), Jim Irwin (LMP)";

        [Tooltip("Mission briefing overview.")]
        [TextArea(3, 5)]
        [SerializeField] private string _briefing = "Egress the Falcon Lunar Module, inspect the landing gear, deploy the Lunar Roving Vehicle (LRV), and assemble the ALSEP scientific package on the Hadley Plain.";

        [Header("Flight Plan Objectives")]
        [Tooltip("Ordered list of mission objectives.")]
        [SerializeField] private ObjectiveDataSO[] _objectives;

        [Tooltip("If true, objectives must be completed in order. If false, objectives can be completed as an open checklist.")]
        [SerializeField] private bool _requireSequential = true;

        [Header("Chaining & Transitions")]
        [Tooltip("Optional subsequent flight plan to trigger after completing this mission (e.g. EVA-1 -> EVA-2).")]
        [SerializeField] private QuestDataSO _nextFlightPlan;

        [Tooltip("Audio chime played upon full mission completion.")]
        [SerializeField] private AudioClip _missionCompleteChime;

        // Public Properties
        public string QuestId => _questId;
        public string FlightPlanTitle => _flightPlanTitle;
        public string HistoricalDate => _historicalDate;
        public string Crew => _crew;
        public string Briefing => _briefing;
        public ObjectiveDataSO[] Objectives => _objectives;
        public int ObjectiveCount => _objectives != null ? _objectives.Length : 0;
        public bool RequireSequential => _requireSequential;
        public QuestDataSO NextFlightPlan => _nextFlightPlan;
        public AudioClip MissionCompleteChime => _missionCompleteChime;

        public ObjectiveDataSO GetObjective(int index)
        {
            if (_objectives != null && index >= 0 && index < _objectives.Length)
                return _objectives[index];

            return null;
        }
    }
}

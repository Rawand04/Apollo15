using UnityEngine;
using SpaceSeries.Core.Dialogue;

namespace SpaceSeries.Core.Experiments
{
    [CreateAssetMenu(fileName = "Experiment_New", menuName = "SpaceSeries/Experiments/Experiment Definition")]
    public sealed class ExperimentDefinitionSO : ScriptableObject
    {
        [Header("Experiment")]
        [SerializeField] private string _experimentName;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private DialogueSequenceSO _dialogue;
        [Tooltip("Offset in spawn-anchor coordinates to align a prefab's visible base without changing source geometry.")]
        [SerializeField] private Vector3 _spawnLocalOffset;
        public Vector3 SpawnLocalOffset => _spawnLocalOffset;
        [Header("Mission Mapping")]
[Tooltip("Parent quest ID, not the objective ID.")]
        [SerializeField] private string _questId;
        [SerializeField] private string _objectiveId;
        public string ExperimentName => _experimentName;
        public GameObject Prefab => _prefab;
        public DialogueSequenceSO Dialogue => _dialogue;
        public string QuestId => _questId;
        public string ObjectiveId => _objectiveId;
        public bool IsValid => !string.IsNullOrWhiteSpace(_experimentName) && _prefab != null &&
            _dialogue != null && _dialogue.LineCount > 0 && !string.IsNullOrWhiteSpace(_questId) &&
            !string.IsNullOrWhiteSpace(_objectiveId);
    }
}

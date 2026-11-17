namespace SpaceSeries.Core.Missions
{
    /// <summary>
    /// Contract for managing active NASA flight plans, evaluating progress, and advancing mission phases.
    /// </summary>
    public interface IQuestManager
    {
        QuestRuntimeState ActiveQuest { get; }
        ObjectiveRuntimeState CurrentObjective { get; }
        bool IsQuestActive { get; }

        void StartQuest(QuestDataSO questData);
        void AdvanceProgress(string objectiveId, int delta);
        void CompleteObjective(string objectiveId);
        void AbandonQuest();
    }
}

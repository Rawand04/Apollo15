using System;
using System.Collections.Generic;

namespace SpaceSeries.Core.Missions
{
    /// <summary>
    /// Runtime progress tracking for an individual objective.
    /// Keeps ScriptableObject assets immutable and prevents runtime state leakage.
    /// </summary>
    [Serializable]
    public class ObjectiveRuntimeState
    {
        public ObjectiveDataSO Data { get; private set; }
        public int CurrentAmount { get; private set; }
        public bool IsCompleted { get; private set; }

        public float ProgressNormalized => Data != null && Data.TargetAmount > 0
            ? (float)CurrentAmount / Data.TargetAmount
            : 0f;

        public ObjectiveRuntimeState(ObjectiveDataSO data)
        {
            Data = data;
            CurrentAmount = 0;
            IsCompleted = false;
        }

        public bool AddProgress(int delta)
        {
            if (IsCompleted || Data == null)
                return false;

            CurrentAmount += delta;
            if (CurrentAmount >= Data.TargetAmount)
            {
                CurrentAmount = Data.TargetAmount;
                IsCompleted = true;
                return true;
            }

            return false;
        }

        public void ForceComplete()
        {
            if (Data != null)
            {
                CurrentAmount = Data.TargetAmount;
            }
            IsCompleted = true;
        }
    }

    /// <summary>
    /// Runtime progress tracking for an entire Apollo flight plan.
    /// </summary>
    [Serializable]
    public class QuestRuntimeState
    {
        public QuestDataSO Data { get; private set; }
        public List<ObjectiveRuntimeState> ObjectiveStates { get; private set; }
        public int CurrentObjectiveIndex { get; private set; }
        public bool IsCompleted { get; private set; }

        public ObjectiveRuntimeState CurrentObjective =>
            (ObjectiveStates != null && CurrentObjectiveIndex >= 0 && CurrentObjectiveIndex < ObjectiveStates.Count)
                ? ObjectiveStates[CurrentObjectiveIndex]
                : null;

        public QuestRuntimeState(QuestDataSO data)
        {
            Data = data;
            ObjectiveStates = new List<ObjectiveRuntimeState>();
            CurrentObjectiveIndex = 0;
            IsCompleted = false;

            if (data != null && data.Objectives != null)
            {
                for (int i = 0; i < data.Objectives.Length; i++)
                {
                    if (data.Objectives[i] != null)
                    {
                        ObjectiveStates.Add(new ObjectiveRuntimeState(data.Objectives[i]));
                    }
                }
            }
        }

        public ObjectiveRuntimeState FindObjectiveState(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId) || ObjectiveStates == null)
                return null;

            for (int i = 0; i < ObjectiveStates.Count; i++)
            {
                if (ObjectiveStates[i].Data != null && ObjectiveStates[i].Data.ObjectiveId == objectiveId)
                {
                    return ObjectiveStates[i];
                }
            }

            return null;
        }

        public void AdvanceToNextObjective()
        {
            CurrentObjectiveIndex++;
            if (CurrentObjectiveIndex >= ObjectiveStates.Count)
            {
                IsCompleted = true;
            }
        }

        public bool CheckAllObjectivesCompleted()
        {
            if (ObjectiveStates == null || ObjectiveStates.Count == 0)
                return true;

            for (int i = 0; i < ObjectiveStates.Count; i++)
            {
                if (!ObjectiveStates[i].IsCompleted && !ObjectiveStates[i].Data.IsOptional)
                    return false;
            }

            IsCompleted = true;
            return true;
        }
    }
}

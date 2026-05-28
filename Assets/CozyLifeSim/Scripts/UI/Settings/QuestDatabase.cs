using System.Collections.Generic;
using UnityEngine;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI.Settings
{
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "CozySim/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        public List<QuestTemplate> Quests = new List<QuestTemplate>();

        /// <summary>
        /// Validates the database and returns any configuration errors.
        /// </summary>
        public bool ValidateDatabase(out List<string> errors)
        {
            errors = new List<string>();
            HashSet<int> uniqueIds = new HashSet<int>();

            if (Quests == null)
            {
                errors.Add("Quest list is null.");
                return false;
            }

            for (int i = 0; i < Quests.Count; i++)
            {
                var q = Quests[i];
                if (q == null)
                {
                    errors.Add($"Quest at index {i} is null.");
                    continue;
                }

                // 1. Check duplicate ID
                if (uniqueIds.Contains(q.QuestId))
                {
                    errors.Add($"Duplicate Quest ID found: {q.QuestId} ('{q.Title}')");
                }
                else
                {
                    uniqueIds.Add(q.QuestId);
                }

                // 2. Check empty Title
                if (string.IsNullOrWhiteSpace(q.Title))
                {
                    errors.Add($"Quest with ID {q.QuestId} has an empty Title.");
                }

                // 3. Check invalid Target Count
                if (q.TargetCount <= 0)
                {
                    errors.Add($"Quest with ID {q.QuestId} ('{q.Title}') has an invalid target count ({q.TargetCount}). Target count must be greater than zero.");
                }

                // 4. Check negative Reward Coins
                if (q.RewardCoins < 0)
                {
                    errors.Add($"Quest with ID {q.QuestId} ('{q.Title}') has negative reward coins ({q.RewardCoins}).");
                }
            }

            return errors.Count == 0;
        }
    }
}

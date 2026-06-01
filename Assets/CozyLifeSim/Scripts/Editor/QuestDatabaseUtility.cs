using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CozyLifeSim.UI.Settings;
using CozyLifeSim.Core;

namespace CozyLifeSim.Editor
{
    public static class QuestDatabaseUtility
    {
        public static QuestDatabase LoadOrCreateDatabase()
        {
            QuestDatabase database = null; // Doi tuong database load hoac tao moi
            string[] guids = AssetDatabase.FindAssets("t:QuestDatabase");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(path);
            }
            else
            {
                string dir = "Assets/CozyLifeSim/Settings";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    AssetDatabase.Refresh();
                }
                string assetPath = $"{dir}/QuestDatabase.asset";
                database = ScriptableObject.CreateInstance<QuestDatabase>();
                AssetDatabase.CreateAsset(database, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[CozySim]</color> Created new QuestDatabase asset at {assetPath}");
            }

            if (database != null)
            {
                BootstrapDefaultQuests(database);
            }

            return database;
        }

        public static void BootstrapDefaultQuests(QuestDatabase database)
        {
            if (database == null) return;

            if (database.Quests == null)
            {
                database.Quests = new List<QuestTemplate>();
            }

            bool addedOrUpdated = false; // Kiem tra xem co add hoac update quest nao khong
            bool isFirstTime = database.BootstrapVersion < 1; // Kiem tra phien ban migration de thuc hien force overwrite

            addedOrUpdated |= CheckOrUpdateQuest(database, 1, "Water 3 Crops", 3, 50, QuestType.WaterCrops, 20, isFirstTime);
            addedOrUpdated |= CheckOrUpdateQuest(database, 2, "Huong Vi Ngay He (Harvest 3 Cay Mia Ngot)", 3, 100, QuestType.HarvestCrops, 40, isFirstTime);
            addedOrUpdated |= CheckOrUpdateQuest(database, 3, "Vui Hoi Trang Ram (Harvest 1 Lua Nuoc)", 1, 150, QuestType.HarvestCrops, 60, isFirstTime);
            addedOrUpdated |= CheckOrUpdateQuest(database, 4, "Net Dep Que Huong (Pet Meo Tam The 3 times)", 3, 200, QuestType.PetAnimal, 100, isFirstTime);

            if (isFirstTime)
            {
                database.BootstrapVersion = 1;
                addedOrUpdated = true;
            }

            if (addedOrUpdated)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[CozySim]</color> Automatically bootstrapped/updated heritage quests inside database.");
            }
        }

        private static bool CheckOrUpdateQuest(QuestDatabase database, int id, string title, int target, int coins, QuestType type, int xp, bool forceOverwrite)
        {
            int targetId = id; // ID cua quest can kiem tra
            QuestTemplate existingQuest = database.Quests.Find(x => x != null && x.QuestId == targetId); // Tim kiem quest da ton tai

            if (existingQuest == null)
            {
                database.Quests.Add(new QuestTemplate(targetId, title, target, coins, type, xp));
                return true;
            }

            // Neu can forceOverwrite (first-time migration), ta moi cap nhat lai de dong bo noi dung Heritage
            if (forceOverwrite)
            {
                if (existingQuest.Title != title || existingQuest.TargetCount != target || existingQuest.RewardCoins != coins || existingQuest.Type != type || existingQuest.RewardXP != xp)
                {
                    existingQuest.Title = title;
                    existingQuest.TargetCount = target;
                    existingQuest.RewardCoins = coins;
                    existingQuest.Type = type;
                    existingQuest.RewardXP = xp;
                    return true;
                }
            }
            return false;
        }
    }
}

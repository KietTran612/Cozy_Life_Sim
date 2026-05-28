using UnityEditor;
using UnityEngine;
using CozyLifeSim.Core;
using CozyLifeSim.UI.Services;
using System.Collections.Generic;

namespace CozyLifeSim.Editor
{
    public static class CozyLifeSimValidation
    {
        [MenuItem("Tools/CozySim/Run Logic Verification Tests")]
        public static void RunTests()
        {
            Debug.Log("<color=cyan>[CozySim TestRunner]</color> Starting core logic verification tests...");
            
            try
            {
                // Reset ONLY our game's save key to prevent wiping other editor/project settings
                PlayerPrefs.DeleteKey("CozyLifeSim_SaveGame");
                PlayerPrefs.Save();
                
                // Test 1: Save & Load
                SaveService saveService = new SaveService();
                if (saveService.ActiveSave == null) throw new System.Exception("ActiveSave is null");
                Debug.Log("<color=green>[PASS]</color> SaveService initialized successfully");

                // Test 2: Inventory Actions (SaveData defaults: Coins = 100, Seeds = 5, Crops = 0)
                InventoryService invService = new InventoryService(saveService);
                int coinsChangedCount = 0;
                invService.OnCoinsChanged += (val) => coinsChangedCount++;
                
                invService.AddCoins(100);
                if (invService.Coins != 200) throw new System.Exception($"Coins should be 200 (100 default + 100 added), got {invService.Coins}");
                if (coinsChangedCount != 1) throw new System.Exception($"OnCoinsChanged should be fired once, got {coinsChangedCount}");
                
                bool consumed = invService.ConsumeCoins(40);
                if (!consumed) throw new System.Exception("Should consume 40 coins successfully");
                if (invService.Coins != 160) throw new System.Exception($"Coins should be 160 (200 - 40 consumed), got {invService.Coins}");
                
                invService.AddSeeds(10);
                if (invService.Seeds != 15) throw new System.Exception($"Seeds should be 15 (5 default + 10 added), got {invService.Seeds}");
                
                bool seedConsumed = invService.ConsumeSeeds(3);
                if (!seedConsumed) throw new System.Exception("Should consume 3 seeds successfully");
                if (invService.Seeds != 12) throw new System.Exception($"Seeds should be 12 (15 - 3 consumed), got {invService.Seeds}");
                
                Debug.Log("<color=green>[PASS]</color> InventoryService and SaveService logic verified");

                // Test 3: Quest Progression & Rewards
                QuestService questService = new QuestService(saveService, invService);
                if (questService.ActiveQuests.Count != 3) throw new System.Exception("Quests should contain 3 entries");
                
                QuestData waterQuest = questService.ActiveQuests[0]; // QuestId = 1: Water 3 Crops
                if (waterQuest.IsCompleted) throw new System.Exception("Water Quest should not be completed yet");
                
                int questProgressCount = 0;
                int questCompletedCount = 0;
                questService.OnQuestProgressed += (q) => questProgressCount++;
                questService.OnQuestCompleted += (q) => questCompletedCount++;
                
                // Progress Quest
                questService.ProgressQuest(1, 1);
                if (waterQuest.CurrentCount != 1) throw new System.Exception("Quest progress should be 1");
                if (questProgressCount != 1) throw new System.Exception("OnQuestProgressed should fire");
                
                // Progress to Completion
                questService.ProgressQuest(1, 2);
                if (!waterQuest.IsCompleted) throw new System.Exception("Quest should be completed");
                if (questCompletedCount != 1) throw new System.Exception("OnQuestCompleted should fire");
                
                // Verify Reward Coins (160 + 50 reward = 210)
                if (invService.Coins != 210) throw new System.Exception($"Coins after quest reward should be 210, got {invService.Coins}");
                
                Debug.Log("<color=green>[PASS]</color> QuestService progression and rewards verified");
                
                // Test 4: Reload and Persistence
                SaveService reloadSaveService = new SaveService();
                InventoryService reloadInvService = new InventoryService(reloadSaveService);
                if (reloadInvService.Coins != 210) throw new System.Exception($"Reloaded Coins should be 210, got {reloadInvService.Coins}");
                if (reloadInvService.Seeds != 12) throw new System.Exception($"Reloaded Seeds should be 12, got {reloadInvService.Seeds}");
                
                QuestService reloadQuestService = new QuestService(reloadSaveService, reloadInvService);
                if (!reloadQuestService.ActiveQuests[0].IsCompleted) throw new System.Exception("Reloaded Quest should remain completed");
                
                Debug.Log("<color=green>[PASS]</color> Save/Load Persistence verified");
                
                // Print all-pass congratulations!
                Debug.Log("<color=cyan>[CozySim TestRunner]</color> <color=green>ALL VERIFICATION TESTS PASSED SUCCESSFULLY!</color>");
                
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Verification Tests Passed", "All core services, inventory modifications, quest progression and PlayerPrefs persistence verified successfully!", "OK");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"<color=red>[FAIL]</color> Verification test failed: {ex.Message}\n{ex.StackTrace}");
                if (Application.isBatchMode)
                {
                    throw;
                }
                else
                {
                    EditorUtility.DisplayDialog("Verification Test Failed", $"Test failed: {ex.Message}", "OK");
                }
            }
        }
    }
}

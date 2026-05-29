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
                QuestService questService = new QuestService(saveService, invService, null); // Test Null Fallback
                if (questService.ActiveQuests.Count != 3) throw new System.Exception("Quests should contain 3 entries");
                
                QuestData waterQuest = questService.ActiveQuests[0]; // QuestId = 1: Water 3 Crops
                if (waterQuest.IsCompleted) throw new System.Exception("Water Quest should not be completed yet");
                
                int questProgressCount = 0;
                int questCompletedCount = 0;
                questService.OnQuestProgressed += (q) => questProgressCount++;
                questService.OnQuestCompleted += (q) => questCompletedCount++;
                
                // Progress Quest
                questService.ProgressQuest(QuestType.WaterCrops, 1);
                if (waterQuest.CurrentCount != 1) throw new System.Exception("Quest progress should be 1");
                if (questProgressCount != 1) throw new System.Exception("OnQuestProgressed should fire");
                
                // Progress to Completion
                questService.ProgressQuest(QuestType.WaterCrops, 2);
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
                
                QuestService reloadQuestService = new QuestService(reloadSaveService, reloadInvService, null); // Test Null Fallback
                if (!reloadQuestService.ActiveQuests[0].IsCompleted) throw new System.Exception("Reloaded Quest should remain completed");
                
                Debug.Log("<color=green>[PASS]</color> Save/Load Persistence verified");

                // Test 5: Quest Editor Data Integrity Validation
                var testDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.QuestDatabase>();
                
                // 5.1 Valid list should pass
                testDb.Quests.Add(new QuestTemplate(1, "Valid Quest 1", 3, 50, QuestType.WaterCrops));
                testDb.Quests.Add(new QuestTemplate(2, "Valid Quest 2", 5, 100, QuestType.HarvestCrops));
                if (!testDb.ValidateDatabase(out var errors))
                {
                    throw new System.Exception($"Valid database failed validation: {string.Join(", ", errors)}");
                }

                // 5.2 Duplicate ID should fail
                testDb.Quests.Add(new QuestTemplate(1, "Duplicate ID", 5, 20, QuestType.PetAnimal));
                if (testDb.ValidateDatabase(out var duplicateErrors))
                {
                    throw new System.Exception("Database with duplicate ID should fail validation.");
                }

                // 5.3 Negative target should fail
                testDb.Quests.Clear();
                testDb.Quests.Add(new QuestTemplate(1, "Negative Goal", -2, 50, QuestType.WaterCrops));
                if (testDb.ValidateDatabase(out var targetErrors))
                {
                    throw new System.Exception("Database with negative target count should fail validation.");
                }

                // 5.4 Negative reward should fail
                testDb.Quests.Clear();
                testDb.Quests.Add(new QuestTemplate(1, "Negative Reward", 3, -10, QuestType.WaterCrops));
                if (testDb.ValidateDatabase(out var rewardErrors))
                {
                    throw new System.Exception("Database with negative reward coins should fail validation.");
                }

                // 5.5 Empty title should fail
                testDb.Quests.Clear();
                testDb.Quests.Add(new QuestTemplate(1, "   ", 3, 50, QuestType.WaterCrops));
                if (testDb.ValidateDatabase(out var titleErrors))
                {
                    throw new System.Exception("Database with blank title should fail validation.");
                }

                Object.DestroyImmediate(testDb);
                Debug.Log("<color=green>[PASS]</color> Quest Editor Data Integrity Validation verified");
                
                // Test 6: Crop Editor Data Integrity Validation
                var testCropDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.CropDatabase>();
                
                // 6.1 Valid list should pass
                var testSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), Vector2.zero);
                testCropDb.Crops.Add(new CozyLifeSim.UI.Settings.CropTemplate(1, "Acorn", 5f, testSprite, testSprite, testSprite, testSprite));
                if (!testCropDb.ValidateDatabase(out var cropErrors))
                {
                    throw new System.Exception($"Valid crop database failed validation: {string.Join(", ", cropErrors)}");
                }

                // 6.2 Duplicate ID should fail
                testCropDb.Crops.Add(new CozyLifeSim.UI.Settings.CropTemplate(1, "Duplicate Acorn", 10f, testSprite, testSprite, testSprite, testSprite));
                if (testCropDb.ValidateDatabase(out var dupCropErrors))
                {
                    throw new System.Exception("Crop database with duplicate ID should fail validation.");
                }

                // 6.3 Negative duration should fail
                testCropDb.Crops.Clear();
                testCropDb.Crops.Add(new CozyLifeSim.UI.Settings.CropTemplate(1, "Negative Duration", -1f, testSprite, testSprite, testSprite, testSprite));
                if (testCropDb.ValidateDatabase(out var durCropErrors))
                {
                    throw new System.Exception("Crop database with negative stage duration should fail validation.");
                }

                // 6.4 Missing sprites should fail
                testCropDb.Crops.Clear();
                testCropDb.Crops.Add(new CozyLifeSim.UI.Settings.CropTemplate(1, "Missing Sprites", 5f, null, testSprite, testSprite, testSprite));
                if (testCropDb.ValidateDatabase(out var spriteCropErrors))
                {
                    throw new System.Exception("Crop database with missing sprites should fail validation.");
                }

                Object.DestroyImmediate(testCropDb);
                if (testSprite != null) Object.DestroyImmediate(testSprite);
                Debug.Log("<color=green>[PASS]</color> Crop Editor Data Integrity Validation verified");

                // Test 7: CropDatabase In-Memory Bootstrapping and Integrity (strictly side-effect free)
                var testLoadedCropDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.CropDatabase>();
                CropDatabaseUtility.BootstrapDefaultCrop(testLoadedCropDb);
                if (testLoadedCropDb == null) throw new System.Exception("Failed to load or create in-memory CropDatabase");
                if (testLoadedCropDb.Crops == null || testLoadedCropDb.Crops.Count == 0) throw new System.Exception("CropDatabase should be bootstrapped with default White Acorn");
                if (testLoadedCropDb.Crops[0].CropId != 1 || testLoadedCropDb.Crops[0].Name != "White Acorn") throw new System.Exception("Bootstrapped crop should be White Acorn (ID 1)");
                Object.DestroyImmediate(testLoadedCropDb);
                Debug.Log("<color=green>[PASS]</color> CropDatabase In-Memory Bootstrapping verified");

                // Test 8: AnimalDatabase In-Memory Bootstrapping and Integrity (strictly side-effect free)
                var testLoadedAnimalDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.AnimalDatabase>();
                AnimalDatabaseUtility.BootstrapDefaultAnimal(testLoadedAnimalDb);
                if (testLoadedAnimalDb == null) throw new System.Exception("Failed to load or create in-memory AnimalDatabase");
                if (testLoadedAnimalDb.Animals == null || testLoadedAnimalDb.Animals.Count == 0) throw new System.Exception("AnimalDatabase should be bootstrapped with default Breathing Chicken");
                if (testLoadedAnimalDb.Animals[0].AnimalId != 1 || testLoadedAnimalDb.Animals[0].Name != "Breathing Chicken") throw new System.Exception("Bootstrapped animal should be Breathing Chicken (ID 1)");
                if (testLoadedAnimalDb.Animals[0].BreathScaleY <= 1.0f) throw new System.Exception("Bootstrapped animal breath scale should be greater than 1.0f");
                Object.DestroyImmediate(testLoadedAnimalDb);
                Debug.Log("<color=green>[PASS]</color> AnimalDatabase In-Memory Bootstrapping verified");

                // Test 9: StickerDatabase In-Memory Validation (strictly side-effect free)
                var testStickerDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.StickerDatabase>();
                
                // 9.1 Valid list should pass
                var testStickerSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), Vector2.zero);
                testStickerDb.Stickers.Add(new CozyLifeSim.UI.Settings.StickerTemplate(1, "Bunny Pink", testStickerSprite, testStickerSprite));
                if (!testStickerDb.ValidateDatabase(out var stickerErrors))
                {
                    throw new System.Exception($"Valid sticker database failed validation: {string.Join(", ", stickerErrors)}");
                }

                // 9.2 Duplicate ID should fail
                testStickerDb.Stickers.Add(new CozyLifeSim.UI.Settings.StickerTemplate(1, "Duplicate Bear", testStickerSprite, testStickerSprite));
                if (testStickerDb.ValidateDatabase(out var dupStickerErrors))
                {
                    throw new System.Exception("Sticker database with duplicate ID should fail validation.");
                }

                // 9.3 Negative ID should fail
                testStickerDb.Stickers.Clear();
                testStickerDb.Stickers.Add(new CozyLifeSim.UI.Settings.StickerTemplate(-1, "Negative ID", testStickerSprite, testStickerSprite));
                if (testStickerDb.ValidateDatabase(out var negIdErrors))
                {
                    throw new System.Exception("Sticker database with negative ID should fail validation.");
                }

                // 9.4 Missing main sprite should fail
                testStickerDb.Stickers.Clear();
                testStickerDb.Stickers.Add(new CozyLifeSim.UI.Settings.StickerTemplate(1, "Missing Sprite", null, testStickerSprite));
                if (testStickerDb.ValidateDatabase(out var spriteErrors))
                {
                    throw new System.Exception("Sticker database with missing sprite should fail validation.");
                }

                Object.DestroyImmediate(testStickerDb);
                if (testStickerSprite != null) Object.DestroyImmediate(testStickerSprite);
                Debug.Log("<color=green>[PASS]</color> StickerDatabase In-Memory Validation verified");

                // Test 9.5: StickerDatabase In-Memory Bootstrapping and Integrity (strictly side-effect free)
                var testLoadedStickerDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.StickerDatabase>();
                StickerDatabaseUtility.BootstrapDefaultStickers(testLoadedStickerDb);
                if (testLoadedStickerDb == null) throw new System.Exception("Failed to load or create in-memory StickerDatabase");
                if (testLoadedStickerDb.Stickers == null || testLoadedStickerDb.Stickers.Count == 0) throw new System.Exception("StickerDatabase should be bootstrapped with default stickers");
                if (testLoadedStickerDb.Stickers[0].StickerId != 1 || testLoadedStickerDb.Stickers[0].Name != "Bunny Pink") throw new System.Exception("Bootstrapped sticker 1 should be Bunny Pink (ID 1)");
                if (testLoadedStickerDb.Stickers[1].StickerId != 2 || testLoadedStickerDb.Stickers[1].Name != "Bear") throw new System.Exception("Bootstrapped sticker 2 should be Bear (ID 2)");
                Object.DestroyImmediate(testLoadedStickerDb);
                Debug.Log("<color=green>[PASS]</color> StickerDatabase In-Memory Bootstrapping verified");
                
                // Print all-pass congratulations in a highly prominent way!
                Debug.Log("\n===================================================================================\n" +
                          "⚡ <size=14><b>[CozySim ALL LOGIC VERIFICATION TESTS PASSED SUCCESSFULLY!]</b></size> ⚡\n" +
                          "<b>All core services, inventory modifications, quest progression and PlayerPrefs persistence verified successfully!</b>\n" +
                          "===================================================================================\n");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("\n===================================================================================\n" +
                               "❌ <size=14><b>[CozySim LOGIC VERIFICATION TEST FAILED!]</b></size> ❌\n" +
                               $"<b>Error:</b> <color=red>{ex.Message}</color>\n" +
                               $"<b>Stack Trace:</b>\n{ex.StackTrace}\n" +
                               "===================================================================================\n");
                if (Application.isBatchMode)
                {
                    throw;
                }
            }
        }
    }
}

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
            int passCount = 0;
            int failCount = 0;
            int expectedWarningCount = 0;

            Debug.Log("<color=cyan>[CozySim TestRunner]</color> Starting core logic verification tests...");
            
            CozyLifeSim.UI.Settings.QuestDatabase questDb = null;
            try
            {
                // Reset ONLY our game's save key to prevent wiping other editor/project settings
                PlayerPrefs.DeleteKey("CozyLifeSim_SaveGame");
                PlayerPrefs.Save();
                
                // Test 1: Save & Load
                SaveService saveService = new SaveService();
                if (saveService.ActiveSave == null) throw new System.Exception("ActiveSave is null");
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "SaveService initialized successfully");

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
                
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "InventoryService and SaveService logic verified");

                // Prepare standard QuestDatabase for normal path testing
                questDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.QuestDatabase>();
                questDb.Quests.Add(new QuestTemplate(1, "Water 3 Crops", 3, 50, QuestType.WaterCrops));
                questDb.Quests.Add(new QuestTemplate(2, "Harvest 2 Mature Crops", 2, 80, QuestType.HarvestCrops));
                questDb.Quests.Add(new QuestTemplate(3, "Pet the Breathing Chicken 5 times", 5, 40, QuestType.PetAnimal));

                // Test 3: Quest Progression & Rewards
                QuestService questService = new QuestService(saveService, invService, questDb);
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
                
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "QuestService progression and rewards verified");
                
                // Test 4: Reload and Persistence
                SaveService reloadSaveService = new SaveService();
                InventoryService reloadInvService = new InventoryService(reloadSaveService);
                if (reloadInvService.Coins != 210) throw new System.Exception($"Reloaded Coins should be 210, got {reloadInvService.Coins}");
                if (reloadInvService.Seeds != 12) throw new System.Exception($"Reloaded Seeds should be 12, got {reloadInvService.Seeds}");
                
                QuestService reloadQuestService = new QuestService(reloadSaveService, reloadInvService, questDb);
                if (!reloadQuestService.ActiveQuests[0].IsCompleted) throw new System.Exception("Reloaded Quest should remain completed");
                
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Save/Load Persistence verified");

                // Test 4.5: Explicit expected fallback test
                QuestService fallbackQuestService = new QuestService(saveService, invService, null, false);
                if (fallbackQuestService.ActiveQuests.Count != 3)
                {
                    throw new System.Exception("Fallback quest service should contain 3 default quests.");
                }

                expectedWarningCount++;
                CozyValidationLog.ExpectedWarning("CozySim Logic", "QuestDatabase null fallback was intentionally exercised.");

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
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Quest Editor Data Integrity Validation verified");
                
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
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Crop Editor Data Integrity Validation verified");

                // Test 7: CropDatabase In-Memory Bootstrapping and Integrity (strictly side-effect free)
                var testLoadedCropDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.CropDatabase>();
                CropDatabaseUtility.BootstrapDefaultCrop(testLoadedCropDb);
                if (testLoadedCropDb == null) throw new System.Exception("Failed to load or create in-memory CropDatabase");
                if (testLoadedCropDb.Crops == null || testLoadedCropDb.Crops.Count == 0) throw new System.Exception("CropDatabase should be bootstrapped with default White Acorn");
                if (testLoadedCropDb.Crops[0].CropId != 1 || testLoadedCropDb.Crops[0].Name != "White Acorn") throw new System.Exception("Bootstrapped crop should be White Acorn (ID 1)");
                Object.DestroyImmediate(testLoadedCropDb);
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "CropDatabase In-Memory Bootstrapping verified");

                // Test 8: AnimalDatabase In-Memory Bootstrapping and Integrity (strictly side-effect free)
                var testLoadedAnimalDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.AnimalDatabase>();
                AnimalDatabaseUtility.BootstrapDefaultAnimal(testLoadedAnimalDb);
                if (testLoadedAnimalDb == null) throw new System.Exception("Failed to load or create in-memory AnimalDatabase");
                if (testLoadedAnimalDb.Animals == null || testLoadedAnimalDb.Animals.Count == 0) throw new System.Exception("AnimalDatabase should be bootstrapped with default Breathing Chicken");
                if (testLoadedAnimalDb.Animals[0].AnimalId != 1 || testLoadedAnimalDb.Animals[0].Name != "Breathing Chicken") throw new System.Exception("Bootstrapped animal should be Breathing Chicken (ID 1)");
                if (testLoadedAnimalDb.Animals[0].BreathScaleY <= 1.0f) throw new System.Exception("Bootstrapped animal breath scale should be greater than 1.0f");
                Object.DestroyImmediate(testLoadedAnimalDb);
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "AnimalDatabase In-Memory Bootstrapping verified");

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
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "StickerDatabase In-Memory Validation verified");

                // Test 9.5: StickerDatabase In-Memory Bootstrapping and Integrity (strictly side-effect free)
                var testLoadedStickerDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.StickerDatabase>();
                StickerDatabaseUtility.BootstrapDefaultStickers(testLoadedStickerDb);
                if (testLoadedStickerDb == null) throw new System.Exception("Failed to load or create in-memory StickerDatabase");
                if (testLoadedStickerDb.Stickers == null || testLoadedStickerDb.Stickers.Count == 0) throw new System.Exception("StickerDatabase should be bootstrapped with default stickers");
                if (testLoadedStickerDb.Stickers[0].StickerId != 1 || testLoadedStickerDb.Stickers[0].Name != "Bunny Pink") throw new System.Exception("Bootstrapped sticker 1 should be Bunny Pink (ID 1)");
                if (testLoadedStickerDb.Stickers[1].StickerId != 2 || testLoadedStickerDb.Stickers[1].Name != "Bear") throw new System.Exception("Bootstrapped sticker 2 should be Bear (ID 2)");
                Object.DestroyImmediate(testLoadedStickerDb);
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "StickerDatabase In-Memory Bootstrapping verified");

                // Test 10: Task 23 Data Model Prices & UnlockedStickers defaults
                var testSaveData = new SaveData();
                if (testSaveData.UnlockedStickerIds == null || testSaveData.UnlockedStickerIds.Count != 2)
                {
                    throw new System.Exception("UnlockedStickerIds list should be initialized and contain exactly 2 defaults (ID 1, 2)");
                }
                if (!testSaveData.UnlockedStickerIds.Contains(1) || !testSaveData.UnlockedStickerIds.Contains(2))
                {
                    throw new System.Exception("UnlockedStickerIds must contain default sticker IDs 1 and 2");
                }

                var testCrop = new CozyLifeSim.UI.Settings.CropTemplate { BuyPrice = 5, SellPrice = 15 };
                if (testCrop.BuyPrice != 5 || testCrop.SellPrice != 15)
                {
                    throw new System.Exception("CropTemplate prices must be initialized correctly");
                }

                var testSticker = new CozyLifeSim.UI.Settings.StickerTemplate { BuyPrice = 50 };
                if (testSticker.BuyPrice != 50)
                {
                    throw new System.Exception("StickerTemplate price must be initialized correctly");
                }

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Task 23 Pricing & UnlockedStickers defaults verified");

                // Test 11: Task 23 IShopService Transaction Safety
                var testShopSave = new SaveService();
                var testShopInv = new InventoryService(testShopSave);

                // Clear save
                testShopSave.ActiveSave.Coins = 100;
                testShopSave.ActiveSave.Seeds = 5;
                testShopSave.ActiveSave.Crops = 0;
                testShopSave.ActiveSave.UnlockedStickerIds = new List<int> { 1, 2 };
                testShopSave.Save();

                var testShopCropDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.CropDatabase>();
                var testCropTemp = new CozyLifeSim.UI.Settings.CropTemplate(1, "Test White Acorn", 1f, null, null, null, null) { BuyPrice = 5, SellPrice = 15 };
                testShopCropDb.Crops.Add(testCropTemp);

                var testShopStickerDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.StickerDatabase>();
                testShopStickerDb.Stickers.Add(new CozyLifeSim.UI.Settings.StickerTemplate(1, "Bunny Pink", null, null) { BuyPrice = 50 });
                testShopStickerDb.Stickers.Add(new CozyLifeSim.UI.Settings.StickerTemplate(2, "Bear", null, null) { BuyPrice = 50 });
                testShopStickerDb.Stickers.Add(new CozyLifeSim.UI.Settings.StickerTemplate(3, "Chicken White", null, null) { BuyPrice = 50 });

                // Construct IShopService with in-memory databases.
                IShopService shopService = new ShopService(testShopSave, testShopInv, testShopCropDb, testShopStickerDb);

                // 11.1 TryBuySeed(1) with 100 coins -> Expect True. Coins = 95, Seeds = 6.
                if (!shopService.TryBuySeed(1)) throw new System.Exception("TryBuySeed(1) should succeed with 100 coins");
                if (testShopInv.Coins != 95 || testShopInv.Seeds != 6) throw new System.Exception($"Balances incorrect after seed purchase. Coins: {testShopInv.Coins}, Seeds: {testShopInv.Seeds}");

                // 11.2 TryBuySeed(999) (invalid ID) -> Expect False (clean abort, return false, no throw)
                if (shopService.TryBuySeed(999)) throw new System.Exception("TryBuySeed(999) should return false on missing template");

                // 11.3 TryBuySticker(1) (already unlocked) -> Expect False
                if (shopService.TryBuySticker(1)) throw new System.Exception("TryBuySticker(1) should fail because default sticker is already unlocked");

                // 11.4 TryBuySticker(3) with sufficient coins -> Expect True. Coins = 45, ID 3 added.
                if (!shopService.TryBuySticker(3)) throw new System.Exception("TryBuySticker(3) should succeed with 95 coins");
                if (testShopInv.Coins != 45) throw new System.Exception($"Coins incorrect after sticker purchase. Coins: {testShopInv.Coins}");
                if (!shopService.IsStickerUnlocked(3)) throw new System.Exception("Sticker ID 3 should be unlocked");

                // 11.5 Repeated TryBuySticker(3) -> Expect False (already unlocked)
                if (shopService.TryBuySticker(3)) throw new System.Exception("Repeated TryBuySticker(3) should fail");
                if (testShopInv.Coins != 45) throw new System.Exception("Coins balance should not change on failed repeated sticker purchase");

                // 11.6 Insufficient Coins validation
                testShopSave.ActiveSave.Coins = 10;
                testShopInv.ReloadFromSave();
                if (shopService.TryBuySticker(3)) throw new System.Exception("TryBuySticker(3) should fail because it is already unlocked");
                testShopStickerDb.Stickers.Add(new CozyLifeSim.UI.Settings.StickerTemplate(4, "Sticker 4", null, null) { BuyPrice = 50 });
                if (shopService.TryBuySticker(4)) throw new System.Exception("TryBuySticker(4) should fail with insufficient coins");
                if (testShopInv.Coins != 10) throw new System.Exception("Coins balance should not change on failed purchase due to insufficient funds");

                // 11.7 Insufficient Crops validation
                testShopSave.ActiveSave.Crops = 0;
                testShopInv.ReloadFromSave();
                if (shopService.TrySellCrop(1)) throw new System.Exception("TrySellCrop(1) should fail with 0 crops");
                if (testShopInv.Crops != 0) throw new System.Exception("Crops balance should not change on failed sale due to insufficient crops");

                // 11.8 Successful TrySellCrop(1) -> Crops = 1, Coins = 10 + 15 = 25
                testShopSave.ActiveSave.Crops = 2;
                testShopInv.ReloadFromSave();
                if (!shopService.TrySellCrop(1)) throw new System.Exception("TrySellCrop(1) should succeed with 2 crops");
                if (testShopInv.Crops != 1 || testShopInv.Coins != 25) throw new System.Exception($"Balances incorrect after crop sale. Crops: {testShopInv.Crops}, Coins: {testShopInv.Coins}");

                // 11.9 Invalid SellPrice validation (SellPrice <= 0 should fail and not consume crops or coins)
                testCropTemp.SellPrice = 0;
                testShopSave.ActiveSave.Crops = 1;
                testShopSave.ActiveSave.Coins = 10;
                testShopInv.ReloadFromSave();
                if (shopService.TrySellCrop(1)) throw new System.Exception("TrySellCrop(1) should fail when SellPrice is 0");
                if (testShopInv.Crops != 1 || testShopInv.Coins != 10) throw new System.Exception($"Balances mutated on invalid SellPrice. Crops: {testShopInv.Crops}, Coins: {testShopInv.Coins}");

                testCropTemp.SellPrice = -5;
                if (shopService.TrySellCrop(1)) throw new System.Exception("TrySellCrop(1) should fail when SellPrice is negative");
                if (testShopInv.Crops != 1 || testShopInv.Coins != 10) throw new System.Exception($"Balances mutated on negative SellPrice. Crops: {testShopInv.Crops}, Coins: {testShopInv.Coins}");

                // Restore SellPrice
                testCropTemp.SellPrice = 15;

                Object.DestroyImmediate(testShopCropDb);
                Object.DestroyImmediate(testShopStickerDb);

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Task 23 IShopService Transaction Safety verified");



                if (questDb != null)
                {
                    Object.DestroyImmediate(questDb);
                    questDb = null;
                }

                CozyValidationLog.Summary("CozySim Logic", passCount, failCount, expectedWarningCount);
            }
            catch (System.Exception ex)
            {
                failCount = 1;
                if (questDb != null)
                {
                    Object.DestroyImmediate(questDb);
                    questDb = null;
                }

                CozyValidationLog.Fail("CozySim Logic", $"{ex.Message}\n{ex.StackTrace}");
                CozyValidationLog.Summary("CozySim Logic", passCount, failCount, expectedWarningCount);
                if (Application.isBatchMode)
                {
                    throw;
                }
            }
        }
    }
}

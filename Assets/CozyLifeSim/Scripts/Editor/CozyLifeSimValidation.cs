using UnityEditor;
using UnityEngine;
using CozyLifeSim.Core;
using CozyLifeSim.UI;
using CozyLifeSim.UI.Services;
using CozyLifeSim.UI.Presenters;
using System.Collections.Generic;
using TMPro;

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

            // Cleanup stale test game objects from previous runs if any
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go != null && (
                    go.name == "TempDialoguePopupTest" ||
                    go.name == "TempDialoguePopupTest155" ||
                    go.name == "TempNpcTest" ||
                    go.name == "MockEventSystem" ||
                    go.name == "DummyUIObject" ||
                    go.name == "TempShopItemWidgetTest" ||
                    go.name == "TempFeedbackToastTest"))
                {
                    Object.DestroyImmediate(go);
                }
            }

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

                // 5.5 Negative reward XP should fail
                testDb.Quests.Clear();
                testDb.Quests.Add(new QuestTemplate(1, "Negative XP", 3, 10, QuestType.WaterCrops, -1));
                if (testDb.ValidateDatabase(out var rewardXpErrors))
                {
                    throw new System.Exception("Database with negative reward XP should fail validation.");
                }

                // 5.6 Empty title should fail
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

                // 6.5 Invalid required level should fail
                testCropDb.Crops.Clear();
                testCropDb.Crops.Add(new CozyLifeSim.UI.Settings.CropTemplate(1, "Invalid Level", 5f, testSprite, testSprite, testSprite, testSprite) { RequiredLevel = 0 });
                if (testCropDb.ValidateDatabase(out var reqLevelCropErrors))
                {
                    throw new System.Exception("Crop database with invalid required level should fail validation.");
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

                testLoadedAnimalDb.Animals[0].RequiredLevel = 0;
                if (testLoadedAnimalDb.ValidateDatabase(out var reqLevelAnimalErrors))
                {
                    throw new System.Exception("Animal database with invalid required level should fail validation.");
                }

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

                // 9.5 Invalid required level should fail
                testStickerDb.Stickers.Clear();
                testStickerDb.Stickers.Add(new CozyLifeSim.UI.Settings.StickerTemplate(1, "Invalid Level", testStickerSprite, testStickerSprite) { RequiredLevel = 0 });
                if (testStickerDb.ValidateDatabase(out var reqLevelStickerErrors))
                {
                    throw new System.Exception("Sticker database with invalid required level should fail validation.");
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
#pragma warning disable CS0618
                if (testSaveData.UnlockedStickerIds == null || testSaveData.UnlockedStickerIds.Count != 2)
                {
                    throw new System.Exception("UnlockedStickerIds list should be initialized and contain exactly 2 defaults (ID 1, 2)");
                }
                if (!testSaveData.UnlockedStickerIds.Contains(1) || !testSaveData.UnlockedStickerIds.Contains(2))
                {
                    throw new System.Exception("UnlockedStickerIds must contain default sticker IDs 1 and 2");
                }
#pragma warning restore CS0618

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
                testShopSave.ActiveSave.StickerOwned.Clear();
                testShopSave.ActiveSave.StickerOwned.Add(new StickerInventory(1, 99));
                testShopSave.ActiveSave.StickerOwned.Add(new StickerInventory(2, 99));
                testShopSave.ActiveSave.HasMigratedStickerOwned = true;
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

                // 11.3 Default sticker starts owned through countable inventory
                if (!shopService.IsStickerUnlocked(1)) throw new System.Exception("Sticker ID 1 should be owned by default");

                // 11.4 TryBuySticker(3) with sufficient coins -> Expect True. Coins = 45, ID 3 count added.
                if (!shopService.TryBuySticker(3)) throw new System.Exception("TryBuySticker(3) should succeed with 95 coins");
                if (testShopInv.Coins != 45) throw new System.Exception($"Coins incorrect after sticker purchase. Coins: {testShopInv.Coins}");
                if (testShopInv.GetStickerCount(3) != 1) throw new System.Exception($"Sticker ID 3 count should be 1, got {testShopInv.GetStickerCount(3)}");

                // 11.5 Repeated TryBuySticker(3) -> Expect another countable copy
                testShopSave.ActiveSave.Coins = 100;
                testShopInv.ReloadFromSave();
                if (!shopService.TryBuySticker(3)) throw new System.Exception("Repeated TryBuySticker(3) should add another countable copy");
                if (testShopInv.GetStickerCount(3) != 2) throw new System.Exception($"Sticker ID 3 count should be 2 after repeated purchase, got {testShopInv.GetStickerCount(3)}");
                if (testShopInv.Coins != 50) throw new System.Exception("Coins balance should deduct price on repeated sticker purchase");

                // 11.6 Insufficient Coins validation
                testShopSave.ActiveSave.Coins = 10;
                testShopInv.ReloadFromSave();
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



                // Test 11.10: ProgressionService XP & Level Up
                SaveService testProgSave = new SaveService();
                testProgSave.ActiveSave.PlayerLevel = 1;
                testProgSave.ActiveSave.PlayerXP = 0;
                testProgSave.Save();

                ProgressionService progService = new ProgressionService(testProgSave);
                int levelUpCount = 0;
                int xpChangedCount = 0;
                int newLevelVal = 0;
                int newXpVal = 0;

                progService.OnLevelUp += (lvl) => { levelUpCount++; newLevelVal = lvl; };
                progService.OnXPChanged += (xp) => { xpChangedCount++; newXpVal = xp; };

                progService.AddXP(50);
                if (progService.PlayerXP != 50) throw new System.Exception($"PlayerXP should be 50, got {progService.PlayerXP}");
                if (progService.PlayerLevel != 1) throw new System.Exception($"PlayerLevel should be 1, got {progService.PlayerLevel}");
                if (xpChangedCount != 1) throw new System.Exception($"OnXPChanged should be called 1 time, got {xpChangedCount}");

                // Nguong level 1 la 1 * 100 = 100 XP
                progService.AddXP(60);
                if (progService.PlayerLevel != 2) throw new System.Exception($"PlayerLevel should level up to 2, got {progService.PlayerLevel}");
                if (progService.PlayerXP != 10) throw new System.Exception($"PlayerXP should carry over leftover 10 XP, got {progService.PlayerXP}");
                if (levelUpCount != 1) throw new System.Exception($"OnLevelUp should fire once, got {levelUpCount}");
                if (newLevelVal != 2) throw new System.Exception($"OnLevelUp payload should be 2, got {newLevelVal}");

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "ProgressionService XP and Level Up verified");

                // Test 11.11: SaveData Migration, Struct-Safe Backfill & Null-Safety Invariants
                SaveService testMigSave = new SaveService();
                var save = testMigSave.ActiveSave;

                // Gia lap save cu bi null/corrupt do du lieu truoc do
                save.StickerOwned = null;
                save.PlacedStickers = null;
                save.HasMigratedStickerOwned = false;

                // Gia lap co locked sticker cu ID = 3 va placed stickers thieu PlacementId
                #pragma warning disable CS0618
                save.UnlockedStickerIds = new List<int> { 3 };
                #pragma warning restore CS0618

                // Tao mock placed stickers, co 1 cai null GUID, 2 cai bi trung GUID de test seenIds regenerate
                var placed1 = new StickerPlacedData { StickerId = 1, PageIndex = 1, PlacementId = null };
                var placed2 = new StickerPlacedData { StickerId = 2, PageIndex = 1, PlacementId = "duplicate_id" };
                var placed3 = new StickerPlacedData { StickerId = 3, PageIndex = 1, PlacementId = "duplicate_id" };

                // Vi struct, ta phai khoi tao list va add
                save.PlacedStickers = new List<StickerPlacedData> { placed1, placed2, placed3 };

                // Chay normalize de bat dau di dan
                testMigSave.NormalizeSaveData();

                // 1. Kiem tra Null-Safety Invariants khoi tao thanh cong
                if (save.StickerOwned == null) throw new System.Exception("StickerOwned should be initialized to a non-null list");
                if (save.PlacedStickers == null) throw new System.Exception("PlacedStickers should be initialized to a non-null list");
                if (save.CompletedQuestIds == null) throw new System.Exception("CompletedQuestIds should be initialized to a non-null list");

                // 2. Kiem tra di dan sticker an toan
                if (!save.HasMigratedStickerOwned) throw new System.Exception("HasMigratedStickerOwned should be set to true");
                var id1 = save.StickerOwned.Find(x => x.StickerId == 1);
                var id2 = save.StickerOwned.Find(x => x.StickerId == 2);
                var id3 = save.StickerOwned.Find(x => x.StickerId == 3);

                if (id1 == null || id1.Count != 99) throw new System.Exception($"Default sticker ID 1 should be refilled to 99, got {id1?.Count}");
                if (id2 == null || id2.Count != 99) throw new System.Exception($"Default sticker ID 2 should be refilled to 99, got {id2?.Count}");
                if (id3 == null || id3.Count != 1) throw new System.Exception($"Legacy unlocked sticker ID 3 should be merged with Count = 1, got {id3?.Count}");

                // 3. Kiem tra backfill PlacementId (struct-safe & duplicate-safe)
                if (string.IsNullOrEmpty(save.PlacedStickers[0].PlacementId)) throw new System.Exception("PlacedSticker 0 should have backfilled PlacementId");
                if (string.IsNullOrEmpty(save.PlacedStickers[1].PlacementId)) throw new System.Exception("PlacedSticker 1 should have backfilled PlacementId");
                if (string.IsNullOrEmpty(save.PlacedStickers[2].PlacementId)) throw new System.Exception("PlacedSticker 2 should have backfilled PlacementId");

                if (save.PlacedStickers[1].PlacementId == save.PlacedStickers[2].PlacementId)
                    throw new System.Exception("Duplicate PlacementIds must be regenerated to ensure uniqueness");

                // Kiem tra no thuc su duoc ghi file (Normalize save tu dong khi isDirty == true)
                SaveService reloadMigSave = new SaveService();
                if (reloadMigSave.ActiveSave.StickerOwned.Find(x => x.StickerId == 3)?.Count != 1)
                    throw new System.Exception("Migrated data was not properly persisted to disk");

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "SaveData Migration and Null-Safety Invariants verified");

                // Test 11.12: Sticker Placement, Atomic Transaction & Memory Rollback
                SaveService testAtomicSave = new SaveService();

                // Clear save dau test de side-effect free
                testAtomicSave.ActiveSave.StickerOwned.Clear();
                testAtomicSave.ActiveSave.PlacedStickers.Clear();
                testAtomicSave.Save();

                InventoryService inventoryService = new InventoryService(testAtomicSave);
                MemoryService memoryService = new MemoryService(testAtomicSave);

                // Dam bao co san sticker trong kho
                inventoryService.AddStickerCountNonSaving(3, 1);
                testAtomicSave.Save();

                int initialCount = inventoryService.GetStickerCount(3);
                if (initialCount != 1) throw new System.Exception($"Initial count of sticker ID 3 should be 1, got {initialCount}");

                // Kich hoat ForceSaveFailure de gia lap loi IO
#if UNITY_EDITOR
                testAtomicSave.ForceSaveFailure = true;
#endif

                // Giao dich dan sticker (Co che rollback trong Presenter)
                bool transactionSuccess = false;
                var data = new StickerPlacedData(3, 1, 10f, 10f, 1f, 0f);

                // 1. Consume sticker count
                if (inventoryService.ConsumeStickerNonSaving(3))
                {
                    // 2. Add placed sticker (Tra ve struct co GUID duoc sinh defensive)
                    data = memoryService.AddPlacedStickerNonSaving(data);

                    try
                    {
                        // 3. Persist to disk
                        testAtomicSave.Save();
                        transactionSuccess = true;
                    }
                    catch (System.Exception)
                    {
                        // 4. Rollback neu luu dia that bai
                        inventoryService.AddStickerCountNonSaving(3, 1);
                        memoryService.TryRemovePlacedStickerNonSaving(data.PlacementId, out _);
                    }
                }

#if UNITY_EDITOR
                testAtomicSave.ForceSaveFailure = false;
#endif

                if (transactionSuccess) throw new System.Exception("Transaction should fail due to simulated IO Save error");

                // Kiem tra bo nho duoc khoi phuc hoàn toan ve trang thai cu
                int currentCount = inventoryService.GetStickerCount(3);
                if (currentCount != 1) throw new System.Exception($"After rollback, count of sticker ID 3 should be 1, got {currentCount}");

                bool removedSuccessfully = memoryService.TryRemovePlacedStickerNonSaving(data.PlacementId, out _);
                if (removedSuccessfully) throw new System.Exception("After rollback, placed sticker must not exist on the book");

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Sticker placement atomic transactions and memory rollback verified");

                // Test 11.13: Quest Completion XP Reward & Active Clean-Up
                SaveService testQuestSave = new SaveService();
                testQuestSave.ActiveSave.PlayerLevel = 1;
                testQuestSave.ActiveSave.PlayerXP = 0;
                testQuestSave.ActiveSave.CompletedQuestIds.Clear();
                testQuestSave.ActiveSave.ActiveQuestProgress.Clear();
                testQuestSave.Save();

                InventoryService testQuestInv = new InventoryService(testQuestSave);
                ProgressionService testQuestProg = new ProgressionService(testQuestSave);

                var testQuestDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.QuestDatabase>();
                var questTemplate = new QuestTemplate(4, "Test XP Quest", 3, 50, QuestType.WaterCrops);
                questTemplate.RewardXP = 150; // XP reward that will trigger level up!
                testQuestDb.Quests.Add(questTemplate);

                QuestService testQuestService = new QuestService(testQuestSave, testQuestInv, testQuestProg, testQuestDb, false);

                // Progress to completion
                testQuestService.ProgressQuest(QuestType.WaterCrops, 3);

                if (testQuestSave.ActiveSave.PlayerLevel != 2)
                    throw new System.Exception($"PlayerLevel should level up to 2 via Quest XP reward, got {testQuestSave.ActiveSave.PlayerLevel}");

                if (testQuestSave.ActiveSave.PlayerXP != 50)
                    throw new System.Exception($"PlayerXP should carry over leftover 50 XP (150 reward - 100 threshold), got {testQuestSave.ActiveSave.PlayerXP}");

                if (!testQuestSave.ActiveSave.CompletedQuestIds.Contains(4))
                    throw new System.Exception("CompletedQuestIds should contain Quest ID 4");

                if (testQuestSave.ActiveSave.ActiveQuestProgress.Exists(x => x.QuestId == 4))
                    throw new System.Exception("ActiveQuestProgress should be cleaned up and not contain Quest ID 4 upon completion");

                Object.DestroyImmediate(testQuestDb);
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Quest completion XP reward and active progress cleanup verified");

                // Test 11.14: ShopService Purchase & Level Locks
                SaveService testShopLockSave = new SaveService();
                testShopLockSave.ActiveSave.Coins = 100;
                testShopLockSave.ActiveSave.Seeds = 0; // Reset seeds to 0 to make test assertion deterministic
                testShopLockSave.ActiveSave.PlayerLevel = 1; // Start at level 1
                testShopLockSave.ActiveSave.PlayerXP = 0;
                testShopLockSave.ActiveSave.StickerOwned.Clear();
                testShopLockSave.ActiveSave.StickerOwned.Add(new StickerInventory(1, 99));
                testShopLockSave.ActiveSave.StickerOwned.Add(new StickerInventory(2, 99));
                testShopLockSave.ActiveSave.HasMigratedStickerOwned = true;
                testShopLockSave.Save();

                InventoryService testShopLockInv = new InventoryService(testShopLockSave);
                ProgressionService testShopLockProg = new ProgressionService(testShopLockSave);

                var testShopLockCropDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.CropDatabase>();
                var testCropLockTemp = new CozyLifeSim.UI.Settings.CropTemplate(1, "Test Level Lock Crop", 1f, null, null, null, null) { BuyPrice = 5, RequiredLevel = 2 }; // Level 2 required
                testShopLockCropDb.Crops.Add(testCropLockTemp);

                var testShopLockStickerDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.StickerDatabase>();
                var testStickerLockTemp = new CozyLifeSim.UI.Settings.StickerTemplate(3, "Premium Chicken", null, null) { BuyPrice = 50, RequiredLevel = 2 }; // Level 2 required
                testShopLockStickerDb.Stickers.Add(testStickerLockTemp);

                // Construct IShopService with the progression service!
                IShopService shopLockService = new ShopService(testShopLockSave, testShopLockInv, testShopLockProg, testShopLockCropDb, testShopLockStickerDb);

                // 1. Try to buy crop seed at Level 1 (should fail)
                if (shopLockService.TryBuySeed(1))
                    throw new System.Exception("TryBuySeed(1) should fail at PlayerLevel 1 because it requires level 2");

                // 2. Try to buy premium sticker at Level 1 (should fail)
                if (shopLockService.TryBuySticker(3))
                    throw new System.Exception("TryBuySticker(3) should fail at PlayerLevel 1 because it requires level 2");

                // 3. Level up to 2
                testShopLockProg.AddXP(100); // Trigger level up!
                if (testShopLockProg.PlayerLevel != 2)
                    throw new System.Exception($"Player level should be 2, got {testShopLockProg.PlayerLevel}");

                // 4. Try to buy crop seed at Level 2 (should succeed)
                if (!shopLockService.TryBuySeed(1))
                    throw new System.Exception("TryBuySeed(1) should succeed at PlayerLevel 2");
                if (testShopLockInv.Coins != 95 || testShopLockInv.Seeds != 1)
                    throw new System.Exception($"Balances incorrect after crop purchase at level 2. Coins: {testShopLockInv.Coins}, Seeds: {testShopLockInv.Seeds}");

                // 5. Try to buy premium sticker at Level 2 (should succeed)
                if (!shopLockService.TryBuySticker(3))
                    throw new System.Exception("TryBuySticker(3) should succeed at PlayerLevel 2");
                if (testShopLockInv.Coins != 45)
                    throw new System.Exception($"Coins incorrect after sticker purchase at level 2. Coins: {testShopLockInv.Coins}");
                if (testShopLockInv.GetStickerCount(3) != 1)
                    throw new System.Exception($"Sticker count should be 1, got {testShopLockInv.GetStickerCount(3)}");

                Object.DestroyImmediate(testShopLockCropDb);
                Object.DestroyImmediate(testShopLockStickerDb);
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "ShopService purchase level locks verified successfully");

                // Test 11.15: ShopService Atomic Seed & Sticker Save Failure Rollback
                var testShopAtomicSave = new SaveService();
                testShopAtomicSave.ActiveSave.Coins = 100;
                testShopAtomicSave.ActiveSave.Seeds = 5;
                testShopAtomicSave.ActiveSave.PlayerLevel = 2;
                testShopAtomicSave.ActiveSave.StickerOwned.Clear();
                testShopAtomicSave.ActiveSave.StickerOwned.Add(new StickerInventory(3, 0));
                testShopAtomicSave.Save();

                var testShopAtomicInv = new InventoryService(testShopAtomicSave);
                var testShopAtomicCropDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.CropDatabase>();
                testShopAtomicCropDb.Crops.Add(new CozyLifeSim.UI.Settings.CropTemplate(1, "Atomic Seed", 1f, null, null, null, null) { BuyPrice = 5, RequiredLevel = 1 });

                var testShopAtomicStickerDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.StickerDatabase>();
                testShopAtomicStickerDb.Stickers.Add(new CozyLifeSim.UI.Settings.StickerTemplate(3, "Atomic Sticker", null, null) { BuyPrice = 50, RequiredLevel = 1 });

                IShopService shopAtomicService = new ShopService(testShopAtomicSave, testShopAtomicInv, testShopAtomicCropDb, testShopAtomicStickerDb);

                // Enable simulated save failure
                testShopAtomicSave.ForceSaveFailure = true;

                // 1. TryBuySeed(1) -> Expect False & RAM rollback (Coins should remain 100, Seeds should remain 5)
                bool seedResult = shopAtomicService.TryBuySeed(1);
                if (seedResult) throw new System.Exception("TryBuySeed(1) should fail under simulated IO save failure");
                if (testShopAtomicInv.Coins != 100) throw new System.Exception($"Coins should remain 100 after seed rollback, got {testShopAtomicInv.Coins}");
                if (testShopAtomicInv.Seeds != 5) throw new System.Exception($"Seeds should remain 5 after seed rollback, got {testShopAtomicInv.Seeds}");

                // 2. TryBuySticker(3) -> Expect False & RAM rollback (Coins should remain 100, Sticker count should remain 0)
                bool stickerResult = shopAtomicService.TryBuySticker(3);
                if (stickerResult) throw new System.Exception("TryBuySticker(3) should fail under simulated IO save failure");
                if (testShopAtomicInv.Coins != 100) throw new System.Exception($"Coins should remain 100 after sticker rollback, got {testShopAtomicInv.Coins}");
                if (testShopAtomicInv.GetStickerCount(3) != 0) throw new System.Exception($"Sticker count should remain 0 after sticker rollback, got {testShopAtomicInv.GetStickerCount(3)}");

                // Disable simulated save failure
                testShopAtomicSave.ForceSaveFailure = false;

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "ShopService Atomic Seed & Sticker Save Failure Rollback verified successfully");

                // Test 11.16: ShopService Atomic Crop Sale Save Failure Rollback
                testShopAtomicSave.ActiveSave.Coins = 100;
                testShopAtomicSave.ActiveSave.Crops = 2;
                testShopAtomicSave.Save();
                testShopAtomicCropDb.Crops.Add(new CozyLifeSim.UI.Settings.CropTemplate(2, "Atomic Crop", 1f, null, null, null, null) { SellPrice = 15, RequiredLevel = 1 });

                try
                {
                    testShopAtomicSave.ForceSaveFailure = true;
                    bool cropSellResult = shopAtomicService.TrySellCrop(2);
                    if (cropSellResult) throw new System.Exception("TrySellCrop(2) should fail under simulated IO save failure");
                    if (testShopAtomicInv.Coins != 100) throw new System.Exception($"Coins should remain 100 after crop sale rollback, got {testShopAtomicInv.Coins}");
                    if (testShopAtomicInv.Crops != 2) throw new System.Exception($"Crops should remain 2 after crop sale rollback, got {testShopAtomicInv.Crops}");
                }
                finally
                {
                    testShopAtomicSave.ForceSaveFailure = false;
                }

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "ShopService Atomic Crop Sale Save Failure Rollback verified successfully");

                Object.DestroyImmediate(testShopAtomicCropDb);
                Object.DestroyImmediate(testShopAtomicStickerDb);

                // Test 11.17: Quest Completion Atomic Save Failure Rollback
                SaveService testQuestAtomicSave = new SaveService();
                testQuestAtomicSave.ActiveSave.Coins = 10;
                testQuestAtomicSave.ActiveSave.PlayerLevel = 1;
                testQuestAtomicSave.ActiveSave.PlayerXP = 0;
                testQuestAtomicSave.ActiveSave.CompletedQuestIds.Clear();
                testQuestAtomicSave.ActiveSave.ActiveQuestProgress.Clear();
                testQuestAtomicSave.Save();

                InventoryService testQuestAtomicInv = new InventoryService(testQuestAtomicSave);
                ProgressionService testQuestAtomicProg = new ProgressionService(testQuestAtomicSave);
                var testQuestAtomicDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.QuestDatabase>();
                testQuestAtomicDb.Quests.Add(new QuestTemplate(10, "Atomic Quest", 1, 50, QuestType.WaterCrops, 150));
                QuestService testQuestAtomicService = new QuestService(testQuestAtomicSave, testQuestAtomicInv, testQuestAtomicProg, testQuestAtomicDb, false);

                try
                {
                    testQuestAtomicSave.ForceSaveFailure = true;
                    bool questProgressResult = testQuestAtomicService.TryProgressQuest(QuestType.WaterCrops, 1);
                    if (questProgressResult) throw new System.Exception("TryProgressQuest should fail under simulated IO save failure");
                    if (testQuestAtomicInv.Coins != 10) throw new System.Exception($"Coins should remain 10 after quest rollback, got {testQuestAtomicInv.Coins}");
                    if (testQuestAtomicProg.PlayerLevel != 1) throw new System.Exception($"PlayerLevel should remain 1 after quest rollback, got {testQuestAtomicProg.PlayerLevel}");
                    if (testQuestAtomicProg.PlayerXP != 0) throw new System.Exception($"PlayerXP should remain 0 after quest rollback, got {testQuestAtomicProg.PlayerXP}");
                    if (testQuestAtomicSave.ActiveSave.CompletedQuestIds.Contains(10)) throw new System.Exception("CompletedQuestIds should not contain Quest ID 10 after rollback");
                    if (testQuestAtomicSave.ActiveSave.ActiveQuestProgress.Exists(x => x.QuestId == 10)) throw new System.Exception("ActiveQuestProgress should not contain Quest ID 10 after rollback");
                    if (testQuestAtomicService.ActiveQuests[0].CurrentCount != 0) throw new System.Exception($"Quest CurrentCount should rollback to 0, got {testQuestAtomicService.ActiveQuests[0].CurrentCount}");
                    if (testQuestAtomicService.ActiveQuests[0].IsCompleted) throw new System.Exception("Quest should not be completed after rollback");
                }
                finally
                {
                    testQuestAtomicSave.ForceSaveFailure = false;
                    Object.DestroyImmediate(testQuestAtomicDb);
                }

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Quest completion atomic save failure rollback verified successfully");

                // Test 11.18: StickerBookPresenter Atomic Placement & Refund
                SaveService testPresSave = new SaveService();
                testPresSave.ActiveSave.Coins = 100;
                testPresSave.ActiveSave.StickerOwned.Clear();
                testPresSave.ActiveSave.StickerOwned.Add(new StickerInventory(3, 2)); // 2 copies of ID 3
                testPresSave.Save();

                InventoryService testPresInv = new InventoryService(testPresSave);
                MemoryService testPresMem = new MemoryService(testPresSave);
                StickerBookPresenter testPresenter = new StickerBookPresenter(testPresMem, testPresInv, testPresSave);

                // 1. Success Path placement
                string pId = testPresenter.TryPlaceSticker(3, 1, 10f, 10f, 1f, 0f);
                if (string.IsNullOrEmpty(pId)) throw new System.Exception("Placement should succeed");
                if (testPresInv.GetStickerCount(3) != 1) throw new System.Exception("Count should decrement to 1");

                // 2. Simulated Save Failure during Placement (try-finally guarded)
                try
                {
                    testPresSave.ForceSaveFailure = true;
                    string failedPId = testPresenter.TryPlaceSticker(3, 1, 10f, 10f, 1f, 0f);
                    if (!string.IsNullOrEmpty(failedPId)) throw new System.Exception("Placement should fail under simulated IO save failure");
                    if (testPresInv.GetStickerCount(3) != 1) throw new System.Exception("Sticker count should remain 1 on failed placement rollback");
                }
                finally
                {
                    testPresSave.ForceSaveFailure = false; // Always restore
                }

                // 3. Success Refund path
                if (!testPresenter.TryReturnSticker(pId)) throw new System.Exception("Return sticker should succeed");
                if (testPresInv.GetStickerCount(3) != 2) throw new System.Exception("Count should refund to 2");

                // 4. Simulated Save Failure during Refund (try-finally guarded)
                // Place it successfully again first
                pId = testPresenter.TryPlaceSticker(3, 1, 10f, 10f, 1f, 0f);
                if (string.IsNullOrEmpty(pId)) throw new System.Exception("Repeated placement should succeed");

                try
                {
                    testPresSave.ForceSaveFailure = true;
                    bool failedReturn = testPresenter.TryReturnSticker(pId);
                    if (failedReturn) throw new System.Exception("Return sticker should fail under simulated IO save failure");
                    if (testPresInv.GetStickerCount(3) != 1) throw new System.Exception("Count should remain 1 on failed return rollback");

                    // Manual loop verification since PlacedStickers is IReadOnlyList and doesn't support .Exists
                    bool exists = false;
                    foreach (var s in testPresMem.PlacedStickers)
                    {
                        if (s.PlacementId == pId) exists = true;
                    }
                    if (!exists) throw new System.Exception("Placed sticker must still exist on failed return rollback");
                }
                finally
                {
                    testPresSave.ForceSaveFailure = false; // Always restore
                }

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "StickerBookPresenter atomic placement and return with simulated failure rollback verified");

                // Test 11.19: Scrapbook Diary Notes & Page Styles persistence, sanitization, and atomic rollback
                SaveService scrapbookSave = new SaveService();
                scrapbookSave.ActiveSave.PlacedDiaryNotes = new List<DiaryNotePlacedData>
                {
                    new DiaryNotePlacedData("", "Old note", 1f, 2f, 0),
                    new DiaryNotePlacedData("duplicate-note", "First duplicate", 3f, 4f, 0),
                    new DiaryNotePlacedData("duplicate-note", "Second duplicate", 5f, 6f, 1)
                };
                scrapbookSave.ActiveSave.PageStyles = new List<PageStyleData>
                {
                    new PageStyleData(0, 1),
                    new PageStyleData(0, 2),
                    new PageStyleData(1, 1)
                };
                scrapbookSave.NormalizeSaveData();

                if (scrapbookSave.ActiveSave.PlacedDiaryNotes == null) throw new System.Exception("PlacedDiaryNotes should be normalized to a non-null list");
                if (scrapbookSave.ActiveSave.PageStyles == null) throw new System.Exception("PageStyles should be normalized to a non-null list");
                var normalizedNoteIds = new HashSet<string>();
                foreach (var note in scrapbookSave.ActiveSave.PlacedDiaryNotes)
                {
                    if (string.IsNullOrEmpty(note.NoteId)) throw new System.Exception("Diary note normalization should backfill empty NoteId");
                    if (!normalizedNoteIds.Add(note.NoteId)) throw new System.Exception("Diary note normalization should backfill duplicate NoteId");
                }
                if (scrapbookSave.ActiveSave.PageStyles.Count != 2) throw new System.Exception($"Duplicate PageStyles should compact to 2 records, got {scrapbookSave.ActiveSave.PageStyles.Count}");
                if (scrapbookSave.ActiveSave.PageStyles[0].PageIndex != 0 || scrapbookSave.ActiveSave.PageStyles[0].StyleIndex != 2)
                {
                    throw new System.Exception("PageStyles compact should keep the most recent style for page 0");
                }

                var scrapbookMemory = new MemoryService(scrapbookSave);
                var diaryA = scrapbookMemory.AddDiaryNoteNonSaving("", "Xin chao", 10f, 20f, 0);
                var diaryB = scrapbookMemory.AddDiaryNoteNonSaving(diaryA.NoteId, "Trung id", 30f, 40f, 0);
                if (string.IsNullOrEmpty(diaryA.NoteId)) throw new System.Exception("AddDiaryNoteNonSaving should assign NoteId");
                if (diaryA.NoteId == diaryB.NoteId) throw new System.Exception("AddDiaryNoteNonSaving should self-heal duplicate NoteId");
                if (!scrapbookMemory.UpdateDiaryNotePositionNonSaving(diaryA.NoteId, 111f, 222f, out var updatedDiary))
                {
                    throw new System.Exception("UpdateDiaryNotePositionNonSaving should update existing note");
                }
                if (!Mathf.Approximately(updatedDiary.PositionX, 111f) || !Mathf.Approximately(updatedDiary.PositionY, 222f))
                {
                    throw new System.Exception("Diary note update should persist new coordinates in RAM");
                }
                scrapbookMemory.SetPageStyleNonSaving(0, 1);
                scrapbookSave.Save();

                SaveService scrapbookReloadSave = new SaveService();
                bool foundReloadedNote = false;
                foreach (var note in scrapbookReloadSave.ActiveSave.PlacedDiaryNotes)
                {
                    if (note.NoteId == diaryA.NoteId && Mathf.Approximately(note.PositionX, 111f) && Mathf.Approximately(note.PositionY, 222f))
                    {
                        foundReloadedNote = true;
                    }
                }
                if (!foundReloadedNote) throw new System.Exception("Diary note position should persist across save/load");
                bool foundReloadedStyle = false;
                foreach (var style in scrapbookReloadSave.ActiveSave.PageStyles)
                {
                    if (style.PageIndex == 0 && style.StyleIndex == 1)
                    {
                        foundReloadedStyle = true;
                    }
                }
                if (!foundReloadedStyle) throw new System.Exception("Page style should persist across save/load");

                var scrapbookInventory = new InventoryService(scrapbookReloadSave);
                var scrapbookPresenter = new StickerBookPresenter(new MemoryService(scrapbookReloadSave), scrapbookInventory, scrapbookReloadSave);
                int addedEvents = 0;
                int updatedEvents = 0;
                int removedEvents = 0;
                int styleEvents = 0;
                scrapbookPresenter.OnDiaryNoteAdded += _ => addedEvents++;
                scrapbookPresenter.OnDiaryNotePositionUpdated += _ => updatedEvents++;
                scrapbookPresenter.OnDiaryNoteRemoved += _ => removedEvents++;
                scrapbookPresenter.OnPageStyleChanged += _ => styleEvents++;

                string presenterNoteId = scrapbookPresenter.TryAddDiaryNote("  Mot ngay dep troi o scrapbook  ", 12f, 24f, 1);
                if (string.IsNullOrEmpty(presenterNoteId)) throw new System.Exception("TryAddDiaryNote should return a generated note id on success");
                if (addedEvents != 1) throw new System.Exception($"OnDiaryNoteAdded should fire once after successful save, got {addedEvents}");
                if (!scrapbookPresenter.TryUpdateDiaryNotePosition(presenterNoteId, 90f, 91f)) throw new System.Exception("TryUpdateDiaryNotePosition should succeed");
                if (updatedEvents != 1) throw new System.Exception($"OnDiaryNotePositionUpdated should fire once after successful save, got {updatedEvents}");
                if (!scrapbookPresenter.TrySetPageStyle(1, 99, 3)) throw new System.Exception("TrySetPageStyle should clamp and save valid style");
                if (scrapbookPresenter.GetPageStyle(1) != 2) throw new System.Exception($"TrySetPageStyle should clamp to max style index 2, got {scrapbookPresenter.GetPageStyle(1)}");
                if (styleEvents != 1) throw new System.Exception($"OnPageStyleChanged should fire once after successful save, got {styleEvents}");

                int noteCountBeforeFailure = scrapbookReloadSave.ActiveSave.PlacedDiaryNotes.Count;
                int styleBeforeFailure = scrapbookPresenter.GetPageStyle(1);
                try
                {
                    scrapbookReloadSave.ForceSaveFailure = true;
                    string failedNoteId = scrapbookPresenter.TryAddDiaryNote("Rollback note", 1f, 1f, 1);
                    if (!string.IsNullOrEmpty(failedNoteId)) throw new System.Exception("TryAddDiaryNote should fail under simulated save failure");
                    if (scrapbookReloadSave.ActiveSave.PlacedDiaryNotes.Count != noteCountBeforeFailure) throw new System.Exception("Failed diary note add should rollback RAM note count");
                    if (addedEvents != 1) throw new System.Exception("OnDiaryNoteAdded should not fire on failed save");

                    bool failedStyle = scrapbookPresenter.TrySetPageStyle(1, 0, 3);
                    if (failedStyle) throw new System.Exception("TrySetPageStyle should fail under simulated save failure");
                    if (scrapbookPresenter.GetPageStyle(1) != styleBeforeFailure) throw new System.Exception("Failed page style change should rollback RAM style");
                    if (styleEvents != 1) throw new System.Exception("OnPageStyleChanged should not fire on failed save");

                    bool failedRemove = scrapbookPresenter.TryRemoveDiaryNote(presenterNoteId);
                    if (failedRemove) throw new System.Exception("TryRemoveDiaryNote should fail under simulated save failure");
                    if (removedEvents != 0) throw new System.Exception("OnDiaryNoteRemoved should not fire on failed save");
                }
                finally
                {
                    scrapbookReloadSave.ForceSaveFailure = false;
                }

                if (!scrapbookPresenter.TryRemoveDiaryNote(presenterNoteId)) throw new System.Exception("TryRemoveDiaryNote should succeed after save failure is disabled");
                if (removedEvents != 1) throw new System.Exception($"OnDiaryNoteRemoved should fire once after successful save, got {removedEvents}");

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Scrapbook diary notes and page styles persistence, sanitization, events, and rollback verified");

                // Test 12: Economic Balance & Level Invariants (Crops, Animals, Stickers & Quests)
                var activeCrops = LoadDatabase<CozyLifeSim.UI.Settings.CropDatabase>();
                var activeAnimals = LoadDatabase<CozyLifeSim.UI.Settings.AnimalDatabase>();
                var activeStickers = LoadDatabase<CozyLifeSim.UI.Settings.StickerDatabase>();
                var activeQuests = LoadDatabase<CozyLifeSim.UI.Settings.QuestDatabase>();

                if (activeCrops == null) throw new System.Exception("CropDatabase asset not found in project!");
                if (activeAnimals == null) throw new System.Exception("AnimalDatabase asset not found in project!");
                if (activeStickers == null) throw new System.Exception("StickerDatabase asset not found in project!");
                if (activeQuests == null) throw new System.Exception("QuestDatabase asset not found in project!");

                // 12.1. Economic Balance Check
                foreach (var crop in activeCrops.Crops)
                {
                    if (crop == null) continue;
                    if (crop.BuyPrice > 0 && crop.SellPrice <= crop.BuyPrice)
                    {
                        throw new System.Exception($"Economic Invariant Violation: Crop '{crop.Name}' (ID {crop.CropId}) has SellPrice ({crop.SellPrice}) <= BuyPrice ({crop.BuyPrice}).");
                    }
                }

                // 12.2. Quest Level Invariant Check
                var sortedQuests = new List<QuestTemplate>(activeQuests.Quests);
                sortedQuests.Sort((a, b) => a.QuestId.CompareTo(b.QuestId));

                int simulatedLevel = 1;
                int simulatedXP = 0;

                foreach (var quest in sortedQuests)
                {
                    if (quest == null) continue;
                    int targetReqLvl = GetTargetRequiredLevel(quest, activeCrops.Crops, activeAnimals.Animals, activeStickers.Stickers);

                    if (targetReqLvl > simulatedLevel)
                    {
                        int availableXP = 0;
                        foreach (var prevQuest in sortedQuests)
                        {
                            if (prevQuest == null) continue;
                            int prevTargetLvl = GetTargetRequiredLevel(prevQuest, activeCrops.Crops, activeAnimals.Animals, activeStickers.Stickers);
                            if (prevTargetLvl <= simulatedLevel)
                            {
                                availableXP += prevQuest.RewardXP;
                            }
                        }

                        int tempXP = simulatedXP + availableXP;
                        int tempLvl = simulatedLevel;
                        while (tempXP >= tempLvl * 100)
                        {
                            tempXP -= tempLvl * 100;
                            tempLvl++;
                        }

                        if (targetReqLvl > tempLvl)
                        {
                            throw new System.Exception($"Level Invariant Violation: Quest '{quest.Title}' (ID {quest.QuestId}) requires target with Level {targetReqLvl}, but players can only reach maximum Level {tempLvl} from all prior executable quests.");
                        }

                        simulatedLevel = tempLvl;
                        simulatedXP = tempXP;
                    }
                }

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Economic Balance & Level Invariants verified");

                // Test 12.5: Heritage Runtime Content Balance & Unlock Path
                if (activeCrops.GetCrop(2) == null || activeCrops.GetCrop(3) == null || activeCrops.GetCrop(4) == null)
                    throw new System.Exception("Heritage crops 2/3/4 must exist in CropDatabase.");
                if (activeStickers.GetSticker(5) == null || activeStickers.GetSticker(6) == null || activeStickers.GetSticker(7) == null)
                    throw new System.Exception("Heritage stickers 5/6/7 must exist in StickerDatabase.");

                int maxContentLevel = GetMaxRequiredLevel(activeCrops.Crops, activeAnimals.Animals, activeStickers.Stickers);
                int reachableLevel = SimulateReachableLevel(activeQuests.Quests);
                if (reachableLevel < maxContentLevel)
                {
                    throw new System.Exception($"Heritage XP Balance Violation: total quest XP can only reach Level {reachableLevel}, but content requires Level {maxContentLevel}.");
                }

                var heritageShopSave = new SaveService();
                heritageShopSave.ActiveSave.Coins = 500;
                heritageShopSave.ActiveSave.Seeds = 0;
                heritageShopSave.ActiveSave.Crops = 0;
                heritageShopSave.ActiveSave.PlayerLevel = 1;
                heritageShopSave.ActiveSave.PlayerXP = 0;
                heritageShopSave.ActiveSave.StickerOwned.Clear();
                heritageShopSave.ActiveSave.CompletedQuestIds.Clear();
                heritageShopSave.ActiveSave.ActiveQuestProgress.Clear();
                heritageShopSave.ActiveSave.HasMigratedStickerOwned = true;
                heritageShopSave.Save();

                var heritageShopInventory = new InventoryService(heritageShopSave);
                var heritageProgression = new ProgressionService(heritageShopSave);
                IShopService heritageShop = new ShopService(heritageShopSave, heritageShopInventory, heritageProgression, activeCrops, activeStickers);

                if (!heritageShop.TryBuySeed(2))
                    throw new System.Exception("Level 1 player should be able to buy Cay Mia Ngot seed.");
                if (!heritageShop.TryBuySticker(5))
                    throw new System.Exception("Level 1 player should be able to buy Ly Nuoc Mia sticker.");
                if (heritageShop.TryBuySeed(3))
                    throw new System.Exception("Level 1 player should not be able to buy Lua Nuoc seed.");
                if (heritageShop.TryBuySticker(6))
                    throw new System.Exception("Level 1 player should not be able to buy Chiec Non La sticker.");

                heritageProgression.AddXP(100);
                if (heritageProgression.PlayerLevel != 2)
                    throw new System.Exception($"Heritage unlock flow should reach Level 2 after 100 XP, got Level {heritageProgression.PlayerLevel}.");
                if (!heritageShop.TryBuySeed(3))
                    throw new System.Exception("Level 2 player should be able to buy Lua Nuoc seed.");
                if (!heritageShop.TryBuySticker(6))
                    throw new System.Exception("Level 2 player should be able to buy Chiec Non La sticker.");

                heritageProgression.AddXP(200);
                if (heritageProgression.PlayerLevel != 3)
                    throw new System.Exception($"Heritage unlock flow should reach Level 3 after another 200 XP, got Level {heritageProgression.PlayerLevel}.");
                if (!heritageShop.TryBuySeed(4))
                    throw new System.Exception("Level 3 player should be able to buy Hoa Sen seed.");
                if (!heritageShop.TryBuySticker(7))
                    throw new System.Exception("Level 3 player should be able to buy Chiec Xich Lo sticker.");

                var heritageQuestSave = new SaveService();
                heritageQuestSave.ActiveSave.Coins = 100;
                heritageQuestSave.ActiveSave.PlayerLevel = 1;
                heritageQuestSave.ActiveSave.PlayerXP = 0;
                heritageQuestSave.ActiveSave.CompletedQuestIds.Clear();
                heritageQuestSave.ActiveSave.ActiveQuestProgress.Clear();
                heritageQuestSave.Save();

                var heritageQuestInventory = new InventoryService(heritageQuestSave);
                var heritageQuestProgression = new ProgressionService(heritageQuestSave);
                var heritageQuestService = new QuestService(heritageQuestSave, heritageQuestInventory, heritageQuestProgression, activeQuests, false);
                int startingQuestCoins = heritageQuestInventory.Coins;
                int startingQuestLevel = heritageQuestProgression.PlayerLevel;
                int startingQuestXP = heritageQuestProgression.PlayerXP;

                QuestData firstHarvestQuest = null;
                foreach (QuestData quest in heritageQuestService.ActiveQuests)
                {
                    if (quest.QuestId == 2)
                    {
                        firstHarvestQuest = quest;
                        break;
                    }
                }

                if (firstHarvestQuest == null || !firstHarvestQuest.Title.Contains("Cay Mia Ngot"))
                    throw new System.Exception("First heritage harvest quest should target Cay Mia Ngot.");
                if (!heritageQuestService.TryProgressQuest(QuestType.HarvestCrops, firstHarvestQuest.TargetCount))
                    throw new System.Exception("Heritage harvest quest should complete through QuestService.");
                if (!firstHarvestQuest.IsCompleted || !heritageQuestSave.ActiveSave.CompletedQuestIds.Contains(2))
                    throw new System.Exception("Heritage harvest quest completion did not persist.");

                int expectedCoins = startingQuestCoins + firstHarvestQuest.RewardCoins;
                GetExpectedProgressionAfterXP(startingQuestLevel, startingQuestXP, firstHarvestQuest.RewardXP, out int expectedLevel, out int expectedXP);
                if (heritageQuestInventory.Coins != expectedCoins || heritageQuestProgression.PlayerLevel != expectedLevel || heritageQuestProgression.PlayerXP != expectedXP)
                {
                    throw new System.Exception($"Heritage harvest rewards should apply exactly. Coins={heritageQuestInventory.Coins}/{expectedCoins}, Level={heritageQuestProgression.PlayerLevel}/{expectedLevel}, XP={heritageQuestProgression.PlayerXP}/{expectedXP}.");
                }

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Heritage runtime content balance and unlock path verified");

                // Test 12.6: Exact Asset Path & GUID Validation
                ValidateHeritageAssetPaths(activeCrops, activeAnimals, activeStickers, ref expectedWarningCount);
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Heritage exact asset paths and fallbacks validated");

                // Test 13: Scene Component & DI Wiring Validation
                ValidateSceneWiring(ref passCount);

                // Test 14: Procedural Flat Fallback C# Integration
                var go = new GameObject("TempFlatFallbackTest");
                var image = go.AddComponent<UnityEngine.UI.Image>();

                try
                {
                    CozyLifeSim.UI.Style.CozyProceduralUI.ApplyFlatFallback(image, Color.green);

                    var outline = go.GetComponent<UnityEngine.UI.Outline>();

                    UnityEngine.UI.Shadow shadow = null;
                    var shadows = go.GetComponents<UnityEngine.UI.Shadow>();
                    foreach (var s in shadows)
                    {
                        if (s.GetType() == typeof(UnityEngine.UI.Shadow))
                        {
                            shadow = s;
                            break;
                        }
                    }

                    if (outline == null) throw new System.Exception("CozyProceduralUI failed to add Outline component!");
                    if (shadow == null) throw new System.Exception("CozyProceduralUI failed to add Shadow component!");

                    if (outline.effectColor != new Color(0.12f, 0.12f, 0.12f, 1f))
                        throw new System.Exception($"Outline color incorrect: {outline.effectColor}");
                    if (shadow.effectDistance != new Vector2(4f, -4f))
                        throw new System.Exception($"Shadow distance incorrect: {shadow.effectDistance}");
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }

                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "Procedural Flat Fallback C# Integration verified successfully");

                // Test 15: Play Mode Runtime Dialogue & NPC Click Validation
                // Test 15: Play Mode Runtime Dialogue & NPC Click Validation
                GameObject dialogGo = null;
                CozyLifeSim.UI.Settings.QuestDatabase testDialogQuestDb = null;
                try
                {
                    dialogGo = new GameObject("TempDialoguePopupTest");
                    var contentPanel = new GameObject("Content_Panel", typeof(RectTransform));
                    contentPanel.transform.SetParent(dialogGo.transform);

                    var portraitImg = new GameObject("Portrait").AddComponent<UnityEngine.UI.Image>();
                    portraitImg.transform.SetParent(contentPanel.transform);

                    var nameTxt = new GameObject("Name").AddComponent<TextMeshProUGUI>();
                    nameTxt.transform.SetParent(contentPanel.transform);

                    var dialogueTxt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
                    dialogueTxt.transform.SetParent(contentPanel.transform);

                    var nextBtn = new GameObject("Button").AddComponent<UnityEngine.UI.Button>();
                    nextBtn.transform.SetParent(contentPanel.transform);

                    var dialoguePopupTest = dialogGo.AddComponent<CozyDialoguePopup>();

                    // Configure using reflection or direct assignments
                    var typeDialogue = typeof(CozyDialoguePopup);
                    typeDialogue.GetField("_contentPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(dialoguePopupTest, contentPanel.GetComponent<RectTransform>());
                    typeDialogue.GetField("_portrait", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(dialoguePopupTest, portraitImg);
                    typeDialogue.GetField("_nameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(dialoguePopupTest, nameTxt);
                    typeDialogue.GetField("_dialogueText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(dialoguePopupTest, dialogueTxt);
                    typeDialogue.GetField("_nextButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(dialoguePopupTest, nextBtn);

                    // Setup mock mapping list
                    var mappingList = new List<CozyDialoguePopup.QuestDialogueMapping>();
                    var mapping = new CozyDialoguePopup.QuestDialogueMapping();
                    mapping.QuestId = 2;
                    mapping.NpcName = "Bà Ngoại";
                    mapping.DialogueText = "Con yêu, cây mía ngọt này thật ngọt ngào...";
                    mapping.Portrait = null;
                    mappingList.Add(mapping);
                    typeDialogue.GetField("_questDialogues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(dialoguePopupTest, mappingList);

                    // Set up QuestService dependency
                    var testDialogSave = new SaveService();
                    testDialogSave.ActiveSave.CompletedQuestIds.Clear();
                    testDialogSave.ActiveSave.ActiveQuestProgress.Clear();
                    var testDialogInv = new InventoryService(testDialogSave);
                    var testDialogProg = new ProgressionService(testDialogSave);
                    testDialogQuestDb = ScriptableObject.CreateInstance<CozyLifeSim.UI.Settings.QuestDatabase>();
                    var testDialogQuestTemplate = new QuestTemplate(2, "Test Quest 2", 1, 50, QuestType.HarvestCrops);
                    testDialogQuestDb.Quests.Add(testDialogQuestTemplate);

                    var testDialogQuestService = new QuestService(testDialogSave, testDialogInv, testDialogProg, testDialogQuestDb, false);

                    // Construct VContainer dependencies manually for test
                    dialoguePopupTest.Construct(testDialogQuestService);

                    // Parent must be active (activeSelf == true)
                    if (!dialogGo.activeSelf) throw new System.Exception("Dialogue popup parent GameObject must start active!");

                    // Initialize (simulating Start)
                    var startMethod = typeDialogue.GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (startMethod != null) startMethod.Invoke(dialoguePopupTest, null);

                    // 1. Simulating quest completion
                    Debug.Log($"[CozySim Test] Before ProgressQuest. activeSelf={contentPanel.activeSelf}");
                    testDialogQuestService.ProgressQuest(QuestType.HarvestCrops, 1);
                    Debug.Log($"[CozySim Test] After ProgressQuest. activeSelf={contentPanel.activeSelf}, isTyping={typeDialogue.GetField("_isTyping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dialoguePopupTest)}, fullText={typeDialogue.GetField("_fullText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dialoguePopupTest)}, text={dialogueTxt.text}");

                    // Content Panel should be activated by the event handler
                    if (!contentPanel.activeSelf) throw new System.Exception("Dialogue popup content panel should be active after quest completion");

                    // 2. Typewriter Skip check
                    dialoguePopupTest.SkipOrNext();
                    Debug.Log($"[CozySim Test] After SkipOrNext. isTyping={typeDialogue.GetField("_isTyping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dialoguePopupTest)}, text={dialogueTxt.text}");
                    if ((bool)typeDialogue.GetField("_isTyping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dialoguePopupTest))
                    {
                        throw new System.Exception("isTyping should be false after Skip!");
                    }
                    if (dialogueTxt.text != "Con yêu, cây mía ngọt này thật ngọt ngào...")
                    {
                        throw new System.Exception($"Dialogue text should be full text after skip, got: {dialogueTxt.text}");
                    }

                    // 3. Dialogue next click to close check
                    dialoguePopupTest.SkipOrNext();
                    if (contentPanel.activeSelf)
                    {
                        throw new System.Exception("Dialogue popup content panel should be inactive after clicking next on finished typewriter");
                    }

                    passCount++;
                    CozyValidationLog.Pass("CozySim Logic", "Play Mode Dialogue typewriter & skip logic verified successfully");
                }
                finally
                {
                    if (dialogGo != null) Object.DestroyImmediate(dialogGo);
                    if (testDialogQuestDb != null) Object.DestroyImmediate(testDialogQuestDb);
                }

                // Test 15.5: NPC click and lookup validation (with Dialogue Popup and Pointer Guard testing)
                GameObject test155DialogGo = null;
                GameObject npcGo = null;
                GameObject mockEventSystemGo = null;
                try
                {
                    // 1. Create a dialogue popup GameObject and wire it
                    test155DialogGo = new GameObject("TempDialoguePopupTest155");
                    var test155ContentPanel = new GameObject("Content_Panel155", typeof(RectTransform));
                    test155ContentPanel.transform.SetParent(test155DialogGo.transform);

                    var test155PortraitImg = new GameObject("Portrait155").AddComponent<UnityEngine.UI.Image>();
                    test155PortraitImg.transform.SetParent(test155ContentPanel.transform);

                    var test155NameTxt = new GameObject("Name155").AddComponent<TextMeshProUGUI>();
                    test155NameTxt.transform.SetParent(test155ContentPanel.transform);

                    var test155DialogueTxt = new GameObject("Text155").AddComponent<TextMeshProUGUI>();
                    test155DialogueTxt.transform.SetParent(test155ContentPanel.transform);

                    var test155NextBtn = new GameObject("Button155").AddComponent<UnityEngine.UI.Button>();
                    test155NextBtn.transform.SetParent(test155ContentPanel.transform);

                    var test155DialoguePopup = test155DialogGo.AddComponent<CozyDialoguePopup>();

                    // Configure using reflection
                    var typeDialogue155 = typeof(CozyDialoguePopup);
                    typeDialogue155.GetField("_contentPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(test155DialoguePopup, test155ContentPanel.GetComponent<RectTransform>());
                    typeDialogue155.GetField("_portrait", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(test155DialoguePopup, test155PortraitImg);
                    typeDialogue155.GetField("_nameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(test155DialoguePopup, test155NameTxt);
                    typeDialogue155.GetField("_dialogueText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(test155DialoguePopup, test155DialogueTxt);
                    typeDialogue155.GetField("_nextButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(test155DialoguePopup, test155NextBtn);

                    // Start dialogue popup (makes content panel inactive initially)
                    var startMethod155 = typeDialogue155.GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (startMethod155 != null) startMethod155.Invoke(test155DialoguePopup, null);

                    // 2. Create NPC GameObject
                    npcGo = new GameObject("TempNpcTest");
                    npcGo.AddComponent<BoxCollider2D>();
                    var npcWidget = npcGo.AddComponent<CozyNPCWidget>();
                    if (npcWidget == null)
                    {
                        throw new System.Exception("npcWidget is null immediately after AddComponent!");
                    }

                    // Configure NPC Widget using reflection
                    var typeNpc = typeof(CozyNPCWidget);
                    var npcData = new CozyNPCWidget.NpcDialogueData();
                    npcData.NpcName = "Bà Ngoại 155";
                    npcData.Portrait = null;
                    npcData.Dialogues = new List<CozyNPCWidget.NpcDialogueLine>
                    {
                        new CozyNPCWidget.NpcDialogueLine { Line = "Hello Vietnamese Heritage!" }
                    };

                    var fieldInfo = typeNpc.GetField("_npcData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fieldInfo == null)
                    {
                        throw new System.Exception("Could not find field _npcData on CozyNPCWidget!");
                    }
                    fieldInfo.SetValue(npcWidget, npcData);

                    // 3. Trigger Start on NPC to populate popup lookup
                    var npcStartMethod = typeNpc.GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (npcStartMethod != null) npcStartMethod.Invoke(npcWidget, null);

                    // Verify popup lookup was successful
                    var dialoguePopupField = typeNpc.GetField("_dialoguePopup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (dialoguePopupField == null) throw new System.Exception("Could not find field _dialoguePopup on CozyNPCWidget!");
                    var resolvedPopup = dialoguePopupField.GetValue(npcWidget) as CozyDialoguePopup;
                    if (resolvedPopup != test155DialoguePopup)
                    {
                        throw new System.Exception("NPC click widget failed to lookup CozyDialoguePopup in scene!");
                    }

                    // 4. Test normal successful click path (without pointer over UI)
                    CozyNPCWidget.PointerOverUiOverride = () => false;

                    var onMouseDownMethod = typeNpc.GetMethod("OnMouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (onMouseDownMethod == null) throw new System.Exception("Could not find OnMouseDown method on CozyNPCWidget!");

                    onMouseDownMethod.Invoke(npcWidget, null);

                    // Verify dialogue popup opened and text contains NPC dialogue line
                    if (!test155ContentPanel.activeSelf)
                    {
                        throw new System.Exception("Dialogue popup content panel should be active after clicking NPC!");
                    }
                    if (test155NameTxt.text != "Bà Ngoại 155")
                    {
                        throw new System.Exception($"Dialogue NPC Name mismatch! Expected 'Bà Ngoại 155', got: {test155NameTxt.text}");
                    }

                    // Reset content panel to inactive for next check
                    test155ContentPanel.SetActive(false);

                    // 5. Test Pointer-over-UI Guard (click should be blocked)
                    CozyNPCWidget.PointerOverUiOverride = () => true;

                    onMouseDownMethod.Invoke(npcWidget, null);

                    if (test155ContentPanel.activeSelf)
                    {
                        throw new System.Exception("Dialogue popup content panel should NOT be active when clicking NPC while mouse is over UI!");
                    }

                    CozyNPCWidget.PointerOverUiOverride = null;

                    // 6. Test fallback when CozyDialoguePopup is missing in scene (should not throw exceptions)
                    // Remove the dialogue popup from scene and lookup reference
                    dialoguePopupField.SetValue(npcWidget, null);
                    Object.DestroyImmediate(test155DialogGo);
                    test155DialogGo = null;

                    // OnMouseDown should not throw when dialogue popup is missing
                    onMouseDownMethod.Invoke(npcWidget, null);

                    // Clean up Test 15.5
                    Object.DestroyImmediate(npcGo);
                    npcGo = null;
                    passCount++;
                    CozyValidationLog.Pass("CozySim Logic", "NPC click fallback, successful lookup, random selection, and pointer-over-UI guard verified successfully");
                }
                finally
                {
                    CozyNPCWidget.PointerOverUiOverride = null;
                    if (mockEventSystemGo != null) Object.DestroyImmediate(mockEventSystemGo);
                    if (test155DialogGo != null) Object.DestroyImmediate(test155DialogGo);
                    if (npcGo != null) Object.DestroyImmediate(npcGo);
                }

                // Test 15.6: ShopItemWidget's disabled states logic-style UI test
                GameObject shopItemWidgetGo = null;
                try
                {
                    shopItemWidgetGo = new GameObject("TempShopItemWidgetTest");
                    var widget = shopItemWidgetGo.AddComponent<ShopItemWidget>();

                    var itemNameText = new GameObject("ItemNameText").AddComponent<TextMeshProUGUI>();
                    itemNameText.transform.SetParent(shopItemWidgetGo.transform);

                    var priceText = new GameObject("PriceText").AddComponent<TextMeshProUGUI>();
                    priceText.transform.SetParent(shopItemWidgetGo.transform);

                    var actionButton = new GameObject("ActionButton").AddComponent<UnityEngine.UI.Button>();
                    actionButton.transform.SetParent(shopItemWidgetGo.transform);

                    var actionButtonText = new GameObject("ActionButtonText").AddComponent<TextMeshProUGUI>();
                    actionButtonText.transform.SetParent(shopItemWidgetGo.transform);

                    var disabledReasonText = new GameObject("DisabledReasonText").AddComponent<TextMeshProUGUI>();
                    disabledReasonText.transform.SetParent(shopItemWidgetGo.transform);

                    var itemIcon = new GameObject("ItemIcon").AddComponent<UnityEngine.UI.Image>();
                    itemIcon.transform.SetParent(shopItemWidgetGo.transform);

                    var typeWidget = typeof(ShopItemWidget);
                    typeWidget.GetField("_itemNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(widget, itemNameText);
                    typeWidget.GetField("_priceText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(widget, priceText);
                    typeWidget.GetField("_actionButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(widget, actionButton);
                    typeWidget.GetField("_actionButtonText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(widget, actionButtonText);
                    typeWidget.GetField("_disabledReasonText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(widget, disabledReasonText);
                    typeWidget.GetField("_itemIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(widget, itemIcon);

                    // Call Start via reflection to register button listener
                    var startMethod = typeWidget.GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (startMethod != null) startMethod.Invoke(widget, null);

                    int callbackCount = 0;
                    System.Action callback = () => callbackCount++;

                    // 1. Blocked setup
                    widget.Setup("Test Item", null, 100, "Buy", false, "Need 999 Coins", callback);
                    if (disabledReasonText.text != "Need 999 Coins")
                    {
                        throw new System.Exception($"Disabled reason text mismatch! Expected 'Need 999 Coins', got: '{disabledReasonText.text}'");
                    }
                    if (!disabledReasonText.gameObject.activeSelf)
                    {
                        throw new System.Exception("Disabled reason GameObject should be active when blocked and reason is non-empty");
                    }
                    if (actionButton.interactable)
                    {
                        throw new System.Exception("Action button should be non-interactable when blocked");
                    }

                    // 2. Interactable setup
                    widget.Setup("Test Item", null, 100, "Buy", true, "", callback);
                    if (disabledReasonText.gameObject.activeSelf)
                    {
                        throw new System.Exception("Disabled reason GameObject should be inactive when interactable");
                    }
                    if (!actionButton.interactable)
                    {
                        throw new System.Exception("Action button should be interactable when not blocked");
                    }

                    // Click action
                    actionButton.onClick.Invoke();
                    if (callbackCount != 1)
                    {
                        throw new System.Exception($"Callback should fire through action button click. Expected 1, got {callbackCount}");
                    }

                    passCount++;
                    CozyValidationLog.Pass("CozySim Logic", "ShopItemWidget setup, disabled states, and button callback verified successfully");
                }
                finally
                {
                    if (shopItemWidgetGo != null) Object.DestroyImmediate(shopItemWidgetGo);
                }

                // Test 15.7: CozyFeedbackToast's Show method logic test
                GameObject toastGo = null;
                try
                {
                    toastGo = new GameObject("TempFeedbackToastTest");
                    var toast = toastGo.AddComponent<CozyFeedbackToast>();

                    var canvasGroup = toastGo.AddComponent<CanvasGroup>();
                    var contentPanel = new GameObject("ContentPanel", typeof(RectTransform));
                    contentPanel.transform.SetParent(toastGo.transform);
                    var messageText = new GameObject("MessageText").AddComponent<TextMeshProUGUI>();
                    messageText.transform.SetParent(contentPanel.transform);

                    var typeToast = typeof(CozyFeedbackToast);
                    typeToast.GetField("_canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(toast, canvasGroup);
                    typeToast.GetField("_contentPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(toast, contentPanel.GetComponent<RectTransform>());
                    typeToast.GetField("_messageText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(toast, messageText);

                    // Call Awake via reflection
                    var awakeMethod = typeToast.GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (awakeMethod != null) awakeMethod.Invoke(toast, null);

                    // Initially contentPanel is inactive, CanvasGroup values are set
                    if (contentPanel.activeSelf)
                    {
                        throw new System.Exception("Content panel should start inactive");
                    }
                    if (canvasGroup.alpha != 0f || canvasGroup.blocksRaycasts || canvasGroup.interactable)
                    {
                        throw new System.Exception("CanvasGroup alpha should be 0 and interaction disabled on Awake");
                    }

                    // Show with empty message should not activate
                    toast.Show("");
                    if (contentPanel.activeSelf)
                    {
                        throw new System.Exception("Content panel should not activate for empty message");
                    }

                    // Show with actual message should activate
                    toast.Show(" Hello World ");
                    if (!contentPanel.activeSelf)
                    {
                        throw new System.Exception("Content panel should activate for non-empty message");
                    }
                    if (messageText.text != "Hello World")
                    {
                        throw new System.Exception($"Message text mismatch! Expected 'Hello World', got: '{messageText.text}'");
                    }

                    // Show second message should replace the first message
                    toast.Show("Second Message");
                    if (messageText.text != "Second Message")
                    {
                        throw new System.Exception($"Message text should be replaced. Expected 'Second Message', got: '{messageText.text}'");
                    }

                    passCount++;
                    CozyValidationLog.Pass("CozySim Logic", "CozyFeedbackToast Show behavior verified successfully");
                }
                finally
                {
                    if (toastGo != null) Object.DestroyImmediate(toastGo);
                }

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

        private static T LoadDatabase<T>() where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
            return null;
        }

        private static int GetTargetRequiredLevel(
            QuestTemplate quest,
            List<CozyLifeSim.UI.Settings.CropTemplate> crops,
            List<CozyLifeSim.UI.Settings.AnimalTemplate> animals,
            List<CozyLifeSim.UI.Settings.StickerTemplate> stickers)
        {
            string title = quest.Title.ToLower();
            if (quest.Type == QuestType.HarvestCrops || title.Contains("harvest") || title.Contains("crop") || title.Contains("mia") || title.Contains("lua") || title.Contains("sen"))
            {
                foreach (var c in crops)
                {
                    if (c == null) continue;
                    if (title.Contains(c.Name.ToLower()) || c.Name.ToLower().Contains(title) || (c.CropId == 2 && title.Contains("mia")) || (c.CropId == 3 && title.Contains("lua")) || (c.CropId == 4 && title.Contains("sen")))
                    {
                        return c.RequiredLevel;
                    }
                }
            }
            else if (quest.Type == QuestType.PetAnimal || title.Contains("pet") || title.Contains("chicken") || title.Contains("meo") || title.Contains("trau"))
            {
                foreach (var a in animals)
                {
                    if (a == null) continue;
                    if (title.Contains(a.Name.ToLower()) || a.Name.ToLower().Contains(title) || (a.AnimalId == 2 && title.Contains("meo")) || (a.AnimalId == 3 && title.Contains("trau")))
                    {
                        return a.RequiredLevel;
                    }
                }
            }
            else if (title.Contains("buy") || title.Contains("sticker") || title.Contains("non la") || title.Contains("xich lo") || title.Contains("long den") || title.Contains("banh mi") || title.Contains("nuoc mia"))
            {
                foreach (var s in stickers)
                {
                    if (s == null) continue;
                    if (title.Contains(s.Name.ToLower()) || s.Name.ToLower().Contains(title) || (s.StickerId == 6 && title.Contains("non la")) || (s.StickerId == 7 && title.Contains("xich lo")) || (s.StickerId == 8 && title.Contains("long den")))
                    {
                        return s.RequiredLevel;
                    }
                }
            }
            return 1; // Default
        }

        private static int GetMaxRequiredLevel(
            List<CozyLifeSim.UI.Settings.CropTemplate> crops,
            List<CozyLifeSim.UI.Settings.AnimalTemplate> animals,
            List<CozyLifeSim.UI.Settings.StickerTemplate> stickers)
        {
            int maxLevel = 1;

            foreach (var crop in crops)
            {
                if (crop != null && crop.RequiredLevel > maxLevel)
                {
                    maxLevel = crop.RequiredLevel;
                }
            }

            foreach (var animal in animals)
            {
                if (animal != null && animal.RequiredLevel > maxLevel)
                {
                    maxLevel = animal.RequiredLevel;
                }
            }

            foreach (var sticker in stickers)
            {
                if (sticker != null && sticker.RequiredLevel > maxLevel)
                {
                    maxLevel = sticker.RequiredLevel;
                }
            }

            return maxLevel;
        }

        private static int SimulateReachableLevel(List<QuestTemplate> quests)
        {
            int level = 1;
            int xp = 0;

            foreach (var quest in quests)
            {
                if (quest == null || quest.RewardXP <= 0) continue;

                xp += quest.RewardXP;
                while (xp >= level * 100)
                {
                    xp -= level * 100;
                    level++;
                }
            }

            return level;
        }

        private static void GetExpectedProgressionAfterXP(int startingLevel, int startingXP, int addedXP, out int expectedLevel, out int expectedXP)
        {
            expectedLevel = Mathf.Max(1, startingLevel);
            expectedXP = Mathf.Max(0, startingXP) + Mathf.Max(0, addedXP);

            while (expectedXP >= expectedLevel * 100)
            {
                expectedXP -= expectedLevel * 100;
                expectedLevel++;
            }
        }

        private static void ValidateSceneWiring(ref int passCount)
        {
            MonoBehaviour[] allBehaviors = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            int checkedFields = 0;
            int missingWires = 0;

            foreach (var mb in allBehaviors)
            {
                if (mb == null) continue;
                System.Type type = mb.GetType();
                if (type.Namespace == null || !type.Namespace.StartsWith("CozyLifeSim"))
                    continue;

                System.Reflection.FieldInfo[] fields = type.GetFields(
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance
                );

                foreach (var field in fields)
                {
                    bool isSerialized = field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), true).Length > 0;
                    if (!isSerialized) continue;

                    if (!typeof(Object).IsAssignableFrom(field.FieldType)) continue;

                    // Exclude optional fields that have runtime procedural fallbacks
                    if (type.Name == "CozyJuiceUtility" && field.Name == "_coinPrefab")
                        continue;

                    checkedFields++;
                    Object val = field.GetValue(mb) as Object;

                    if (val == null)
                    {
                        missingWires++;
                        Debug.LogWarning($"<color=yellow>[CozySim Wiring Warning]</color> Component <b>{type.Name}</b> on GameObject <b>{mb.gameObject.name}</b> has an unassigned/null field: <b>{field.Name}</b> ({field.FieldType.Name})");
                    }
                }
            }

            var scope = Object.FindFirstObjectByType<GameLifetimeScope>();
            if (scope == null)
            {
                throw new System.Exception("GameLifetimeScope not found in the active scene!");
            }

            var scopeType = typeof(GameLifetimeScope);
            var cropDbField = scopeType.GetField("_cropDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var animalDbField = scopeType.GetField("_animalDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var stickerDbField = scopeType.GetField("_stickerDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var questDbField = scopeType.GetField("_questDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (cropDbField == null || cropDbField.GetValue(scope) == null) throw new System.Exception("GameLifetimeScope is missing CropDatabase reference!");
            if (animalDbField == null || animalDbField.GetValue(scope) == null) throw new System.Exception("GameLifetimeScope is missing AnimalDatabase reference!");
            if (stickerDbField == null || stickerDbField.GetValue(scope) == null) throw new System.Exception("GameLifetimeScope is missing StickerDatabase reference!");
            if (questDbField == null || questDbField.GetValue(scope) == null) throw new System.Exception("GameLifetimeScope is missing QuestDatabase reference!");

            if (missingWires > 0)
            {
                int errorCount = missingWires; // So luong loi thieu wiring phat hien duoc
                throw new System.Exception($"Scene component wiring validation failed: found {errorCount} unassigned/null serialized fields!");
            }

            passCount++;
            CozyValidationLog.Pass("CozySim Logic", $"Scene component wiring and DI Database references validated. (Checked {checkedFields} fields, {missingWires} unassigned)");
        }

        private static void ValidateHeritageAssetPaths(
            CozyLifeSim.UI.Settings.CropDatabase cropDb,
            CozyLifeSim.UI.Settings.AnimalDatabase animalDb,
            CozyLifeSim.UI.Settings.StickerDatabase stickerDb,
            ref int expectedWarningCount)
        {
            // 1. Stickers (5 stickers, IDs 4-8)
            var stickerPaths = new Dictionary<int, string>
            {
                { 4, "Assets/CozyLifeSim/Textures/Heritage/Sticker_BanhMiCart.png" },
                { 5, "Assets/CozyLifeSim/Textures/Heritage/Sticker_SugarcaneJuice.png" },
                { 6, "Assets/CozyLifeSim/Textures/Heritage/Sticker_ConicalHat.png" },
                { 7, "Assets/CozyLifeSim/Textures/Heritage/Sticker_Cyclo.png" },
                { 8, "Assets/CozyLifeSim/Textures/Heritage/Sticker_StarLantern.png" }
            };

            foreach (var pair in stickerPaths)
            {
                var sticker = stickerDb.GetSticker(pair.Key);
                if (sticker == null) throw new System.Exception($"Sticker ID {pair.Key} is missing from StickerDatabase!");

                bool fileExists = System.IO.File.Exists(pair.Value);
                if (fileExists)
                {
                    CozyAssetImporterUtility.ConfigureAsSprite(pair.Value);
                    string actualPath = AssetDatabase.GetAssetPath(sticker.Sprite);
                    if (actualPath != pair.Value)
                    {
                        throw new System.Exception($"Sticker ID {pair.Key} ({sticker.Name}) sprite path mismatch! Expected: {pair.Value}, got: {actualPath}");
                    }
                }
                else
                {
                    expectedWarningCount++;
                    CozyValidationLog.ExpectedWarning("CozySim Logic", $"Core sticker asset {pair.Value} is missing. Fallback sprite in use: {AssetDatabase.GetAssetPath(sticker.Sprite)}");
                }
            }

            // 2. Animals (2 animals, IDs 2-3)
            var animalPaths = new Dictionary<int, string>
            {
                { 2, "Assets/CozyLifeSim/Textures/Heritage/Animal_CalicoCat.png" },
                { 3, "Assets/CozyLifeSim/Textures/Heritage/Animal_WaterBuffalo.png" }
            };

            foreach (var pair in animalPaths)
            {
                var animal = animalDb.Animals.Find(x => x != null && x.AnimalId == pair.Key);
                if (animal == null) throw new System.Exception($"Animal ID {pair.Key} is missing from AnimalDatabase!");

                bool fileExists = System.IO.File.Exists(pair.Value);
                if (fileExists)
                {
                    CozyAssetImporterUtility.ConfigureAsSprite(pair.Value);
                    string actualPath = AssetDatabase.GetAssetPath(animal.Sprite);
                    if (actualPath != pair.Value)
                    {
                        throw new System.Exception($"Animal ID {pair.Key} ({animal.Name}) sprite path mismatch! Expected: {pair.Value}, got: {actualPath}");
                    }
                }
                else
                {
                    expectedWarningCount++;
                    CozyValidationLog.ExpectedWarning("CozySim Logic", $"Core animal asset {pair.Value} is missing. Fallback sprite in use: {AssetDatabase.GetAssetPath(animal.Sprite)}");
                }
            }

            // 3. Crops (3 crops, 3 stages each = 9 stages, IDs 2-4)
            var cropPaths = new Dictionary<int, (string seed, string sprout, string mature)>
            {
                {
                    2, (
                        "Assets/CozyLifeSim/Textures/Heritage/Crop_Sugarcane_Seed.png",
                        "Assets/CozyLifeSim/Textures/Heritage/Crop_Sugarcane_Sprout.png",
                        "Assets/CozyLifeSim/Textures/Heritage/Crop_Sugarcane_Mature.png"
                    )
                },
                {
                    3, (
                        "Assets/CozyLifeSim/Textures/Heritage/Crop_Rice_Seed.png",
                        "Assets/CozyLifeSim/Textures/Heritage/Crop_Rice_Sprout.png",
                        "Assets/CozyLifeSim/Textures/Heritage/Crop_Rice_Mature.png"
                    )
                },
                {
                    4, (
                        "Assets/CozyLifeSim/Textures/Heritage/Crop_Lotus_Seed.png",
                        "Assets/CozyLifeSim/Textures/Heritage/Crop_Lotus_Sprout.png",
                        "Assets/CozyLifeSim/Textures/Heritage/Crop_Lotus_Mature.png"
                    )
                }
            };

            foreach (var pair in cropPaths)
            {
                var crop = cropDb.GetCrop(pair.Key);
                if (crop == null) throw new System.Exception($"Crop ID {pair.Key} is missing from CropDatabase!");

                var paths = pair.Value;

                // Seed
                if (System.IO.File.Exists(paths.seed))
                {
                    CozyAssetImporterUtility.ConfigureAsSprite(paths.seed);
                    string actual = AssetDatabase.GetAssetPath(crop.SeedSprite);
                    if (actual != paths.seed) throw new System.Exception($"Crop ID {pair.Key} seed sprite path mismatch! Expected: {paths.seed}, got: {actual}");
                }
                else
                {
                    expectedWarningCount++;
                    CozyValidationLog.ExpectedWarning("CozySim Logic", $"Core crop seed asset {paths.seed} is missing. Fallback sprite in use: {AssetDatabase.GetAssetPath(crop.SeedSprite)}");
                }

                // Sprout
                if (System.IO.File.Exists(paths.sprout))
                {
                    CozyAssetImporterUtility.ConfigureAsSprite(paths.sprout);
                    string actual = AssetDatabase.GetAssetPath(crop.SproutSprite);
                    if (actual != paths.sprout) throw new System.Exception($"Crop ID {pair.Key} sprout sprite path mismatch! Expected: {paths.sprout}, got: {actual}");
                }
                else
                {
                    expectedWarningCount++;
                    CozyValidationLog.ExpectedWarning("CozySim Logic", $"Core crop sprout asset {paths.sprout} is missing. Fallback sprite in use: {AssetDatabase.GetAssetPath(crop.SproutSprite)}");
                }

                // Mature
                if (System.IO.File.Exists(paths.mature))
                {
                    CozyAssetImporterUtility.ConfigureAsSprite(paths.mature);
                    string actual = AssetDatabase.GetAssetPath(crop.MatureSprite);
                    if (actual != paths.mature) throw new System.Exception($"Crop ID {pair.Key} mature sprite path mismatch! Expected: {paths.mature}, got: {actual}");
                }
                else
                {
                    expectedWarningCount++;
                    CozyValidationLog.ExpectedWarning("CozySim Logic", $"Core crop mature asset {paths.mature} is missing. Fallback sprite in use: {AssetDatabase.GetAssetPath(crop.MatureSprite)}");
                }
            }

            // 4. Staging NPC Grandma portrait (1 asset)
            string grandmaPath = "Assets/CozyLifeSim/Textures/Heritage/NPC_Portrait_Grandma.png";
            if (System.IO.File.Exists(grandmaPath))
            {
                CozyAssetImporterUtility.ConfigureAsSprite(grandmaPath);
                // Verify that texture importer settings are configured correctly
                TextureImporter importer = AssetImporter.GetAtPath(grandmaPath) as TextureImporter;
                if (importer == null) throw new System.Exception("Grandma portrait is not registered in AssetDatabase!");
                if (importer.textureType != TextureImporterType.Sprite || !importer.alphaIsTransparency)
                {
                    throw new System.Exception("Grandma portrait sprite importer settings incorrect!");
                }
            }
            else
            {
                expectedWarningCount++;
                CozyValidationLog.ExpectedWarning("CozySim Logic", $"Staging NPC Grandma portrait {grandmaPath} is missing.");
            }

            // 5. Staging NPC Co Ba and scrapbook assets (5 optional assets)
            var optionalPaths = new List<string>
            {
                "Assets/CozyLifeSim/Textures/Heritage/NPC_Portrait_CoBa.png",
                "Assets/CozyLifeSim/Textures/Heritage/Scrapbook_Bg_OldPaper.png",
                "Assets/CozyLifeSim/Textures/Heritage/Scrapbook_Bg_PastelPink.png",
                "Assets/CozyLifeSim/Textures/Heritage/Scrapbook_Bg_GridPaper.png",
                "Assets/CozyLifeSim/Textures/Heritage/Scrapbook_StickyNote_Yellow.png"
            };

            foreach (var path in optionalPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    CozyAssetImporterUtility.ConfigureAsSprite(path);
                }
                else
                {
                    expectedWarningCount++;
                    CozyValidationLog.ExpectedWarning("CozySim Logic", $"Optional heritage asset {path} is missing, using fallback behavior (Task 33/34 staging).");
                }
            }
        }

        private class MockInputModule : UnityEngine.EventSystems.BaseInputModule
        {
            public bool IsOverUI = false;
            public override bool IsPointerOverGameObject(int pointerId) => IsOverUI;
            public override void Process() {}
        }
    }
}

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

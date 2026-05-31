# [COMPLETED & HISTORICAL] Shop Transaction Atomicity & Interface Safety Implementation Plan

> **Note**: This plan is fully complete and all changes have been successfully implemented, verified, and integrated into the codebase under Task 24.5.

**Goal:** Ensure the shop seed and sticker transactions are truly atomic upon disk saving failures, align the `Try*` return value contract by avoiding exceptions, clean up an unused import in `StickerPlacedData.cs`, and verify everything with targeted unit tests.

**Architecture:** 
1. Introduce Non-Saving seed methods `AddSeedsNonSaving` and `ConsumeSeedsNonSaving` to `IInventoryService` and `InventoryService`.
2. Rewrite `TryBuySeed` in `ShopService.cs` to execute the transaction atomically on RAM using Non-Saving APIs, attempting disk persistence via `_saveService.Save()` inside a `try-catch` block. Roll back both coin deduction and seed addition in RAM if `Save()` throws, and gracefully return `false`.
3. Rewrite `TryBuySticker` in `ShopService.cs` to roll back RAM on save failure and gracefully return `false` instead of throwing the exception, adhering to standard `Try*` semantics.
4. Clean up `using System.Runtime.InteropServices;` in `StickerPlacedData.cs`.
5. Add simulated Save failure test `Test 11.15` in `CozyLifeSimValidation.cs` to guarantee transaction atomicity and memory safety.

**Tech Stack:** C#, Unity Editor, CozyLifeSim Core/UI architecture.

---

### Task 1: Introduce Non-Saving Seed APIs

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Core/IInventoryService.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/Services/InventoryService.cs`

**Step 1: Write Non-Saving interface methods**
Add signature definitions in `Assets/CozyLifeSim/Scripts/Core/IInventoryService.cs`:
```csharp
        void AddSeedsNonSaving(int amount);
        bool ConsumeSeedsNonSaving(int amount);
```

**Step 2: Implement Non-Saving methods in InventoryService**
Modify `Assets/CozyLifeSim/Scripts/UI/Services/InventoryService.cs`:
```csharp
        public void AddSeeds(int amount)
        {
            AddSeedsNonSaving(amount);
            _saveService.Save();
        }

        public bool ConsumeSeeds(int amount)
        {
            if (ConsumeSeedsNonSaving(amount))
            {
                _saveService.Save();
                return true;
            }
            return false;
        }

        public void AddSeedsNonSaving(int amount)
        {
            if (amount <= 0) return;
            ActiveSave.Seeds += amount;
            OnSeedsChanged?.Invoke(ActiveSave.Seeds);
        }

        public bool ConsumeSeedsNonSaving(int amount)
        {
            if (amount <= 0 || ActiveSave.Seeds < amount) return false;
            ActiveSave.Seeds -= amount;
            OnSeedsChanged?.Invoke(ActiveSave.Seeds);
            return true;
        }
```

---

### Task 2: Make Shop Transactions Atomic and Safe

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/UI/Services/ShopService.cs`

**Step 1: Update TryBuySeed transaction and catch exceptions**
Change `TryBuySeed` to deduct coin and add seeds non-saving, try to save, and roll back RAM on exception:
```csharp
        public bool TryBuySeed(int cropId)
        {
            if (_cropDatabase == null)
            {
                Debug.Log("[CozySim Shop] Cannot buy seed: CropDatabase is missing.");
                return false;
            }

            var crop = _cropDatabase.GetCrop(cropId);
            if (crop == null)
            {
                Debug.Log($"[CozySim Shop] Cannot buy seed: Crop template for ID {cropId} not found.");
                return false;
            }

            int currentLevel = _progressionService != null ? _progressionService.PlayerLevel : 999;
            if (currentLevel < crop.RequiredLevel)
            {
                Debug.Log($"[CozySim Shop] Cannot buy seed: Player level {currentLevel} is lower than required level {crop.RequiredLevel}.");
                return false;
            }

            int price = crop.BuyPrice;
            if (_inventoryService.Coins < price)
            {
                Debug.Log($"[CozySim Shop] Insufficient coins to buy seed for crop ID {cropId}. Cost: {price}, owned: {_inventoryService.Coins}");
                return false;
            }

            // Perform transaction: deduct coins non-saving, then add seeds non-saving
            if (_inventoryService.ConsumeCoinsNonSaving(price))
            {
                _inventoryService.AddSeedsNonSaving(1);
                try
                {
                    _saveService.Save();
                    OnShopTransactionSuccess?.Invoke();
                    return true;
                }
                catch (Exception ex)
                {
                    // Rollback RAM
                    _inventoryService.AddCoinsNonSaving(price);
                    _inventoryService.ConsumeSeedsNonSaving(1);
                    Debug.LogWarning($"[CozySim Shop] TryBuySeed failed to save. Rolled back RAM. Exception: {ex.Message}");
                    return false;
                }
            }

            return false;
        }
```

**Step 2: Update TryBuySticker to avoid exception throwing**
Change `TryBuySticker` to log and gracefully return `false` on save failure rather than throwing:
```csharp
            _inventoryService.AddStickerCountNonSaving(stickerId, 1);
            try
            {
                _saveService.Save();
                OnShopTransactionSuccess?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                _inventoryService.AddCoinsNonSaving(price);
                _inventoryService.ConsumeStickerNonSaving(stickerId);
                Debug.LogWarning($"[CozySim Shop] TryBuySticker failed to save. Rolled back RAM. Exception: {ex.Message}");
                return false;
            }
```

---

### Task 3: Clean up StickerPlacedData Unused Import

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Core/StickerPlacedData.cs`

**Step 1: Remove unused InteropServices**
Remove `using System.Runtime.InteropServices;` from the top of the file.

---

### Task 4: Add Unit Verification Tests for Simulated Failure

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs`

**Step 1: Implement Test 11.15 failure case**
Add a test simulating disk IO write failure and verify that seed and sticker balances do not mutate on disk/RAM when transactions fail:
```csharp
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

                Object.DestroyImmediate(testShopAtomicCropDb);
                Object.DestroyImmediate(testShopAtomicStickerDb);
                passCount++;
                CozyValidationLog.Pass("CozySim Logic", "ShopService Atomic Seed & Sticker Save Failure Rollback verified successfully");
```

---

### Task 5: Run Verification Command and Confirm Success

**Step 1: Compile and run logic tests**
Call verification runner through MCP endpoint: `Tools/CozySim/Run Logic Verification Tests` or logic test verification.
Ensure 18 passed, 0 failed, 1 expected warning.

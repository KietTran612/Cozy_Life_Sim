using System;
using System.Collections.Generic;
using System.Reflection;
using CozyLifeSim.Core;
using CozyLifeSim.UI;
using CozyLifeSim.UI.Services;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace CozyLifeSim.Editor
{
    public static class CozyLifeSimMcpGameplayLoopValidation
    {
        private const float StepTimeoutSeconds = 8f;

        private static readonly List<string> Passes = new List<string>();
        private static readonly List<string> Errors = new List<string>();

        private static FarmRefs _farm;
        private static AnimalRefs _animal;
        private static StickerRefs _stickers;
        private static ISaveService _saveService;
        private static IInventoryService _inventoryService;
        private static IMemoryService _memoryService;
        private static IQuestService _questService;
        private static InventoryHudWidget _inventoryHud;
        private static QuestHudWidget _questHud;
        private static SaveData _saveBackup;
        private static int _stepIndex;
        private static int _waterCount;
        private static float _deadline;
        private static bool _isRunning;
        private static bool _restoreSaveOnFinish;
        private static int _initialCoins;
        private static int _initialSeeds;
        private static int _initialCrops;
        private static int _initialStickerCount;
        private static Transform _originalStickerParent;
        private static Vector2 _originalStickerAnchoredPosition;
        private static int _waterQuestReward;

        private static readonly Step[] Steps =
        {
            new Step("Initialize runtime references", InitializeRuntimeReferences),
            new Step("Plant seed", PlantSeed),
            new Step("Water crop stage 0", WaterCrop),
            new Step("Wait for crop stage 1", WaitForCropStage1),
            new Step("Water crop stage 1", WaterCrop),
            new Step("Wait for crop stage 2", WaitForCropStage2),
            new Step("Water crop stage 2", WaterCrop),
            new Step("Wait for harvest-ready crop", WaitForCropStage3),
            new Step("Harvest crop", HarvestCrop),
            new Step("Pet chicken", PetChicken),
            new Step("Place sticker", PlaceSticker),
            new Step("Verify PlayerPrefs persistence with fresh services", VerifyFreshServicePersistence),
            new Step("Restore test save", RestoreTestSave)
        };

        [MenuItem("Tools/CozySim/Run MCP Gameplay Loop Validation")]
        public static void RunGameplayLoopValidation()
        {
            if (!Application.isPlaying)
            {
                CozyValidationLog.Fail("CozySim RuntimeLoop", "Enter Play Mode before running this validation. MCP flow: sim_play, then editor_invoke_method.");
                return;
            }

            if (_isRunning)
            {
                CozyValidationLog.Warn("CozySim RuntimeLoop", "Validation is already running.");
                return;
            }

            Passes.Clear();
            Errors.Clear();
            _stepIndex = 0;
            _waterCount = 0;
            _isRunning = true;
            _restoreSaveOnFinish = false;
            _deadline = Time.realtimeSinceStartup + StepTimeoutSeconds;

            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            CozyValidationLog.Pass("CozySim RuntimeLoop", "Started runtime validation.");
        }

        [MenuItem("Tools/CozySim/Check Loop Validation Status")]
        public static void CheckLoopValidationStatus()
        {
            Debug.Log($"[CozySim Status] isRunning={_isRunning}, stepIndex={_stepIndex}, passes={Passes.Count}, errors={Errors.Count}, deadline={_deadline - Time.realtimeSinceStartup}s remaining");
        }

        private static void Tick()
        {
            if (!Application.isPlaying)
            {
                Fail("Play Mode stopped before validation completed.");
                Finish();
                return;
            }

            if (_stepIndex >= Steps.Length)
            {
                Finish();
                return;
            }

            Step step = Steps[_stepIndex];
            bool completed;
            try
            {
                completed = step.Execute();
            }
            catch (Exception ex)
            {
                Fail($"{step.Name}: threw {ex.GetType().Name}: {ex.Message}");
                Finish();
                return;
            }

            if (completed)
            {
                if (HasErrors())
                {
                    Finish();
                    return;
                }

                Pass(step.Name);
                _stepIndex++;
                _deadline = Time.realtimeSinceStartup + StepTimeoutSeconds;
                return;
            }

            if (Time.realtimeSinceStartup > _deadline)
            {
                Fail($"{step.Name}: timed out after {StepTimeoutSeconds:0.#}s.");
                Finish();
            }
        }

        private static bool InitializeRuntimeReferences()
        {
            var scope = LifetimeScope.Find<GameLifetimeScope>();
            if (scope == null || scope.Container == null)
            {
                Fail("GameLifetimeScope container is not available.");
                return true;
            }

            _saveService = scope.Container.Resolve<ISaveService>();
            _inventoryService = scope.Container.Resolve<IInventoryService>();
            _memoryService = scope.Container.Resolve<IMemoryService>();
            _questService = scope.Container.Resolve<IQuestService>();

            if (_saveService == null || _inventoryService == null || _memoryService == null || _questService == null)
            {
                Fail("Required services could not be resolved from VContainer.");
                return true;
            }

            _inventoryHud = FindSceneComponent<InventoryHudWidget>("Header_Panel");
            _questHud = FindSceneComponent<QuestHudWidget>("Quest_Content");

            _saveBackup = CloneSave(_saveService.ActiveSave);
            _restoreSaveOnFinish = true;

            SaveData activeSave = _saveService.ActiveSave;
            activeSave.Coins = 100;
            activeSave.Seeds = 5;
            activeSave.Crops = 0;
            activeSave.PlacedStickers.Clear();
            activeSave.CompletedQuestIds.Clear();
            activeSave.ActiveQuestProgress.Clear();
            _saveService.Save();

            // Re-initialize the in-memory services to reflect the cleared save data and rehydrate events/UI
            if (_questService != null)
            {
                _questService.ReloadFromSave(false);
            }
            if (_inventoryService != null)
            {
                _inventoryService.ReloadFromSave();
            }



            _waterQuestReward = GetWaterQuestReward(_questService);

            _initialCoins = _inventoryService.Coins;
            _initialSeeds = _inventoryService.Seeds;
            _initialCrops = _inventoryService.Crops;
            _initialStickerCount = _memoryService.PlacedStickers.Count;

            CropWidget crop = FindSceneComponent<CropWidget>("Farm_Plot");
            AnimalWidget animal = FindSceneComponent<AnimalWidget>("Animal_Pen");
            StickerBook stickerBook = FindSceneComponent<StickerBook>("StickerBook_Panel");

            if (animal != null)
            {
                FieldInfo isPettingField = typeof(AnimalWidget).GetField("_isPetting", BindingFlags.Instance | BindingFlags.NonPublic);
                if (isPettingField != null)
                {
                    isPettingField.SetValue(animal, false);
                }
                
                FieldInfo petSeqField = typeof(AnimalWidget).GetField("_petSequence", BindingFlags.Instance | BindingFlags.NonPublic);
                if (petSeqField != null)
                {
                    var seq = petSeqField.GetValue(animal) as Sequence;
                    seq?.Kill();
                }

                FieldInfo breathTweenField = typeof(AnimalWidget).GetField("_breathTween", BindingFlags.Instance | BindingFlags.NonPublic);
                if (breathTweenField != null)
                {
                    var tween = breathTweenField.GetValue(animal) as Tween;
                    tween?.Play();
                }
            }
            if (crop == null || animal == null || stickerBook == null)
            {
                Fail("One or more gameplay widgets are missing from the scene.");
                return true;
            }

            SerializedObject cropObject = new SerializedObject(crop);
            cropObject.FindProperty("_stageDurationSeconds").floatValue = 1f;
            cropObject.ApplyModifiedPropertiesWithoutUndo();

            _farm = new FarmRefs(
                crop,
                GetReference<Button>(cropObject, "_plantButton"),
                GetReference<Button>(cropObject, "_waterButton"),
                GetReference<Button>(cropObject, "_harvestButton"));

            SerializedObject animalObject = new SerializedObject(animal);
            _animal = new AnimalRefs(
                animal,
                GetReference<Button>(animalObject, "_interactionButton"),
                GetReference<Transform>(animalObject, "_spawnRoot"));

            _stickers = new StickerRefs(
                stickerBook,
                FindSceneComponent<StickerBookPage>("Page_0"),
                FindInventorySticker());

            if (_stickers.Sticker != null)
            {
                _originalStickerParent = _stickers.Sticker.transform.parent;
                var rect = _stickers.Sticker.GetComponent<RectTransform>();
                if (rect != null)
                {
                    _originalStickerAnchoredPosition = rect.anchoredPosition;
                }
            }

            if (!_farm.IsValid || !_animal.IsValid || !_stickers.IsValid)
            {
                Fail($"Required references missing: farm={_farm.IsValid} (widget={_farm.Widget!=null}, plant={_farm.PlantButton!=null}, water={_farm.WaterButton!=null}, harvest={_farm.HarvestButton!=null}), " +
                     $"animal={_animal.IsValid} (widget={_animal.Widget!=null}, button={_animal.Button!=null}, spawn={_animal.SpawnRoot!=null}), " +
                     $"stickers={_stickers.IsValid} (book={_stickers.Book!=null}, page={_stickers.Page!=null}, sticker={_stickers.Sticker!=null})");
            }

            CozyValidationLog.ExpectedWarning("CozySim RuntimeLoop", "Crop growth is accelerated through validation hooks to keep MCP runtime tests deterministic.");

            return true;
        }

        private static bool PlantSeed()
        {
            if (HasErrors()) return true;

            _farm.PlantButton.onClick.Invoke();
            if (_inventoryService.Seeds != _initialSeeds - 1)
            {
                Fail($"Plant seed did not consume one seed. Expected {_initialSeeds - 1}, got {_inventoryService.Seeds}.");
            }

            return true;
        }

        private static bool WaterCrop()
        {
            if (HasErrors()) return true;
            if (!_farm.WaterButton.interactable)
            {
                return false;
            }

            _farm.WaterButton.onClick.Invoke();
            CropState state = GetCropState();
            if (!state.IsWatered)
            {
                InvokeCropMethod("CompleteWatering");
            }

            _waterCount++;
            if (_waterCount == 3)
            {
                QuestData waterQuest = FindQuest(QuestType.WaterCrops);
                if (waterQuest == null || !waterQuest.IsCompleted || waterQuest.CurrentCount != waterQuest.TargetCount)
                {
                    Fail("Water quest was not completed after 3 watering actions.");
                }
            }

            return true;
        }

        private static bool WaitForCropStage1()
        {
            return WaitForCropStage(1, false);
        }

        private static bool WaitForCropStage2()
        {
            return WaitForCropStage(2, false);
        }

        private static bool WaitForCropStage3()
        {
            return _farm.HarvestButton != null && WaitForCropStage(3, false) && _farm.HarvestButton.interactable;
        }

        private static bool HarvestCrop()
        {
            if (HasErrors()) return true;

            _farm.HarvestButton.onClick.Invoke();
            if (_inventoryService.Crops != _initialCrops + 1)
            {
                Fail($"Harvest did not add one crop. Expected {_initialCrops + 1}, got {_inventoryService.Crops}.");
            }

            int expectedCoins = _initialCoins + 10 + _waterQuestReward;
            if (_inventoryService.Coins != expectedCoins)
            {
                Fail($"Harvest did not add expected coins. Expected {expectedCoins}, got {_inventoryService.Coins}.");
            }

            QuestData harvestQuest = FindQuest(QuestType.HarvestCrops);
            if (harvestQuest == null || harvestQuest.CurrentCount != 1 || harvestQuest.IsCompleted)
            {
                Fail("Harvest quest should persist partial progress 1/2 after one harvest.");
            }

            return true;
        }

        private static bool PetChicken()
        {
            if (HasErrors()) return true;

            int beforeHeartCount = _animal.SpawnRoot.childCount;
            _animal.Button.onClick.Invoke();

            int expectedCoins = _initialCoins + 10 + 5 + _waterQuestReward;
            if (_inventoryService.Coins != expectedCoins)
            {
                Fail($"Pet chicken did not result in expected coins. Expected {expectedCoins}, got {_inventoryService.Coins}.");
            }

            if (_animal.SpawnRoot.childCount <= beforeHeartCount)
            {
                Fail("Pet chicken did not spawn a heart feedback instance.");
            }

            QuestData petQuest = FindQuest(QuestType.PetAnimal);
            if (petQuest == null || petQuest.CurrentCount != 1 || petQuest.IsCompleted)
            {
                Fail("Pet quest should persist partial progress 1/5 after one pet.");
            }

            return true;
        }

        private static bool PlaceSticker()
        {
            if (HasErrors()) return true;

            _stickers.Sticker.FinalizePlacement(_stickers.Page.transform, new Vector2(24f, -18f), _stickers.Page.PageIndex);

            if (_memoryService.PlacedStickers.Count != _initialStickerCount + 1)
            {
                Fail($"Sticker placement was not saved. Expected {_initialStickerCount + 1}, got {_memoryService.PlacedStickers.Count}.");
            }

            StickerPlacedData sticker = _memoryService.PlacedStickers[0];
            if (sticker.PageIndex != 0 || Mathf.Abs(sticker.PositionX - 24f) > 0.01f || Mathf.Abs(sticker.PositionY - (-18f)) > 0.01f)
            {
                Fail("Sticker placement data did not persist the expected page position.");
            }

            if (Mathf.Abs(sticker.Scale - 1.0f) > 0.001f)
            {
                Fail($"Sticker scale should persist as 1.0, got {sticker.Scale:0.###}.");
            }

            return true;
        }

        private static bool VerifyFreshServicePersistence()
        {
            if (HasErrors()) return true;

            var freshSave = new CozyLifeSim.UI.Services.SaveService();
            var freshInventory = new CozyLifeSim.UI.Services.InventoryService(freshSave);
            var freshMemory = new CozyLifeSim.UI.Services.MemoryService(freshSave);
            
            var questDatabase = GetQuestDatabase();
            var freshQuest = new CozyLifeSim.UI.Services.QuestService(freshSave, freshInventory, questDatabase, false);

            if (freshInventory.Seeds != 4)
            {
                Fail($"Fresh save should restore 4 seeds, got {freshInventory.Seeds}.");
            }

            if (freshInventory.Crops != 1)
            {
                Fail($"Fresh save should restore 1 crop, got {freshInventory.Crops}.");
            }

            int expectedFreshCoins = 100 + 10 + 5 + GetWaterQuestReward(freshQuest);
            if (freshInventory.Coins != expectedFreshCoins)
            {
                Fail($"Fresh save should restore {expectedFreshCoins} coins, got {freshInventory.Coins}.");
            }

            if (freshMemory.PlacedStickers.Count != 1)
            {
                Fail($"Fresh save should restore 1 placed sticker, got {freshMemory.PlacedStickers.Count}.");
            }

            QuestData waterQuest = FindQuest(freshQuest, QuestType.WaterCrops);
            QuestData harvestQuest = FindQuest(freshQuest, QuestType.HarvestCrops);
            QuestData petQuest = FindQuest(freshQuest, QuestType.PetAnimal);

            if (waterQuest == null || !waterQuest.IsCompleted)
            {
                Fail("Fresh quest service should restore completed Water quest.");
            }

            if (harvestQuest == null || harvestQuest.CurrentCount != 1 || harvestQuest.IsCompleted)
            {
                Fail("Fresh quest service should restore Harvest quest partial progress 1/2.");
            }

            if (petQuest == null || petQuest.CurrentCount != 1 || petQuest.IsCompleted)
            {
                Fail("Fresh quest service should restore Pet quest partial progress 1/5.");
            }

            return true;
        }

        private static bool RestoreTestSave()
        {
            RestoreSaveBackup();
            return true;
        }

        private static bool WaitForCropStage(int expectedStage, bool expectedWatered)
        {
            if (HasErrors()) return true;

            CropState state = GetCropState();
            if (state.GrowthStage == expectedStage - 1 && state.IsWatered)
            {
                InvokeCropMethod("AdvanceStage");
                InvokeCropMethod("UpdateVisuals");
                state = GetCropState();
            }

            return state.GrowthStage == expectedStage && state.IsWatered == expectedWatered;
        }

        private static CropState GetCropState()
        {
            FieldInfo field = typeof(CropWidget).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || _farm.Widget == null)
            {
                return default;
            }

            return (CropState)field.GetValue(_farm.Widget);
        }

        private static void SetCropState(CropState state)
        {
            FieldInfo field = typeof(CropWidget).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null && _farm.Widget != null)
            {
                field.SetValue(_farm.Widget, state);
            }
        }

        private static void InvokeCropMethod(string methodName)
        {
            MethodInfo method = typeof(CropWidget).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null || _farm.Widget == null)
            {
                Fail($"CropWidget private method '{methodName}' was not found.");
                return;
            }

            method.Invoke(_farm.Widget, null);
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;

            if (_restoreSaveOnFinish)
            {
                RestoreSaveBackup();
            }

            _isRunning = false;

            int passedCount = Passes.Count;
            int failedCount = Errors.Count;

            foreach (string pass in Passes)
            {
                CozyValidationLog.Pass("CozySim RuntimeLoop", pass);
            }

            foreach (string error in Errors)
            {
                CozyValidationLog.Fail("CozySim RuntimeLoop", error);
            }

            CozyValidationLog.Summary("CozySim RuntimeLoop", passedCount, failedCount);
        }

        private static void RestoreSaveBackup()
        {
            if (_saveService == null || _saveBackup == null)
            {
                return;
            }

            CopySave(_saveBackup, _saveService.ActiveSave);
            _saveService.Save();

            // Rehydrate quest and inventory services to trigger HUD widget updates
            if (_questService != null)
            {
                _questService.ReloadFromSave(false);
            }
            if (_inventoryService != null)
            {
                _inventoryService.ReloadFromSave();
            }

            // Restore sticker visual layout to inventory tray
            if (_stickers.Sticker != null && _originalStickerParent != null)
            {
                _stickers.Sticker.ResetToTray(_originalStickerParent, _originalStickerAnchoredPosition);
            }

            _restoreSaveOnFinish = false;
        }

        private static void Pass(string message)
        {
            Passes.Add(message);
        }

        private static void Fail(string message)
        {
            if (!Errors.Contains(message))
            {
                Errors.Add(message);
            }
        }

        private static bool HasErrors()
        {
            return Errors.Count > 0;
        }

        private static SaveData CloneSave(SaveData source)
        {
            SaveData clone = new SaveData();
            CopySave(source, clone);
            return clone;
        }

        private static void CopySave(SaveData source, SaveData destination)
        {
            destination.Coins = source.Coins;
            destination.Seeds = source.Seeds;
            destination.Crops = source.Crops;

            destination.PlacedStickers.Clear();
            foreach (StickerPlacedData sticker in source.PlacedStickers)
            {
                destination.PlacedStickers.Add(sticker);
            }

            destination.CompletedQuestIds.Clear();
            foreach (int questId in source.CompletedQuestIds)
            {
                destination.CompletedQuestIds.Add(questId);
            }

            destination.ActiveQuestProgress.Clear();
            foreach (QuestProgressData quest in source.ActiveQuestProgress)
            {
                destination.ActiveQuestProgress.Add(quest);
            }
        }

        private static CozySticker FindInventorySticker()
        {
            foreach (CozySticker sticker in FindSceneObjects<CozySticker>())
            {
                if (sticker.gameObject.activeInHierarchy && sticker.GetComponentInParent<StickerBookPage>() == null)
                {
                    return sticker;
                }
            }

            return null;
        }

        private static T GetReference<T>(SerializedObject so, string propertyName) where T : UnityEngine.Object
        {
            SerializedProperty property = so.FindProperty(propertyName);
            return property == null ? null : property.objectReferenceValue as T;
        }

        private static T FindSceneComponent<T>(string objectName) where T : Component
        {
            foreach (T component in FindSceneObjects<T>())
            {
                if (component.gameObject.name == objectName)
                {
                    return component;
                }
            }

            return null;
        }

        private static IEnumerable<T> FindSceneObjects<T>() where T : UnityEngine.Object
        {
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
            {
                if (item == null || EditorUtility.IsPersistent(item))
                {
                    continue;
                }

                GameObject go = item as GameObject;
                if (go == null && item is Component component)
                {
                    go = component.gameObject;
                }

                if (go == null || !go.scene.IsValid() || !go.scene.isLoaded)
                {
                    continue;
                }

                yield return item;
            }
        }

        private static CozyLifeSim.UI.Settings.QuestDatabase GetQuestDatabase()
        {
            GameLifetimeScope scope = LifetimeScope.Find<GameLifetimeScope>() as GameLifetimeScope;
            if (scope == null) return null;

            SerializedObject so = new SerializedObject(scope);
            SerializedProperty property = so.FindProperty("_questDatabase");
            return property == null ? null : property.objectReferenceValue as CozyLifeSim.UI.Settings.QuestDatabase;
        }

        private static int GetWaterQuestReward(IQuestService questService)
        {
            QuestData waterQuest = FindQuest(questService, QuestType.WaterCrops);
            return waterQuest != null ? waterQuest.RewardCoins : 50;
        }

        private static QuestData FindQuest(QuestType type)
        {
            return FindQuest(_questService, type);
        }

        private static QuestData FindQuest(IQuestService service, QuestType type)
        {
            if (service == null) return null;
            foreach (QuestData quest in service.ActiveQuests)
            {
                if (quest.Type == type)
                {
                    return quest;
                }
            }
            return null;
        }

        private readonly struct Step
        {
            public Step(string name, Func<bool> execute)
            {
                Name = name;
                Execute = execute;
            }

            public string Name { get; }
            public Func<bool> Execute { get; }
        }

        private readonly struct FarmRefs
        {
            public FarmRefs(CropWidget widget, Button plantButton, Button waterButton, Button harvestButton)
            {
                Widget = widget;
                PlantButton = plantButton;
                WaterButton = waterButton;
                HarvestButton = harvestButton;
            }

            public CropWidget Widget { get; }
            public Button PlantButton { get; }
            public Button WaterButton { get; }
            public Button HarvestButton { get; }
            public bool IsValid => Widget != null && PlantButton != null && WaterButton != null && HarvestButton != null;
        }

        private readonly struct AnimalRefs
        {
            public AnimalRefs(AnimalWidget widget, Button button, Transform spawnRoot)
            {
                Widget = widget;
                Button = button;
                SpawnRoot = spawnRoot;
            }

            public AnimalWidget Widget { get; }
            public Button Button { get; }
            public Transform SpawnRoot { get; }
            public bool IsValid => Widget != null && Button != null && SpawnRoot != null;
        }

        private readonly struct StickerRefs
        {
            public StickerRefs(StickerBook book, StickerBookPage page, CozySticker sticker)
            {
                Book = book;
                Page = page;
                Sticker = sticker;
            }

            public StickerBook Book { get; }
            public StickerBookPage Page { get; }
            public CozySticker Sticker { get; }
            public bool IsValid => Book != null && Page != null && Sticker != null;
        }
    }
}

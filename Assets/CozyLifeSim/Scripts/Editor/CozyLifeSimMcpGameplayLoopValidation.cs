using System;
using System.Collections.Generic;
using System.Reflection;
using CozyLifeSim.Core;
using CozyLifeSim.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace CozyLifeSim.Editor
{
    public static class CozyLifeSimMcpGameplayLoopValidation
    {
        private const string LogPrefix = "[CozySim MCP Gameplay Validation]";
        private const float StepTimeoutSeconds = 8f;

        private static readonly List<string> Passes = new List<string>();
        private static readonly List<string> Errors = new List<string>();

        private static FarmRefs _farm;
        private static AnimalRefs _animal;
        private static StickerRefs _stickers;
        private static ISaveService _saveService;
        private static IInventoryService _inventoryService;
        private static IMemoryService _memoryService;
        private static SaveData _saveBackup;
        private static int _stepIndex;
        private static float _deadline;
        private static bool _isRunning;
        private static bool _restoreSaveOnFinish;
        private static int _initialCoins;
        private static int _initialSeeds;
        private static int _initialCrops;
        private static int _initialStickerCount;

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
            new Step("Restore test save", RestoreTestSave)
        };

        [MenuItem("Tools/CozySim/Run MCP Gameplay Loop Validation")]
        public static void RunGameplayLoopValidation()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError($"{LogPrefix} Enter Play Mode before running this validation. MCP flow: sim_play, then editor_invoke_method.");
                return;
            }

            if (_isRunning)
            {
                Debug.LogWarning($"{LogPrefix} Validation is already running.");
                return;
            }

            Passes.Clear();
            Errors.Clear();
            _stepIndex = 0;
            _isRunning = true;
            _restoreSaveOnFinish = false;

            Debug.Log($"{LogPrefix} Started.");

            for (_stepIndex = 0; _stepIndex < Steps.Length; _stepIndex++)
            {
                Step step = Steps[_stepIndex];
                bool completed;
                try
                {
                    completed = step.Execute();
                }
                catch (Exception ex)
                {
                    Fail($"{step.Name}: threw {ex.GetType().Name}: {ex.Message}");
                    break;
                }

                if (!completed)
                {
                    Fail($"{step.Name}: did not complete in synchronous MCP validation.");
                    break;
                }

                if (HasErrors())
                {
                    break;
                }

                Pass(step.Name);
            }

            Finish();
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

            if (_saveService == null || _inventoryService == null || _memoryService == null)
            {
                Fail("Required services could not be resolved from VContainer.");
                return true;
            }

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

            _initialCoins = _inventoryService.Coins;
            _initialSeeds = _inventoryService.Seeds;
            _initialCrops = _inventoryService.Crops;
            _initialStickerCount = _memoryService.PlacedStickers.Count;

            CropWidget crop = FindSceneComponent<CropWidget>("Farm_Plot");
            AnimalWidget animal = FindSceneComponent<AnimalWidget>("Animal_Pen");
            StickerBook stickerBook = FindSceneComponent<StickerBook>("StickerBook_Panel");
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

            if (!_farm.IsValid || !_animal.IsValid || !_stickers.IsValid)
            {
                Fail("Required button, page, sticker, or spawn references are missing.");
            }

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

            int expectedCoins = _initialCoins + 10;
            if (_inventoryService.Coins != expectedCoins)
            {
                Fail($"Harvest did not add 10 coins. Expected {expectedCoins}, got {_inventoryService.Coins}.");
            }

            return true;
        }

        private static bool PetChicken()
        {
            if (HasErrors()) return true;

            int beforeHeartCount = _animal.SpawnRoot.childCount;
            _animal.Button.onClick.Invoke();

            int expectedCoins = _initialCoins + 15;
            if (_inventoryService.Coins != expectedCoins)
            {
                Fail($"Pet chicken did not add 5 coins after harvest. Expected {expectedCoins}, got {_inventoryService.Coins}.");
            }

            if (_animal.SpawnRoot.childCount <= beforeHeartCount)
            {
                Fail("Pet chicken did not spawn a heart feedback instance.");
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
            if (_restoreSaveOnFinish)
            {
                RestoreSaveBackup();
            }

            _isRunning = false;

            foreach (string pass in Passes)
            {
                Debug.Log($"<color=green>{LogPrefix} PASS</color> {pass}");
            }

            foreach (string error in Errors)
            {
                Debug.LogError($"<color=red>{LogPrefix} FAIL</color> {error}");
            }

            string summary = $"{Passes.Count} passed, {Errors.Count} errors.";
            if (Errors.Count > 0)
            {
                Debug.LogError($"<color=red>{LogPrefix}</color> {summary}");
            }
            else
            {
                Debug.Log($"<color=cyan>{LogPrefix}</color> {summary}");
            }
        }

        private static void RestoreSaveBackup()
        {
            if (_saveService == null || _saveBackup == null)
            {
                return;
            }

            CopySave(_saveBackup, _saveService.ActiveSave);
            _saveService.Save();
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
                if (sticker.GetComponentInParent<StickerBookPage>() == null)
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

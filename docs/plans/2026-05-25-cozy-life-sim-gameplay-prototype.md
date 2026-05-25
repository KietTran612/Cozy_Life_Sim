# Cozy Life Sim Gameplay Prototype Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Implement the high-performance core prototype gameplay mechanics for "Xóm Nhỏ Tuổi Thơ" including tactile sticker placement, a 2D pseudo-3D notebook page-flip, a UniTask crop growth loop, and breathing chicken feedback.

**Architecture:** Model pure state values as lightweight serialized pack-aligned structs inside the Core Assembly. Drive presentation feedback entirely through UI classes inside the UI Assembly utilizing DI-registered dependencies, clean event hooks, UniTask loops for zero-recurring-GC ticking, and highly polished DOTween animation sequences.

**Tech Stack:** Unity 2D (C#), VContainer (DI), UniTask (Async), UGUI, DOTween.

---

### Task 0: Value types and DTO structures in the Core Assembly

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/Core/StickerPlacedData.cs`
- Create: `Assets/CozyLifeSim/Scripts/Core/CropState.cs`

**Step 1: Write `StickerPlacedData` struct**
Create pure data representation of a placed sticker. Use pack-aligned sequential struct layout to ensure minimum footprint and cache friendliness.
Create `Assets/CozyLifeSim/Scripts/Core/StickerPlacedData.cs`:
```csharp
using System.Runtime.InteropServices;

namespace CozyLifeSim.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StickerPlacedData
    {
        public int StickerId;
        public int PageIndex;
        public float PositionX;
        public float PositionY;
        public float Scale;
        public float Rotation;

        public StickerPlacedData(int stickerId, int pageIndex, float positionX, float positionY, float scale, float rotation)
        {
            StickerId = stickerId;
            PageIndex = pageIndex;
            PositionX = positionX;
            PositionY = positionY;
            Scale = scale;
            Rotation = rotation;
        }
    }
}
```

**Step 2: Write `CropState` struct**
Create pure data representation of a crop state.
Create `Assets/CozyLifeSim/Scripts/Core/CropState.cs`:
```csharp
using System.Runtime.InteropServices;

namespace CozyLifeSim.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CropState
    {
        public int CropId;
        public int GrowthStage; // 0: Seed, 1: Sprout, 2: Mature, 3: Harvestable
        public float TimeRemainingSeconds;
        public bool IsWatered;

        public CropState(int cropId, int growthStage, float timeRemainingSeconds, bool isWatered)
        {
            CropId = cropId;
            GrowthStage = growthStage;
            TimeRemainingSeconds = timeRemainingSeconds;
            IsWatered = isWatered;
        }
    }
}
```

**Step 3: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/Core/StickerPlacedData.cs Assets/CozyLifeSim/Scripts/Core/CropState.cs
git commit -m "feat: implement pack-aligned Core data structs for sticker and crop states"
```

---

### Task 1: Tactile UGUI Sticker Drag & Drop with DOTween Feedback

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/UI/CozyLifeSim.UI.asmdef`
- Create: `Assets/CozyLifeSim/Scripts/UI/CozySticker.cs`
- Create: `Assets/CozyLifeSim/Scripts/UI/StickerBookPage.cs`

**Step 1: Reference DOTween.Modules in UI Assembly Definition**
Now that we are implementing gameplay scripts using DOTween, add `"DOTween.Modules"` back to the references list inside `Assets/CozyLifeSim/Scripts/UI/CozyLifeSim.UI.asmdef` so that all DOTween extensions are available:
```json
    "references": [
        "CozyLifeSim.Core",
        "VContainer",
        "UniTask",
        "Unity.TextMeshPro",
        "DOTween.Modules"
    ],
```

**Step 2: Write `CozySticker` component**
Implement drag-and-drop interaction utilizing UGUI `IBeginDragHandler`, `IDragHandler`, and `IEndDragHandler` interfaces. Use DOTween for high-juice physical feedback (upscaling and shadow offset on lift, smooth return on invalid release).
Create `Assets/CozyLifeSim/Scripts/UI/CozySticker.cs`:
```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace CozyLifeSim.UI
{
    public class CozySticker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private int _stickerId;
        [SerializeField] private RectTransform _shadowOffset;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Vector3 _startPosition;
        private Transform _originalParent;
        private Canvas _canvas;
        private RectTransform _rectTransform;
        private Tween _scaleTween;
        private Tween _shadowTween;
        private bool _isDragging;

        public int StickerId => _stickerId;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _startPosition = _rectTransform.anchoredPosition;
            _originalParent = transform.parent;
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_canvas == null) return;
            _isDragging = true;

            _scaleTween?.Kill();
            _shadowTween?.Kill();

            // Upscale sticker on lift
            _scaleTween = transform.DOScale(1.1f, 0.15f).SetEase(Ease.OutBack);

            // Translate shadow downwards to mock physical depth/lift
            if (_shadowOffset != null)
            {
                _shadowTween = _shadowOffset.DOAnchorPos(new Vector2(-10f, -15f), 0.15f).SetEase(Ease.OutQuad);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.alpha = 0.8f;
            }

            // Move to top hierarchy of canvas to prevent clipping
            transform.SetParent(_canvas.transform, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _canvas == null) return;
            
            // Move using canvas scale factor
            _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            _scaleTween?.Kill();
            _shadowTween?.Kill();

            // Check if dropped over a valid drop target (e.g. StickerBookPage)
            var droppedOn = eventData.pointerEnter;
            bool isValidDrop = false;

            if (droppedOn != null)
            {
                var page = droppedOn.GetComponentInParent<StickerBookPage>();
                if (page != null)
                {
                    isValidDrop = page.TryPlaceSticker(this, eventData.position, eventData.pressEventCamera);
                }
            }

            if (!isValidDrop)
            {
                // Smoothly snap back to inventory tray if dropped invalidly
                if (_canvasGroup != null)
                {
                    _canvasGroup.blocksRaycasts = true;
                    _canvasGroup.alpha = 1.0f;
                }

                transform.SetParent(_originalParent, true);
                _rectTransform.DOAnchorPos(_startPosition, 0.3f).SetEase(Ease.OutQuad);
                _scaleTween = transform.DOScale(1.0f, 0.3f).SetEase(Ease.OutQuad);

                if (_shadowOffset != null)
                {
                    _shadowTween = _shadowOffset.DOAnchorPos(Vector2.zero, 0.3f).SetEase(Ease.OutQuad);
                }
            }
        }

        public void FinalizePlacement(Transform pageParent, Vector2 pageAnchoredPosition)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1.0f;
            }

            transform.SetParent(pageParent, true);
            _rectTransform.anchoredPosition = pageAnchoredPosition;
            
            // Update fallback targets to prevent snapping back to original inventory tray on future invalid drops
            _originalParent = pageParent;
            _startPosition = pageAnchoredPosition;

            // Return to physical state
            _scaleTween = transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad);
            if (_shadowOffset != null)
            {
                _shadowTween = _shadowOffset.DOAnchorPos(new Vector2(-3f, -4f), 0.2f).SetEase(Ease.OutQuad);
            }
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();
            _shadowTween?.Kill();
        }
    }
}
```

**Step 3: Create `StickerBookPage` placement zone**
Create `Assets/CozyLifeSim/Scripts/UI/StickerBookPage.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;

namespace CozyLifeSim.UI
{
    [RequireComponent(typeof(Image))]
    public class StickerBookPage : MonoBehaviour
    {
        [SerializeField] private int _pageIndex;

        public int PageIndex => _pageIndex;

        public bool TryPlaceSticker(CozySticker sticker, Vector2 screenPosition, Camera eventCamera)
        {
            RectTransform pageRect = GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(pageRect, screenPosition, eventCamera, out Vector2 localPoint))
            {
                // Confirm drop is fully within page boundaries
                if (pageRect.rect.Contains(localPoint))
                {
                    sticker.FinalizePlacement(transform, localPoint);
                    return true;
                }
            }
            return false;
        }
    }
}
```

**Step 4: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/UI/CozyLifeSim.UI.asmdef Assets/CozyLifeSim/Scripts/UI/CozySticker.cs Assets/CozyLifeSim/Scripts/UI/StickerBookPage.cs
git commit -m "feat: implement tactile sticker drag-and-drop system with DOTween feedback"
```

---

### Task 2: Pseudo-3D Notebook Page Flip

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/StickerBook.cs`

**Step 1: Write `StickerBook` controller**
Manage transitioning between multiple sticker book pages. Animate page flips by using DOTween to scale the width (`DOScaleX`) of a transition canvas down to `0.0f` and then back to `1.0f`, swapping the content page exactly at the midpoint (`0.0f`).
Create `Assets/CozyLifeSim/Scripts/UI/StickerBook.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

namespace CozyLifeSim.UI
{
    public class StickerBook : MonoBehaviour
    {
        [SerializeField] private List<StickerBookPage> _pages;
        [SerializeField] private RectTransform _flipPageIndicator; // Target for ScaleX compression
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _prevButton;

        private int _currentPageIndex = 0;
        private bool _isTransitioning = false;
        private Sequence _flipSequence;

        private void Start()
        {
            if (_pages == null || _pages.Count == 0)
            {
                if (_nextButton != null) _nextButton.interactable = false;
                if (_prevButton != null) _prevButton.interactable = false;
                return;
            }

            // Set initial state
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i] != null)
                {
                    _pages[i].gameObject.SetActive(i == _currentPageIndex);
                }
            }

            if (_nextButton != null) _nextButton.onClick.AddListener(NextPage);
            if (_prevButton != null) _prevButton.onClick.AddListener(PrevPage);
            
            UpdateNavigationButtons();
        }

        public void NextPage()
        {
            if (_pages == null || _pages.Count == 0) return;
            if (_currentPageIndex < _pages.Count - 1)
            {
                FlipPage(_currentPageIndex + 1);
            }
        }

        public void PrevPage()
        {
            if (_pages == null || _pages.Count == 0) return;
            if (_currentPageIndex > 0)
            {
                FlipPage(_currentPageIndex - 1);
            }
        }

        private void FlipPage(int targetIndex)
        {
            if (_isTransitioning || _pages == null || targetIndex < 0 || targetIndex >= _pages.Count) return;
            _isTransitioning = true;

            if (_pages[targetIndex] == null || _pages[_currentPageIndex] == null)
            {
                _isTransitioning = false;
                return;
            }

            if (_flipPageIndicator != null)
            {
                _flipPageIndicator.gameObject.SetActive(true);
                _flipPageIndicator.localScale = Vector3.one;

                _flipSequence?.Kill();
                _flipSequence = DOTween.Sequence();

                // Animate half-flip compression
                _flipSequence.Append(_flipPageIndicator.DOScaleX(0.0f, 0.25f).SetEase(Ease.InQuad));
                
                // Swap pages at midpoint
                _flipSequence.AppendCallback(() =>
                {
                    _pages[_currentPageIndex].gameObject.SetActive(false);
                    _currentPageIndex = targetIndex;
                    _pages[_currentPageIndex].gameObject.SetActive(true);

                    UpdateNavigationButtons();
                });

                // Animate half-flip expansion
                _flipSequence.Append(_flipPageIndicator.DOScaleX(1.0f, 0.25f).SetEase(Ease.OutQuad));
                
                // Complete page swap
                _flipSequence.OnComplete(() =>
                {
                    _flipPageIndicator.gameObject.SetActive(false);
                    _isTransitioning = false;
                });
            }
            else
            {
                // Fallback direct swap if visual asset not assigned
                _pages[_currentPageIndex].gameObject.SetActive(false);
                _currentPageIndex = targetIndex;
                _pages[_currentPageIndex].gameObject.SetActive(true);
                UpdateNavigationButtons();
                _isTransitioning = false;
            }
        }

        private void UpdateNavigationButtons()
        {
            if (_pages == null || _pages.Count == 0)
            {
                if (_nextButton != null) _nextButton.interactable = false;
                if (_prevButton != null) _prevButton.interactable = false;
                return;
            }
            if (_nextButton != null) _nextButton.interactable = _currentPageIndex < _pages.Count - 1;
            if (_prevButton != null) _prevButton.interactable = _currentPageIndex > 0;
        }

        private void OnDestroy()
        {
            _flipSequence?.Kill();
            if (_nextButton != null) _nextButton.onClick.RemoveListener(NextPage);
            if (_prevButton != null) _prevButton.onClick.RemoveListener(PrevPage);
        }
    }
}
```

**Step 2: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/UI/StickerBook.cs
git commit -m "feat: implement 2D pseudo-3D notebook page-flipping mechanism via DOTween ScaleX"
```

---

### Task 3: Real-Time Crop Growth Logic Loop with UniTask Timer

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/CropWidget.cs`

**Step 1: Write `CropWidget` component**
Implement the crop growth logic driven by a background `UniTask.Delay` timer (ticking every 1s, zero GC, absolutely NO Unity Update calls). Provide watering interaction feedback: tilts the watering can, triggers a shake rotation to water, and shakes the crop on growth stage increments.
Create `Assets/CozyLifeSim/Scripts/UI/CropWidget.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI
{
    public class CropWidget : MonoBehaviour
    {
        [SerializeField] private int _cropId;
        [SerializeField] private float _stageDurationSeconds = 5f;
        
        [Header("UI Visuals")]
        [SerializeField] private Image _cropVisual;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Button _waterButton;
        [SerializeField] private RectTransform _wateringCan;

        [Header("Stage Sprites")]
        [SerializeField] private Sprite _seedSprite;
        [SerializeField] private Sprite _sproutSprite;
        [SerializeField] private Sprite _matureSprite;
        [SerializeField] private Sprite _harvestSprite;

        private CropState _state;
        private CancellationTokenSource _cts;
        private bool _isWatering = false;

        private void Start()
        {
            _state = new CropState(_cropId, 0, _stageDurationSeconds, false);
            UpdateVisuals();

            if (_waterButton != null)
            {
                _waterButton.onClick.AddListener(Irrigate);
            }

            _cts = new CancellationTokenSource();
            RunGrowthTimer(_cts.Token).Forget();
        }

        private async UniTaskVoid RunGrowthTimer(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await UniTask.Delay(1000, cancellationToken: token);

                    if (_state.GrowthStage < 3 && _state.IsWatered)
                    {
                        _state.TimeRemainingSeconds -= 1.0f;
                        if (_state.TimeRemainingSeconds <= 0f)
                        {
                            AdvanceStage();
                        }
                        UpdateVisuals();
                    }
                }
            }
            catch (System.OperationCanceledException)
            {
                // Safe and clean exit when widget is destroyed
            }
        }

        private void AdvanceStage()
        {
            _state.GrowthStage++;
            _state.TimeRemainingSeconds = _stageDurationSeconds;
            _state.IsWatered = false; // Requires watering for next stage

            // Juicy juice: bounce crop scale when growing
            if (_cropVisual != null)
            {
                _cropVisual.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0f), 0.5f, 10, 1f);
            }
        }

        private void Irrigate()
        {
            if (_isWatering || _state.IsWatered || _state.GrowthStage >= 3) return;
            _isWatering = true;

            if (_wateringCan != null)
            {
                // Position watering can and tilt 45 deg
                _wateringCan.gameObject.SetActive(true);
                _wateringCan.localScale = Vector3.zero;
                _wateringCan.localRotation = Quaternion.identity;

                Sequence seq = DOTween.Sequence();
                seq.Append(_wateringCan.DOScale(1.0f, 0.25f).SetEase(Ease.OutBack));
                seq.Append(_wateringCan.DORotate(new Vector3(0f, 0f, -45f), 0.3f).SetEase(Ease.OutQuad));
                seq.Append(_wateringCan.DOShakeRotation(0.5f, new Vector3(0f, 0f, 15f), 10, 90));
                
                seq.OnComplete(() =>
                {
                    _state.IsWatered = true;
                    _isWatering = false;

                    // Shake crop to show it's watered
                    if (_cropVisual != null)
                    {
                        _cropVisual.transform.DOShakePosition(0.4f, 8f, 15);
                    }

                    // Return and hide can
                    _wateringCan.DORotate(Vector3.zero, 0.2f);
                    _wateringCan.DOScale(0.0f, 0.2f).OnComplete(() => _wateringCan.gameObject.SetActive(false));
                    
                    UpdateVisuals();
                });
            }
            else
            {
                // Direct fallback
                _state.IsWatered = true;
                _isWatering = false;
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            if (_cropVisual != null)
            {
                _cropVisual.sprite = _state.GrowthStage switch
                {
                    0 => _seedSprite,
                    1 => _sproutSprite,
                    2 => _matureSprite,
                    3 => _harvestSprite,
                    _ => _seedSprite
                };
            }

            if (_timerText != null)
            {
                if (_state.GrowthStage >= 3)
                {
                    _timerText.text = "READY TO HARVEST!";
                }
                else if (!_state.IsWatered)
                {
                    _timerText.text = "NEED WATER!";
                }
                else
                {
                    _timerText.text = $"Growing: {Mathf.CeilToInt(_state.TimeRemainingSeconds)}s";
                }
            }

            if (_waterButton != null)
            {
                _waterButton.interactable = !_state.IsWatered && _state.GrowthStage < 3;
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            if (_waterButton != null) _waterButton.onClick.RemoveListener(Irrigate);
        }
    }
}
```

**Step 2: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/UI/CropWidget.cs
git commit -m "feat: implement real-time UniTask-based crop growth loop and juice-tilted watering can"
```

---

### Task 4: Interactive Animal Breathing & Petting

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs`

**Step 1: Write `AnimalWidget` component**
Model a breathing chicken. Simulate natural breathing by starting an infinite Yoyo scale animation (`DOScaleY`). On-click, provide positive micro-feedback: trigger a physical jump (`DOJump`) and spawn a floating heart or scale pop-up.
Create `Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CozyLifeSim.UI
{
    public class AnimalWidget : MonoBehaviour
    {
        [SerializeField] private Button _interactionButton;
        [SerializeField] private RectTransform _heartPrefab; // Spawned on petting
        [SerializeField] private Transform _spawnRoot;

        private Tween _breathTween;
        private Sequence _petSequence;

        private void Start()
        {
            // Start infinite breathing simulation
            // LoopType.Yoyo compresses and decompresses scale over a natural breathing interval (1.5s)
            _breathTween = transform.DOScaleY(1.03f, 1.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            if (_interactionButton != null)
            {
                _interactionButton.onClick.AddListener(PetAnimal);
            }
        }

        private void PetAnimal()
        {
            // Pet jump sequence
            _breathTween?.Pause();
            _petSequence?.Kill(true);

            _petSequence = DOTween.Sequence();

            // Perform physical hop (UGUI-safe RectTransform anchored position jump to prevent layout shifting)
            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                Vector2 startPos = rect.anchoredPosition;
                _petSequence.Append(rect.DOAnchorPosY(startPos.y + 25f, 0.2f).SetEase(Ease.OutQuad));
                _petSequence.Append(rect.DOAnchorPosY(startPos.y, 0.2f).SetEase(Ease.InQuad));
            }
            else
            {
                // Fallback for non-UI transform
                _petSequence.Append(transform.DOJump(transform.position, 25f, 1, 0.4f).SetUpdate(true));
            }
            
            // Pop heart reaction
            if (_heartPrefab != null && _spawnRoot != null)
            {
                RectTransform heart = Instantiate(_heartPrefab, _spawnRoot);
                heart.localPosition = Vector2.zero;
                heart.localScale = Vector3.zero;

                Sequence heartSeq = DOTween.Sequence();
                heartSeq.Append(heart.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
                heartSeq.Join(heart.DOLocalMoveY(60f, 0.6f).SetEase(Ease.OutQuad));

                // Safe fetch: add CanvasGroup dynamically if missing to prevent NullReference crash
                if (!heart.TryGetComponent<CanvasGroup>(out var canvasGroup))
                {
                    canvasGroup = heart.gameObject.AddComponent<CanvasGroup>();
                }
                heartSeq.Insert(0.4f, canvasGroup.DOFade(0.0f, 0.2f));

                heartSeq.OnComplete(() => Destroy(heart.gameObject));
            }

            _petSequence.OnComplete(() =>
            {
                transform.localScale = Vector3.one;
                _breathTween?.Play();
            });
        }

        private void OnDestroy()
        {
            _breathTween?.Kill();
            _petSequence?.Kill();
            if (_interactionButton != null) _interactionButton.onClick.RemoveListener(PetAnimal);
        }
    }
}
```

**Step 2: Commit**
```bash
git add Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs
git commit -m "feat: implement chicken breathing simulation loop and click jump petting feedback"
```

---

## Verification Plan

### Automated Verification
Since the project is using Unity 6 Assembly Definitions:
1. Ensure the project compiles cleanly under Unity Hub without compilation issues.
2. In the Unity Editor, verify that the console outputs zero errors.

### Manual Verification
1. **Sticker Placement:** Drag a sticker from the inventory, let it hover, release inside/outside page area. Verify it snaps back if outside, and attaches relative to parent page without shifting double offset when inside.
2. **Page Flip:** Press the Next/Prev buttons. Verify that the transition canvas correctly shrinks on the X-axis, swaps pages at scale X = 0, and opens cleanly.
3. **Crop Timer:** Tap the Watering Can button. Verify it tilts and shakes, is marked as watered, and grows each 5-second interval without using `Update()`.
4. **Chicken Breathing:** Check that the chicken smoothly scales up and down continuously. Click the petting button and watch it jump and spawn a floating feedback element.

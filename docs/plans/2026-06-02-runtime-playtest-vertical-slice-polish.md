# Runtime Playtest & Vertical Slice Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Turn the existing farm, shop, quest, NPC dialogue, scrapbook, and diary systems into a smooth playable vertical slice with clear feedback, predictable navigation, and automated coverage for the full user journey.

**Architecture:** Keep the existing MVP/service structure intact. Add a small UI feedback surface and targeted presenter/widget affordances, then expand Editor validation to simulate the real vertical slice path without mutating the player's persistent save. Scene setup remains the single source for generated hierarchy wiring and must stay idempotent.

**Tech Stack:** Unity 6000.3.11f1, UGUI, TextMeshPro, DOTween, UniTask, VContainer, Editor validation menu items.

---

## I. Scope And Review Notes

> [!IMPORTANT]
> **This task is polish and validation, not a new content system.**
> * Do not add a new brand, rename the project, or change the product identity.
> * Do not depend on Task 35 secondary assets. Missing optional assets must continue to use fallback rendering.
> * Do not rewrite existing services. Extend only the UI and validation seams needed for a playable vertical slice.
> * Do not commit unless the user explicitly requests a commit.

**Vertical slice target flow:**

1. Player enters Play Mode with a clean or normalized save.
2. Player sees current coins/seeds/crops/level and active quest state.
3. Player opens shop, understands why items are buyable, locked, or unaffordable.
4. Player buys seed/sticker, plants, waters through growth, harvests, sells crops, and receives coin/XP feedback.
5. Player completes a quest and sees dialogue feedback after the successful save-backed reward.
6. Player opens scrapbook, places a sticker, changes page style, writes a diary note, drags it, deletes it, and reloads without data drift.
7. Validation restores the original save after the automated run.

---

## II. Proposed Changes

### Task 1: Add A Lightweight Runtime Feedback Toast

**Files:**
- Create: `Assets/CozyLifeSim/Scripts/UI/CozyFeedbackToast.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs`
- Unity-generated: `Assets/CozyLifeSim/Scripts/UI/CozyFeedbackToast.cs.meta`

Do not manually create `CozyFeedbackToast.cs.meta`. After adding the script, wait for Unity import/compile to generate the `.meta`, then include it in the final changed scope if the task is committed later.

- [ ] **Step 1: Create `CozyFeedbackToast`**

Add a popup-independent UI component that can be found by runtime widgets without creating service-layer dependencies.

Required behavior:
- `Show(string message)` trims input and ignores empty messages.
- Shows a small text panel near the lower center of the canvas.
- Kills prior DOTween animations before showing a new message.
- Uses `CanvasGroup` for fade and keeps raycast blocking disabled.
- Cleans tweens in `OnDestroy`.

Implementation shape:

```csharp
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CozyLifeSim.UI
{
    public class CozyFeedbackToast : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _contentPanel;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private float _visibleSeconds = 1.4f;

        private Sequence _sequence;

        public void Show(string message)
        {
            string resolved = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
            if (string.IsNullOrEmpty(resolved)) return;

            if (_messageText != null) _messageText.text = resolved;
            if (_contentPanel != null) _contentPanel.gameObject.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
                _canvasGroup.alpha = 0f;
            }

            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetTarget(this);
            if (_contentPanel != null)
            {
                _contentPanel.localScale = new Vector3(0.96f, 0.96f, 1f);
                _sequence.Join(_contentPanel.DOScale(1f, 0.12f).SetEase(Ease.OutBack));
            }
            if (_canvasGroup != null)
            {
                _sequence.Join(_canvasGroup.DOFade(1f, 0.12f));
                _sequence.AppendInterval(Mathf.Max(0.1f, _visibleSeconds));
                _sequence.Append(_canvasGroup.DOFade(0f, 0.18f));
            }
            _sequence.OnComplete(() =>
            {
                if (_contentPanel != null) _contentPanel.gameObject.SetActive(false);
            });
        }

        private void Awake()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            if (_contentPanel != null) _contentPanel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
            DOTween.Kill(this);
        }
    }
}
```

- [ ] **Step 2: Register and wire the toast**

In `GameLifetimeScope.cs`, add serialized field `CozyFeedbackToast _feedbackToast` next to `_dialoguePopup`. Register the serialized reference when assigned, otherwise use the same active-scene fallback pattern already used for `CozyJuiceUtility` and `CozyDialoguePopup`:

```csharp
if (_feedbackToast != null)
{
    builder.RegisterComponent(_feedbackToast);
}
else
{
    var foundToast = FindFirstObjectByType<CozyFeedbackToast>();
    if (foundToast != null)
    {
        builder.RegisterComponent(foundToast);
    }
    else
    {
        Debug.LogWarning("[CozySim] CozyFeedbackToast is missing from the scene!");
    }
}
```

In `CozySceneSetupWindow.cs`, generate:
- `Canvas/Popup_Root/FeedbackToast`
- child content panel with `CanvasGroup`
- `TextMeshProUGUI` message text
- serialized references on `CozyFeedbackToast`
- serialized reference on `GameLifetimeScope`

`FeedbackToast` itself must remain active in the hierarchy so `FindFirstObjectByType<CozyFeedbackToast>()` works. Hide only its `_contentPanel` child at startup.

Keep all assignments guarded with existing idempotent helpers so consecutive silent setup creates exactly zero scene diffs.

- [ ] **Step 3: Add scene validation for toast wiring**

In `CozyLifeSimSceneGameplayValidation.cs`, add validation that:
- `CozyFeedbackToast` exists.
- `_canvasGroup`, `_contentPanel`, and `_messageText` are assigned.
- `CanvasGroup.blocksRaycasts == false`.
- `GameLifetimeScope` has `_feedbackToast` assigned.

Expected validation result: existing scene validation passes with the same expected optional asset warnings only.

---

### Task 2: Make Shop Disabled States Explain Themselves

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/UI/ShopItemWidget.cs`
- Modify: `Assets/CozyLifeSim/Scripts/UI/ShopPopup.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs`

- [ ] **Step 1: Extend `ShopItemWidget.Setup` with a disabled reason**

Add serialized field:

```csharp
[SerializeField] private TextMeshProUGUI _disabledReasonText;
```

Replace the current setup signature with:

```csharp
public void Setup(
    string itemName,
    Sprite icon,
    int price,
    string buttonLabel,
    bool isInteractable,
    string disabledReason,
    System.Action onAction)
```

Required UI rules:
- Button remains non-interactable when blocked.
- `_disabledReasonText` is visible only when blocked and reason is non-empty.
- Empty reason hides the text.
- Existing button listener behavior remains unchanged.

- [ ] **Step 2: Compute concrete reasons in `ShopPopup.RefreshShop`**

For seed and sticker purchases:

```csharp
string reason = string.Empty;
if (playerLevel < requiredLevel)
{
    reason = $"Requires Level {requiredLevel}";
}
else if (_inventoryService == null || _inventoryService.Coins < buyPrice)
{
    reason = $"Need {buyPrice} Coins";
}
```

For crop selling:

```csharp
string reason = _inventoryService == null || _inventoryService.Crops <= 0
    ? "No crops to sell"
    : string.Empty;
```

Pass the reason into `ShopItemWidget.Setup`.

- [ ] **Step 3: Update scene setup and validation**

In `CozySceneSetupWindow.cs`, add a small disabled reason text under the price/action row on the shop item template and wire `_disabledReasonText`.

In `CozyLifeSimValidation.cs`, add a logic-style UI test that creates a temporary `ShopItemWidget`, wires TextMeshPro fields with serialized object assignment matching existing validation style, calls `Setup` twice, invokes the widget's private `Start()` once through reflection to mirror Unity lifecycle listener registration in Edit Mode, then invokes the public button path with `_actionButton.onClick.Invoke()` and asserts:
- blocked setup shows `"Need 999 Coins"`;
- interactable setup hides the reason text;
- action callback still fires through the button click event.

---

### Task 3: Add Farm Action Feedback For Blocked Player Actions

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/UI/CropWidget.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs`

- [ ] **Step 1: Resolve `CozyFeedbackToast` safely in `CropWidget`**

Add a private field:

```csharp
private CozyFeedbackToast _feedbackToast;
```

In `Start()`, after existing DI lookup:

```csharp
if (_feedbackToast == null)
{
    _feedbackToast = FindFirstObjectByType<CozyFeedbackToast>();
}
```

Add helper:

```csharp
private void ShowFeedback(string message)
{
    if (_feedbackToast != null)
    {
        _feedbackToast.Show(message);
    }
}
```

- [ ] **Step 2: Provide concrete messages for blocked actions**

In `PlantSeed()`:
- if crop is already planted, show `"This plot is already growing."`;
- if presenter is null, show `"Farm is not ready yet."`;
- if `TryPlantCrop()` returns false, show `"You need a seed to plant."`.

In `Irrigate()`:
- if empty plot, show `"Plant a seed first."`;
- if already watered, show `"Already watered."`;
- if harvest-ready, show `"Ready to harvest."`;
- if currently watering, show no message.

In `HarvestCrop()`:
- if crop is not ready, show `"Keep growing this crop."`;
- if presenter is null, show `"Farm is not ready yet."`.

- [ ] **Step 3: Add validation for message routing**

In `CozyLifeSimValidation.cs`, add a test for `CozyFeedbackToast.Show` that verifies:
- blank input on a freshly initialized toast does not activate the content panel;
- non-empty input activates the content panel and writes the trimmed message;
- second message replaces the first message.

Call the private `Awake()` method through reflection in the Edit Mode logic test before assertions so the initial hidden state matches runtime initialization. Do not require waiting for DOTween completion in this logic test.

---

### Task 4: Add Vertical Slice Validation Coverage

**Files:**
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimMcpGameplayLoopValidation.cs`
- Modify: `Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimSceneGameplayValidation.cs`

- [ ] **Step 1: Extend MCP gameplay validation steps**

Add steps after existing sticker placement and before save restore:
- `Open scrapbook and change page style`
- `Create diary note`
- `Move diary note`
- `Remove diary note`
- `Complete quest dialogue trigger`

The validation must use concrete, registered runtime dependencies:
- `IMemoryService` for checking `PlacedDiaryNotes` and `PageStyles`.
- Resolve `StickerBookPresenter` from `GameLifetimeScope.Container`; fail the validation step if it cannot be resolved.
- Resolve the active `StickerBook`, read its private serialized `_pages` list through `SerializedObject`, and use the first non-null `StickerBookPage` from that list. Compute `maxStyleCount` from `firstPage.StyleCount`; fail the validation step if no page is assigned or `maxStyleCount <= 0`.
- Use `StickerBookPresenter.TrySetPageStyle(0, 1, maxStyleCount)`, `TryAddDiaryNote("Playtest note", 12f, -18f, 0)`, `TryUpdateDiaryNotePosition(noteId, 24f, -30f)`, and `TryRemoveDiaryNote(noteId)` so the same transaction and rollback code path used by gameplay is covered.
- Add `using Cysharp.Threading.Tasks;` to the validation file if needed for `.Forget()`.
- Resolve `CozyDialoguePopup` from the scene, call `ShowDialogue("Ba", "Con lam tot lam.", null).Forget()`, then call `SkipOrNext()` once to force the full text path. Fail if the popup or its content panel cannot be found.

The test must preserve the existing backup/restore pattern used by `CozyLifeSimMcpGameplayLoopValidation`.

- [ ] **Step 2: Add scene validation for scrapbook and dialogue playability**

Extend `CozyLifeSimSceneGameplayValidation.cs` with checks that:
- `StickerBook` exists and page references are assigned.
- every generated `StickerBookPage` has a diary note template and add-note/page-style controls assigned.
- `CozyDialoguePopup` parent GameObject is active and its internal content panel starts hidden.
- each `CozyNPCWidget` has `_npcData.NpcName` set and at least one serialized `_npcData.Dialogues[*].Line` with non-empty text. Treat an empty dialogue list as a scene validation error, not a fallback.

- [ ] **Step 3: Expected verification commands**

Run in Unity after scripts compile:

```text
Tools/CozySim/Run Logic Verification Tests
Tools/CozySim/Run Scene Gameplay Loop Validation
```

Then enter Play Mode and run:

```text
Tools/CozySim/Run MCP Gameplay Loop Validation
Tools/CozySim/Check Loop Validation Status
```

Expected:
- logic tests pass;
- scene validation has 0 errors and only known optional asset warnings;
- runtime loop restores the original save;
- no compiler errors in Console/Editor log.

---

### Task 5: Manual Playtest Script And Handoff

**Files:**
- Create: `docs/plans/2026-06-02-runtime-playtest-checklist.md`
- Modify: `docs/plans/task.md`
- Modify: `docs/plans/current-handoff.md`

- [ ] **Step 1: Create a concise manual playtest checklist**

The checklist should be short and executable by a human in under 10 minutes:

```markdown
# Runtime Playtest Checklist

- [ ] Start Play Mode from `Assets/CozyLifeSim/Scenes/Main.unity`.
- [ ] Confirm HUD shows coins, seeds, crops, and level.
- [ ] Open shop, verify locked/unaffordable items explain why.
- [ ] Buy one seed and one sticker when affordable.
- [ ] Plant, water through all stages, harvest, and sell a crop.
- [ ] Complete at least one quest and confirm dialogue appears.
- [ ] Open scrapbook, place a sticker, cycle page style, add a diary note, drag it, and delete it.
- [ ] Stop and restart Play Mode; confirm persisted state is coherent.
- [ ] Run scene validation after playtest; confirm no scene wiring errors.
```

- [ ] **Step 2: Update handoff files after implementation**

When Task 36 is implemented and verified:
- mark Task 36 `[x]` in `docs/plans/task.md`;
- update `docs/plans/current-handoff.md` with completed work, verification, warnings, uncommitted scope, and recommended next task;
- do not add long logs to handoff files.

---

## III. Self-Review

**Spec coverage:** The plan covers full-loop player feedback, shop disabled states, farm blocked-action feedback, scrapbook/diary/dialogue runtime validation, manual playtest, and handoff updates.

**Scope check:** Main menu/new game/continue flow is intentionally excluded. It can become Task 37 after this vertical slice is smooth.

**Risk controls:**
- Existing service atomicity remains unchanged.
- Validation restores saves after automated runtime checks.
- Scene setup remains idempotent.
- Task 35 optional assets remain non-blocking through existing fallback behavior.

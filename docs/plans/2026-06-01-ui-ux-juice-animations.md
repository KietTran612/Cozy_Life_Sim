# UI/UX Juice & Animations Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Nang tam trai nghiem tuong tac trong game lam song dong hon (wow factor) thong qua viec danh bong giao dien bang he thong chuyen dong dan hoi, thanh kinh nghiem fill tu tu, va hieu ung tien bay kem visual coin ticker.

**Architecture:** Su dung thu vien DOTween de thuc hien cac phep bien doi vector ve Scale, Position, va Float. Trien khai cac lop UI Widget va Utility doc lap de quan ly hieu ung phan hoi, dang ky dependency qua VContainer, tu dong hoa setup va hoan toan an toan lifecycle.

**Tech Stack:** Unity UGUI, C# scripting, DOTween, VContainer DI.

---

## I. Yeu Cau Danh Gia Tu Nguoi Dung (User Review Required)

> [!IMPORTANT]
> **Refactor Co Che Coin Fly (Giam Toi Da GC Churn):**
> *   Khong dung co che Instantiate/Destroy runtime cho visual coin fly. Thiet ke pool ho tro warmup an toan khi khoi dong.
> *   Dat muc tieu **"Khong Instantiate/Destroy runtime; giam toi da GC churn"**. Bo sung `DOTween.SetTweensCapacity` de toi uu hoa truoc dung luong tween va sequence cua DOTween khi khoi dong.
> *   Khong viet static class `CozyJuiceUtility`. Thay vao do, chung ta se xay dung **`CozyJuiceUtility` duoi dang MonoBehaviour component** (Singleton runtime gan voi Canvas va duoc dang ky qua GameLifetimeScope).
> *   Vong doi cua Object Pool cho dong xu bay se **gan chat vao Canvas cua Scene**. Khi scene reload hoac Canvas bi destroy, toan bo pool se duoc don dep (`Clear` trong `OnDestroy`) de tranh hien tuong reference chet (MissingReferenceException).
> 
> **VContainer Dependency Registration & Widget Runtime Optional Fallback:**
> *   Khong dung Singleton tinh toan cuc. `CozyJuiceUtility` duoc attach vao Canvas/UI_Root va duoc dang ky trong `GameLifetimeScope.cs` bang `builder.RegisterComponent(_juiceUtility)` (them field serialized tuong ung).
> *   De dam bao an toan tuyet doi neu scene field chua duoc wire hoac chay trong validation test thieu utility, `GameLifetimeScope.Configure` se co co che null-guard va runtime fallback.
> *   De ngan ngua hoan toan loi DI resolution failure co the pha vo do thi dependency (DI Graph) khi VContainer cố gang resolve tham so tham chieu (ke ca khi co mac dinh `= null`), cac widgets **se khong nhan CozyJuiceUtility qua injection parameter cua ham Construct**.
> *   Thay vao do, cac widgets (`ShopPopup`, `CropWidget`, `AnimalWidget`) se su dung co che **runtime optional lookup/fallback**: Chung se khai bao `private CozyJuiceUtility _juiceUtility;` va trong ham `Start()` se tu dong lay tham chieu bang cach dung `FindFirstObjectByType<CozyJuiceUtility>()` neu chua duoc gan truoc. Co che nay giup tach biet hoan toan khoi DI container khi thieu component ma khong he crash game.
> 
> **Dong Bo Target Position Cua Coin Fly Trong CozyJuiceUtility:**
> *   Dong xu bay can co dich den ro rang de dien hoat chinh xac. `CozyJuiceUtility` se bo sung mot truong serialized:
>     `[SerializeField] private RectTransform _coinTargetTransform;`
> *   Trong qua trinh setup scene (`CozySceneSetupWindow.cs`), cong cu setup se tu dong tim kiem UI element `Coins_Text` va gan `RectTransform` cua no vao `_coinTargetTransform` cua `CozyJuiceUtility` mot cach **idempotent** (khong lam thay doi git diff neu da trung khop).
> 
> **Nang Cap Event OnCropHarvested Thanh Action<int>:**
> *   Thay doi khai bao su kien `OnCropHarvested` tu `Action` thanh `Action<int>` truyen luong coin thu hoach (10 coins) tu `FarmPresenter`.
> *   De dam bao khong lam break compilation cua he thong hien tai, quy trinh thuc thi bat buoc phai **quet, kiem tra va cap nhat toan bo cac call sites, subscribers, va cac test files co lien quan** (nhu `FarmPresenter.cs`, test classes, mock helpers) dong bo theo signature moi.

---

## II. Chi Tiet Cac Thay Doi De Xuat (Proposed Changes)

### Task 1: Cap nhat API Progression Service va nang cap event OnCropHarvested trong FarmPresenter

**Files:**
- Modify: [IProgressionService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/IProgressionService.cs)
- Modify: [ProgressionService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/ProgressionService.cs)
- Modify: [FarmPresenter.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Presenters/FarmPresenter.cs)

**Step 1: Cap nhat Interface Progression**
*   Them phuong thuc vao `IProgressionService.cs`:
    `int GetXPThresholdForLevel(int level);`

**Step 2: Trien khai trong ProgressionService.cs**
*   Thay doi phuong thuc `GetXPThreshold` trong `ProgressionService.cs` tu `private` thanh `public` de hien thuc hoa interface moi:
    `public int GetXPThresholdForLevel(int level) => GetXPThreshold(level);`

**Step 3: Cap nhat FarmPresenter.cs truyen coin amount va ra soat subscribers**
*   Thay doi khai bao event:
    `public event Action<int> OnCropHarvested;`
*   Thay doi cac dong goi event trong `HarvestCrop()` de truyen tham so 10:
    `OnCropHarvested?.Invoke(10);`
*   Thuc hien tim kiem va cap nhat bat ky file nao dang ky nghe su kien `OnCropHarvested += ...` hoac mock test de phu hop voi chu ky `Action<int>`.

---

### Task 2: CozyButtonJuice - Click Co Gian Dan Hoi & Lifecycle Guard

**Files:**
- Create: [CozyButtonJuice.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozyButtonJuice.cs)
- Modify: [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)

**Step 1: Code logic bat-up scale co an toan lifecycle**
*   File duoc dat tai [CozyButtonJuice.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozyButtonJuice.cs) dong bo voi namespace `CozyLifeSim.UI` cua cac UI components khac.
*   Su dung suffix `f` dung chuan float literal trong C# (`0.1f`, `0.12f`...) de dam bao compile thanh cong.
*   Dung `Awake()` de capture `_originalScale` kem theo co `_hasOriginalScale` de ngan chan tinh trang race condition khi `OnDisable` phat dong truoc `Start` (khien scale bi set ve zero).
*   Su dung `Sequence` cua DOTween de quan ly toan bo chuoi hoat anh nay len 1.05f va thu nho ve ban dau. Khi goi `_scaleTween?.Kill()` se dong loat tat va reset dong bo ca sequence ma khong de lai bat ky tween con nao chay ngam (unmanaged nested tween).
*   Ke thua tu `IPointerDownHandler`, `IPointerUpHandler`, `IPointerExitHandler` de khoi phuc lai scale khi chuot di ra ngoai button.
*   Xu ly `OnDisable()` va `OnDestroy()` de giai phong tween va khoi phuc scale goc.

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace CozyLifeSim.UI
{
    public class CozyButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Vector3 _originalScale;
        private bool _hasOriginalScale;
        private Tween _scaleTween;

        private void Awake()
        {
            if (!_hasOriginalScale)
            {
                _originalScale = transform.localScale;
                _hasOriginalScale = true;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ResetTween();
            _scaleTween = transform.DOScale(_originalScale * 0.92f, 0.1f).SetEase(Ease.OutQuad).SetTarget(this);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetTween();
            
            // Dung Sequence de quan ly ca tween cha lan tween con dong bo
            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.Append(transform.DOScale(_originalScale * 1.05f, 0.12f).SetEase(Ease.OutBack));
            seq.Append(transform.DOScale(_originalScale, 0.1f));
            
            _scaleTween = seq;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResetScale();
        }

        private void OnDisable()
        {
            ResetScale();
        }

        private void OnDestroy()
        {
            ResetScale();
        }

        private void ResetTween()
        {
            _scaleTween?.Kill();
        }

        private void ResetScale()
        {
            ResetTween();
            if (_hasOriginalScale)
            {
                transform.localScale = _originalScale;
            }
        }
    }
}
```

**Step 2: Tu dong attach CozyButtonJuice vao tat ca Button**
*   Sua logic ham `SetupButton` cua `CozySceneSetupWindow.cs` de tu dong gan component `CozyButtonJuice` cho bat ky button nao duoc setup. Snippet su dung bien local `btn` phu hop voi ma nguon hien tai:
```csharp
CozyButtonJuice juice = btn.gameObject.GetComponent<CozyButtonJuice>();
if (juice == null)
{
    juice = btn.gameObject.AddComponent<CozyButtonJuice>();
    isDirty = true;
}
```
*   Bao toan co `isDirty` de scene duoc danh dau update va git diff scene setup lan sau van bang 0.

---

### Task 3: ProgressionHudWidget & Wiring in Scene Setup

**Files:**
- Create: [ProgressionHudWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/ProgressionHudWidget.cs)
- Modify: [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)

**Step 1: Code ProgressionHudWidget.cs**
*   Widget chua 2 fields serialized:
    *   `[SerializeField] private TextMeshProUGUI _levelText;`
    *   `[SerializeField] private Image _xpProgressBar;`
*   Ho tro Inject `IProgressionService` va trien khai **Initial Render (Hien thi ngay khi bat dau)** de tranh default state bi sai lech truoc khi co event moi.
*   Dang ky va **huy dang ky (unsubscribe)** su kien `OnXPChanged` va `OnLevelUp` trong `OnDestroy()`.
*   Xu ly chong tran thanh XP (Level up order): Dung `Mathf.Clamp01((float)xp / threshold)` truoc khi tween de ngat triet de bug fill > 1 khi event XP phat truoc OnLevelUp.
*   Clean up tweens cua slider/level text trong `OnDestroy()`.

**Step 2: Viet scene setup generator & wire trong CozySceneSetupWindow.cs**
*   Tu dong instantiate `Progression_HUD` lam con cua `Header_Panel`.
*   **Thiet lap cau hinh Image.type = Filled**:
    *   Truoc khi attach widget, phai lay UGUI Image cua thanh XP va set:
        `image.type = Image.Type.Filled;`
        `image.fillMethod = Image.FillMethod.Horizontal;`
        `image.fillOrigin = (int)Image.OriginHorizontal.Left;`
        `image.fillAmount = 0.0f;`
    *   Neu khong thiet lap Filled, hoat anh `DOFillAmount` se khong the render tren UGUI.
*   Dung SerializedObject va `SafeSetObjectReference` de wire dung `_levelText` va `_xpProgressBar` truoc khi goi `ApplyModifiedProperties()`, dam bao pre-check an toan de giu vung 0 git diff scene.

---

### Task 4: Coin Ticker & Centralized Reusable Coin Pool

**Files:**
- Modify: [InventoryHudWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/InventoryHudWidget.cs)
- Create: [CozyJuiceUtility.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozyJuiceUtility.cs)
- Modify: [ShopPopup.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/ShopPopup.cs)
- Modify: [CropWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CropWidget.cs)
- Modify: [AnimalWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/AnimalWidget.cs)
- Modify: [GameLifetimeScope.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs)
- Modify: [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)

**Step 1: Code CozyJuiceUtility.cs voi dich den bay va runtime warmup**
*   Component MonoBehaviour, co pool rieng tu quan ly dong xu:
```csharp
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace CozyLifeSim.UI
{
    public class CozyJuiceUtility : MonoBehaviour
    {
        [SerializeField] private RectTransform _coinTargetTransform;
        [SerializeField] private GameObject _coinPrefab;
        
        private readonly Queue<GameObject> _coinPool = new Queue<GameObject>();

        private void Start()
        {
            DOTween.SetTweensCapacity(500, 50);
            // Khoi tao prewarm pooling truoc o day de giam thieu GC churn
        }

        public void PlayCoinFlyAnimation(Vector3 startWorldPos, int coinsAmount)
        {
            if (_coinTargetTransform == null) return;
            Vector3 targetScreenPos = _coinTargetTransform.position;
            // Lay tu pool va thuc hien tween bay tu startWorldPos den targetScreenPos bang DOTween
        }

        private void OnDestroy()
        {
            // Clear pool de tranh memory leak hoac MissingReferenceException tren scene reload
            _coinPool.Clear();
        }
    }
}
```

**Step 2: Khoi chay runtime optional lookup cho cac Widget**
*   Cac components `ShopPopup.cs`, `CropWidget.cs`, `AnimalWidget.cs` se **khong nhan** `CozyJuiceUtility` qua constructor / ham Construct.
*   Thay vao do, chung se tuong tac an toan nhu sau:
```csharp
private CozyJuiceUtility _juiceUtility;

private void Start()
{
    if (_juiceUtility == null)
    {
        _juiceUtility = FindFirstObjectByType<CozyJuiceUtility>();
    }
}
```
*   Trong `CropWidget.cs`:
    *   Dang ky lang nghe su kien trong `Start()`:
        `_presenter.OnCropHarvested += PlayHarvestCoinFly;`
    *   Huy dang ky trong `OnDestroy()`:
        `_presenter.OnCropHarvested -= PlayHarvestCoinFly;`
    *   Trien khai:
        ```csharp
        private void PlayHarvestCoinFly(int rewardCoinsAmount)
        {
            if (_juiceUtility != null && _cropVisual != null)
            {
                _juiceUtility.PlayCoinFlyAnimation(_cropVisual.transform.position, rewardCoinsAmount);
            }
        }
        ```
*   Trong `AnimalWidget.cs`:
    *   Dang ky lang nghe su kien trong `Start()`:
        `_presenter.OnPetRewardGiven += PlayPetCoinFly;`
    *   Huy dang ky trong `OnDestroy()`:
        `_presenter.OnPetRewardGiven -= PlayPetCoinFly;`
    *   Trien khai:
        ```csharp
        private void PlayPetCoinFly(int rewardCoinsAmount)
        {
            if (_juiceUtility != null && _animalVisual != null)
            {
                _juiceUtility.PlayCoinFlyAnimation(_animalVisual.transform.position, rewardCoinsAmount);
            }
        }
        ```

**Step 3: Dang ky Component trong GameLifetimeScope & Auto Wire trong Setup Window**
*   Bo sung field serialized vao `GameLifetimeScope.cs`:
    `[SerializeField] private CozyJuiceUtility _juiceUtility;`
*   Trong Configure, thuc hien dang ky component voi null-guard va runtime fallback an toan:
```csharp
if (_juiceUtility != null)
{
    builder.RegisterComponent(_juiceUtility);
}
else
{
    var foundUtility = FindFirstObjectByType<CozyJuiceUtility>();
    if (foundUtility != null)
    {
        builder.RegisterComponent(foundUtility);
    }
    else
    {
        Debug.LogWarning("[CozySim] CozyJuiceUtility is missing from the scene!");
    }
}
```
*   Trong `CozySceneSetupWindow.cs`, setup de tu dong tim hoac tao `Cozy_Juice_Utility` GameObject duoi `Canvas/UI_Root`, tu dong tim `Coins_Text` va gan `RectTransform` cua no vao `_coinTargetTransform` cua `CozyJuiceUtility`, sau do wire lien ket truc tiep utility nay vao `_juiceUtility` cua `GameLifetimeScope` dung pre-check so sanh de giu git diff = 0.

**Step 4: Viet Coin Ticker trong InventoryHudWidget.cs**
*   Su dung `DOTween.To` an toan va **don dep tween (`_coinTween?.Kill()`) trong `OnDestroy()`** de tranh leak bo nho:
```csharp
private int _displayedCoins = -1;
private Tween _coinTween;

private void UpdateCoins(int value)
{
    if (_coinsText == null) return;
    if (_displayedCoins == -1)
    {
        _displayedCoins = value;
        _coinsText.text = $"Coins: {value}";
        return;
    }
    
    _coinTween?.Kill();
    _coinTween = DOTween.To(() => _displayedCoins, x => {
        _displayedCoins = x;
        _coinsText.text = $"Coins: {x}";
    }, value, 0.75f).SetEase(Ease.OutQuad).SetTarget(this);
}
```

---

## III. Verification Plan (Quy Trinh Kiem Thu & Tieu Chi Nghiem Thu)

### Kiem Thu Tu Dong (Automated Tests)
1.  **Test 0: Scene Silent Setup Idempotency**
    *   Chay setup silent scene 2 lan -> git diff Main.unity = 0.
2.  **Test 1: Juice & Ticker Play Mode Verification**
    *   Bo sung kiem thu Play Mode trong Validation Suite thuc te (Play Mode UI/Runtime validation):
    *   **Kiem tra Wiring logic trong test**:
        *   Assert gameobject chua component `CozyJuiceUtility` duoc setup va wire vao `GameLifetimeScope` khong bi null.
        *   Assert `_coinTargetTransform` duoc wire dung element `Coins_Text`.
        *   Assert component `ProgressionHudWidget` co truong `_levelText` va `_xpProgressBar` (Filled type) hop le.
        *   Assert tat ca cac UGUI Buttons trong scene can hieu ung co chua component `CozyButtonJuice`.
        *   Assert reference `ShopPopup`'s `_juiceUtility` hoat dong tot qua optional runtime lookup.
        *   Assert reference `CropWidget`'s va `AnimalWidget`'s `_juiceUtility` hoat dong tot qua optional runtime lookup.
    *   Gia lap cong XP tu `IProgressionService` va verify `ProgressionHudWidget` cap nhat dung thanh XP (khong bi fill > 1) va chu Level nay punching.
    *   Verify khi cong Coins tu `IInventoryService` thi Displayed Coins bat dau ticker.
    *   Verify khi xoa/disable gameobject thi toan bo subscription duoc clean, `_coinTween`/`_scaleTween` duoc kill hoan toan.
3.  **Test 2: Graceful Fallback Missing Utility**
    *   Gia lap moi truong khong co `CozyJuiceUtility` trong scene, thuc hien build container va runtime:
    *   Assert rang game van khoi dong thanh cong, scene setup/lifecycle hoat dong binh thuong ma khong bi break do thieu utility va khong gap bat ky exception DI nao.

### Kiem Thu Thu Cong (Manual Verification)
1.  Bam lien tuc vao cac tab shop hoac nut bam xem hieu ung click elastic nay nut co gian hoat dong muot ma va khong bi ket/de scale ke ca khi spam.
2.  Gieo hat, tuoi nuoc, thu hoach cay hoac pet meo, xac nhan cac dong xu bay muot ma tu vi tri cay/ga ve phia `Coins_Text` tren Header Coins, va so tien chu chay tang dan (Visual coin ticker) sinh dong.

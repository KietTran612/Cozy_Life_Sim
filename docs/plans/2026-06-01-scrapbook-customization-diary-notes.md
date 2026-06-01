# Scrapbook Polish & Custom Diary Notes Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` de execute this plan in single-flow mode.

**Goal:** Mo rong tinh nang Sổ Tay (Sticker Book) cho phep nguoi choi tuy bien chat lieu nen trang giay va dán cac mau nhat ky/ghi chu (Diary Sticky Notes) tu do canh cac sticker hoai niem cua minh.

**Architecture:** Luu tru toa do, noi dung nhat ky (dung NoteId GUID doc nhat), va phong cach nen trang (PageStyleData) duoi dang cac **C# serializable structs** trong `SaveData`. Viec dung struct dam bao list copy luc lay backup luon la **deep value copy**, phuc vu cho quy trinh atomic transactional rollback hoan hao. Toan bo data dong bo tai runtime qua `IMemoryService` va cap nhat len `StickerBookPage` nho co che restore/clear an toan, ho tro keo tha can doi vi tri snap-back khi loi, xoa bang double-click va input popup logic trim/max-60.

**Tech Stack:** UGUI RectTransform, SaveSystem Serialization, DOTween, VContainer DI.

---

## I. Yeu Cau Danh Gia Tu Nguoi Dung (User Review Required)

> [!IMPORTANT]
> **Tuong Thich Nguoc Cua Save File (Safe Backward Compatibility & Normalization):**
> *   Bo sung `PlacedDiaryNotes` va `PageStyles` vao `SaveData.cs`.
> *   Phai thuc hien khoi tao va san hanh du lieu (Data Sanitization/Normalization) trong `SaveService.cs` khi load game:
>   *   Neu hai danh sach bi `null` thi tu dong khoi tao List rong moi.
>   *   Quet toan bo `PlacedDiaryNotes` de tu dong backfill GUID cho note nao co `NoteId` rong hoac bi trung lap.
>   *   Nen giay trung lap: Thuc hien compact thu quan list `PageStyles` bang cach chi giu lai record moi nhat cho moi `PageIndex` (dam bao moi trang chi co duy nhat 1 record style).
> 
> **Tu Trien Khai An Toan Cua Service APIs (Self-Healing & Guard):**
> *   API `UpdateDiaryNotePositionNonSaving` va `RemoveDiaryNoteNonSaving` se chu dong guard kiem tra `if (string.IsNullOrEmpty(noteId))` va tra ve `false` ngay lap tuc de bao ve an toan.
> *   API `AddDiaryNoteNonSaving` se chu dong kiem tra neu `noteId` truyen vao bi rong hoac da ton tai trung lap trong danh sach `PlacedDiaryNotes`, no se **tu dong sinh ra GUID doc nhat moi** (`System.Guid.NewGuid().ToString()`) tren RAM de tu chu tu chua (self-healing) thay vi chi dua dam vao `SaveService` luc load file.
> 
> **Trien Khai C# Struct Cho Deep Value Copy Backup:**
> *   `DiaryNotePlacedData` va `PageStyleData` phai duoc trien khai duoi dang **struct** (giong nhu `StickerPlacedData`).
> *   Khi duoc thiet ke la value type (struct), cac lenh clone list kieu `new List<T>(source)` se thuc hien **deep copy triet de tung phan tu**. Thay doi gia tri tren object moi se khong bao gio bi lay lan sang vung nho backup cu, dam bao rollback RAM hoat dong chinh xac tuyet doi.
> 
> **Kien Truc Giao Dich An Toan (Atomic Transaction & Visual Snap-back):**
> *   De tranh desync (RAM/Save gap loi khi ghi dia, nhung UI va vi tri note van bi thay doi/de lech):
>   *   Khi nguoi choi kéo note, `CozyDiaryNote.cs` se luu lai vi tri cu `oldPosition`.
>   *   Khi tha chuot (End Drag), goi transaction `TryUpdateDiaryNotePosition` cua Presenter.
>   *   Presenter chup anh RAM cu, chay thay doi non-saving, goi `Save()`.
>   *   Neu Save that bai: Presenter rollback RAM ve cu, tra ve `false`. UI cua `CozyDiaryNote` se tu dong **snap-back (di chuyen dan hoi bang DOTween) tro ve vi tri `oldPosition` ban dau** de bao toan tinh dong bo visual va data.
>   *   **DOTween Snap-back Lifecycle Clean**: Luan chuyen tween snap-back phai duoc luu tru trong bien rieng `_snapBackTween` tren `CozyDiaryNote`. Thuc hien chu dong `.Kill()` tween nay khi nguoi choi **Begin Drag moi** va trong ham `OnDestroy()`/`OnDisable()` de tranh tuyet doi leak hoac loi ghi đè vi tri transform.
>   *   **Gia tri keo tha theo Canvas Scale Factor**: Trong `CozyDiaryNote.OnDrag()`, toa do keo phai duoc chia cho `Canvas.scaleFactor` cua canvas cha de dam bao toc do keo tha dong nhat, chinh xac tren moi man hinh co do phan giai khac nhau.
> 
> **Tuong Tac Xoa Note Bang Double-Click & Unidirectional Event Flow:**
> *   Nguoi choi co the xoa note dán truc tiep tren trang giay bang cach **Double-Click** vao note do. 
> *   Khi double-click xay ra, widget goi presenter `TryRemoveDiaryNote(NoteId)`.
> *   De tranh loi NullReferenceException tai cho duyet xoa: `StickerBookPage` se dung `TryGetComponent(out CozyDiaryNote note)` va so sanh an toan, tranh dung truc tiep `GetComponent` khong kiem check null.
> *   De dam bao luong luu chuyen thong tin mot chieu (Unidirectional Event Flow): Note se **khong tu dong Destroy tren UI**. Thay vao do, `StickerBookPage` se dang ky su kien `OnDiaryNoteRemoved` tu presenter. Khi transaction hoan thanh va save thanh cong, page se tim GameObject cua note tuong ung, chay hoat anh thu nho DOTween roi moi Destroy, giu vung dong bo data va visual tuyet doi.
> 
> **Expose Style Count Tu StickerBookPage de Clamped Page Styles:**
> *   Tranh viec StickerBook tu y phan bo tham chieu sprite khien sai lech ownership:
>   *   `StickerBookPage` se truc tiep expose mot property read-only:
>       `public int StyleCount => _backgroundStyles != null ? _backgroundStyles.Length : 0;`
>   *   `StickerBook.cs` se truy van so luong style nay tu trang sach hien hanh: `int maxStyleCount = currentPage.StyleCount;`.
>   *   Presenter `TrySetPageStyle` se clamp trong pham vi `[0, maxStyleCount - 1]` truoc khi goi service non-saving de dam bao tính dong goi và phan chia ownership thiet ke sach se.

---

## II. Chi Tiet Cac Thay Doi De Xuat (Proposed Changes)

### Task 1: Bo sung struct va API vao SaveData, IMemoryService

**Files:**
- Modify: [SaveData.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/SaveData.cs)
- Modify: [SaveService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/SaveService.cs)
- Modify: [IMemoryService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/IMemoryService.cs)
- Modify: [MemoryService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/MemoryService.cs)

**Step 1: Cap nhat structs trong SaveData.cs**
*   Dinh nghia structure Diary Note va Page Style kieu struct de dam bao deep copy value:
```csharp
[System.Serializable]
public struct DiaryNotePlacedData
{
    public string NoteId;
    public string NoteText;
    public float PositionX;
    public float PositionY;
    public int PageIndex;
}

[System.Serializable]
public struct PageStyleData
{
    public int PageIndex;
    public int BackgroundStyleIndex;
}
```
*   Them cac danh sach vao `SaveData`: `PlacedDiaryNotes` va `PageStyles`.
*   Gieo logic null check va san hanh data (GUID, page compact) tai `SaveService.cs`.

**Step 2: Cap nhat IMemoryService.cs va MemoryService.cs**
*   API update va remove bat buoc phai **tra ve bool** de Presenter nhan biet chinh xac noteId co hop le hay khong truoc khi thuc hien ghi file Save:
```csharp
// Trong IMemoryService.cs
IReadOnlyList<DiaryNotePlacedData> PlacedDiaryNotes { get; }
IReadOnlyList<PageStyleData> PageStyles { get; }

DiaryNotePlacedData AddDiaryNoteNonSaving(string noteId, string text, float x, float y, int pageIndex);
bool UpdateDiaryNotePositionNonSaving(string noteId, float x, float y);
bool RemoveDiaryNoteNonSaving(string noteId);
void SetPageStyleNonSaving(int pageIndex, int styleIndex);
void RestoreMemoryStateNonSaving(List<DiaryNotePlacedData> notes, List<PageStyleData> styles);
```
*   Trien khai Upsert set page style va kiem tra cap nhat vi tri tra ve dung/sai trong `MemoryService.cs`:
```csharp
public DiaryNotePlacedData AddDiaryNoteNonSaving(string noteId, string text, float x, float y, int pageIndex)
{
    // Self-healing: Tu dong gieo GUID neu truyen vao rong hoac trung lap
    string resolvedId = string.IsNullOrEmpty(noteId) || _saveService.ActiveSave.PlacedDiaryNotes.Exists(n => n.NoteId == noteId) 
        ? System.Guid.NewGuid().ToString() 
        : noteId;

    DiaryNotePlacedData note = new DiaryNotePlacedData
    {
        NoteId = resolvedId,
        NoteText = text,
        PositionX = x,
        PositionY = y,
        PageIndex = pageIndex
    };
    
    _saveService.ActiveSave.PlacedDiaryNotes.Add(note);
    return note;
}

public bool UpdateDiaryNotePositionNonSaving(string noteId, float x, float y)
{
    if (string.IsNullOrEmpty(noteId)) return false;
    
    int index = _saveService.ActiveSave.PlacedDiaryNotes.FindIndex(n => n.NoteId == noteId);
    if (index < 0) return false;
    
    DiaryNotePlacedData note = _saveService.ActiveSave.PlacedDiaryNotes[index];
    note.PositionX = x;
    note.PositionY = y;
    _saveService.ActiveSave.PlacedDiaryNotes[index] = note;
    return true;
}

public bool RemoveDiaryNoteNonSaving(string noteId)
{
    if (string.IsNullOrEmpty(noteId)) return false;
    
    int count = _saveService.ActiveSave.PlacedDiaryNotes.RemoveAll(n => n.NoteId == noteId);
    return count > 0;
}
```

---

### Task 2: Nang cap StickerBookPresenter thuc hien Giao dich An toan

**Files:**
- Modify: [StickerBookPresenter.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Presenters/StickerBookPresenter.cs)

**Step 1: Code logic Transactional trong Presenter**
*   Khai bao các events cho UI:
    *   `public event Action<DiaryNotePlacedData> OnDiaryNoteAdded;`
    *   `public event Action<string, float, float> OnDiaryNotePositionUpdated;`
    *   `public event Action<string> OnDiaryNoteRemoved;`
    *   `public event Action<int, int> OnPageStyleChanged;`
*   Trien khai atomic transactions ho tro clamp style index truoc khi gui xuong Service layer:
```csharp
public bool TrySetPageStyle(int pageIndex, int styleIndex, int maxStyleCount)
{
    if (maxStyleCount <= 0) return false;
    int clampedStyle = Mathf.Clamp(styleIndex, 0, maxStyleCount - 1);
    
    var backupNotes = new List<DiaryNotePlacedData>(_memoryService.PlacedDiaryNotes);
    var backupStyles = new List<PageStyleData>(_memoryService.PageStyles);

    _memoryService.SetPageStyleNonSaving(pageIndex, clampedStyle);

    try
    {
        _saveService.Save();
        OnPageStyleChanged?.Invoke(pageIndex, clampedStyle);
        return true;
    }
    catch (System.Exception ex)
    {
        _memoryService.RestoreMemoryStateNonSaving(backupNotes, backupStyles);
        UnityEngine.Debug.LogWarning($"[CozySim Book] Save PageStyle failed. Rolled back RAM. Exception: {ex.Message}");
        return false;
    }
}
```

---

### Task 3: CozyDiaryInputPopup - Dialog go chu go trim, validation & targeted listener cleanup

**Files:**
- Create: [CozyDiaryInputPopup.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozyDiaryInputPopup.cs)
- Modify: [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)
- Modify: [GameLifetimeScope.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs)

**Step 1: Code CozyDialogueInputPopup.cs voi clean callback & targeted listener**
*   Luu giu reference callback: `private System.Action<string> _onConfirmCallback;`.
*   Trong `Start()` dang ky targeted listener rieng biet de tranh can thiep vao cac setup khac trong scene:
    `_confirmButton.onClick.AddListener(HandleConfirmClicked);`
    `_cancelButton.onClick.AddListener(HandleCancelClicked);`
*   Trong `OpenPopup(System.Action<string> onConfirm)`: gan callback, lam sach input field va set active content panel.
*   **Cleanup an toan**: Trong `ClosePopup()` va `OnDestroy()`, luon dat lai `_onConfirmCallback = null;` va thuc hien go targeted listener tuong thich:
    `_confirmButton.onClick.RemoveListener(HandleConfirmClicked);`
    `_cancelButton.onClick.RemoveListener(HandleCancelClicked);`
    Tranh tuyet doi viec su dung `RemoveAllListeners()` gay anh huong den he thong editor setup khac.

---

### Task 4: UI CozyDiaryNote & Update StickerBook / StickerBookPage

**Files:**
- Create: [CozyDiaryNote.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozyDiaryNote.cs)
- Modify: [StickerBookPage.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/StickerBookPage.cs)
- Modify: [StickerBook.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/StickerBook.cs)
- Modify: [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)

**Step 1: Code CozyDiaryNote cho phep Keo Tha, Drag Tween Lifecycle va Double-Click**
```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

namespace CozyLifeSim.UI
{
    public class CozyDiaryNote : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI _text;
        
        private string _noteId;
        private RectTransform _rectTransform;
        private Vector2 _oldAnchoredPos;
        private StickerBookPresenter _presenter;
        private Canvas _parentCanvas;
        private Tween _snapBackTween;

        public void Setup(string noteId, string text, StickerBookPresenter presenter)
        {
            _noteId = noteId;
            _text.text = text;
            _presenter = presenter;
            _rectTransform = transform as RectTransform;
            _parentCanvas = GetComponentInParent<Canvas>();
        }

        public string NoteId => _noteId;

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Kill cu truoc khi drag moi de dam bao an toan lifecycle cho transform
            _snapBackTween?.Kill();
            _oldAnchoredPos = _rectTransform.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            float scale = _parentCanvas != null ? _parentCanvas.scaleFactor : 1f;
            _rectTransform.anchoredPosition += eventData.delta / scale;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_presenter == null) return;
            
            bool success = _presenter.TryUpdateDiaryNotePosition(_noteId, _rectTransform.anchoredPosition.x, _rectTransform.anchoredPosition.y);
            if (!success)
            {
                _snapBackTween?.Kill();
                _snapBackTween = _rectTransform.DOAnchorPos(_oldAnchoredPos, 0.4f).SetEase(Ease.OutBack);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 2 && _presenter != null)
            {
                _presenter.TryRemoveDiaryNote(_noteId);
            }
        }

        private void OnDisable()
        {
            _snapBackTween?.Kill();
        }

        private void OnDestroy()
        {
            _snapBackTween?.Kill();
        }
    }
}
```

**Step 2: Nang cap StickerBookPage de lang nghe event va xoa visual an toan**
*   Luu giu list spawned: `List<GameObject> _spawnedDiaryNotes`.
*   Subscribe `StickerBookPresenter.OnDiaryNoteRemoved` va xoa visual tuong ung voi TryGetComponent an toan:
```csharp
private void HandleDiaryNoteRemoved(string noteId)
{
    int index = _spawnedDiaryNotes.FindIndex(x => x != null && x.TryGetComponent(out CozyDiaryNote note) && note.NoteId == noteId);
    if (index >= 0)
    {
        GameObject go = _spawnedDiaryNotes[index];
        _spawnedDiaryNotes.RemoveAt(index);
        
        go.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
            Destroy(go);
        });
    }
}
```
*   Expose property doc hop le:
    `public int StyleCount => _backgroundStyles != null ? _backgroundStyles.Length : 0;`

**Step 3: Nang cap StickerBook.cs de dung Nut Bam & Wire an toan**
*   StickerBook se khai bao serialized:
    *   `[SerializeField] private Button _changeStyleButton;`
    *   `[SerializeField] private Button _addDiaryNoteButton;`
    *   `[SerializeField] private CozyDiaryInputPopup _diaryInputPopup;`
    *   `[SerializeField] private CozyDiaryNote _diaryNotePrefabTemplate;`
*   Khi nut bam Doi Nen duoc click, StickerBook se truy van `int maxStyleCount = currentPage.StyleCount;` tu trang hien hanh va goi presenter: `_presenter.TrySetPageStyle(pageIndex, nextStyleIndex, maxStyleCount)` bao toan owner thiet ke tuyet doi.

---

## III. Verification Plan (Quy Trinh Kiem Thu & Tieu Chi Nghiem Thu)

### Kiem Thu Tu Dong (Automated Tests)
1.  **Test 0: Scrapbook Polish Logic Validation Suite**
    *   **Backward Compatibility & Sanitization Test**: Luu Save file rong/cu, kiem tra load game len cac List PlacedDiaryNotes va PageStyles van duoc tu dong khoi tao hop le. Backfill GUID cho note rong/trung lap tu dong trong service, compact loc trùng PageStyles theo Index trang ma khong xay ra crash.
    *   **Note ID Uniqueness Test**: Add 3 notes lien tiep, assert tat ca NoteId GUID sinh ra deu khac nhau, khong co ma nao trung lap.
    *   **Update Position & Style Persistence**: Thay doi vi tri cua note 1 va doi nen trang 0 sang style 2, Save game va load lai, kiem tra toa do va style nen trang giay van duoc khoi phuc hoan hao (ke ca khi page khong co note nao).
    *   **Atomic Save failure rollback**: Gia lap save loi, verify rang RAM cua `MemoryService` tu dong rollback nguyen trang nhu cu, va UI thuc hien snap-back ve vi tri cu dung toa do co dọn dẹp tween an toan.
2.  **Test 1: Idempotency Verification**
    *   Chay setup silent lan 2 Main.unity van 0 git diffs, assert toan bo references cua `CozyDiaryInputPopup`, `CozyDiaryNote` prefab template, cac nut bam `_changeStyleButton`, `_addDiaryNoteButton` tren `StickerBook` deu khop hoan toan.

### Kiem Thu Thu Cong (Manual Verification)
1.  Mo Sổ Tay, bam "Đổi Nền": Xac nhan trang giay doi tu o ly sang giay co dien muot ma.
2.  Bam "Viết Nhật Ký", go "Mot ngay he nang vang ve tham Ngoai", sau do keo tha giay ghi chu đặt ben canh Sticker Banh Mi. Kiem tra toc do keo tha luon chuan xac bat ke Canvas scaler thay doi ra sao.
3.  Double-click giay ghi chu: xac nhan no bien mat khoi trang sach sau hoat anh thu nho va vi tri duoc xoa sach khoi file save.

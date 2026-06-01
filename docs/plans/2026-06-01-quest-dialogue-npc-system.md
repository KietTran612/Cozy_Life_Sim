# Expanded Quests & Dialogue NPC System Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Thoi hon am ap vao cac hoat dong trong game bang cach bo sung he thong doi thoai NPC ke truyen co hieu ung typewriter dan xen khi lam nhiem vu, gia tang chieu sau cho di san Viet Nam.

**Architecture:** Xay dung mot Popup thong tin chuyen dung `CozyDialoguePopup` o Presentation layer, tu dong dang ky su kien `OnQuestCompleted` cua Quest Service de trigger dialogue ke truyen sau khi nhiem vu hoan thanh (giu vung tinh dong bo va nguyen tu hoa atomicity cua logic thuong/save). Cac tuong tac click tren scene cua nguoi choi tren NPC se duoc dieu phoi rieng qua co che runtime lookup an toan de hien thi bong bong thoai.

**Tech Stack:** Unity UGUI, UniTask, VContainer DI.

---

## I. Yeu Cau Danh Gia Tu Nguoi Dung (User Review Required)

> [!IMPORTANT]
> **Bao Toan Tinh Nguyen Tu (Atomicity) Cua Quest Flow:**
> *   Khong can thiep vao quy trinh complete va phat thuong trong `QuestService.TryProgressQuest()`. Thuong coin/XP va Save luon duoc thuc hien nguyen tu va dong bo o Service layer de dam bao tinh nang rollback khi loi.
> *   Widget se khong tu y hoan hoan complete. Thay vao do, chung ta se de `CozyDialoguePopup` tu dong dang ky su kien `IQuestService.OnQuestCompleted`. Khi nhiem vu **da hoan thanh va luu thanh cong**, dialogue se duoc kich hoat bat len ngay sau do nhu mot phan thuong narrative.
> 
> **VContainer Dependency Registration & Wiring Cho Dialogue Popup:**
> *   Khong dung static Singleton tinh cuc bo de tranh memory leak khi reload scene.
> *   `CozyDialoguePopup` se duoc attach duoi Canvas Popup vao scene, khai bao serialized field `_dialoguePopup` trong `GameLifetimeScope.cs` va dang ky qua `builder.RegisterComponent(_dialoguePopup)`.
> *   De dam bao an toan DI graph va tranh crack build container neu scene field chua duoc gieo (wire), `GameLifetimeScope.Configure` se co co che null-guard va runtime lookup fallback giong voi utility.
> *   Trong `CozySceneSetupWindow.cs`, setup window se tu dong tao va wire `CozyDialoguePopup` vao `GameLifetimeScope` idempotent.
> *   Cac doi tuong click ngoai world nhu `CozyNPCWidget` se dung runtime lookup `FindFirstObjectByType<CozyDialoguePopup>()` de lay tham chieu an toan ma khong bi crash DI khi chay validation test co lap thieu popup.
> 
> **Quy Tac An Toan Typewriter & Reentrancy Control (UniTask Cancellation & Skip):**
> *   Tranh loi khong dang ky su kien khi Popup o trang thai Inactive: Vi popup thuong bi ẩn di khi khoi dong, neu dang ky su kien trong `Start()` hoac `Awake()`, cac ham nay se khong chay khien cho event `OnQuestCompleted` khong the trigger. Do do, chung ta se **dang ky su kien OnQuestCompleted ngay trong ham `Construct` cua VContainer** (VContainer luon chay Construct tren ca cac object inactive). 
> *   **Chong Double-Subscribe trong Construct**: De phong truong hop runtime hoac unit test thuc hien re-inject khien `Construct` chay nhieu lan, logic dang ky su kien bat buoc phai thuc hien go dang ky truoc khi dang ky moi (`_questService.OnQuestCompleted -= HandleQuestCompleted;` truoc khi `+=`).
> *   **Bao Toan State Parent luon Active**: Do FindFirstObjectByType khong tim cac object inactive, parent GameObject `CozyDialoguePopup` trong hierarchy phai **luon o trang thai Active**. Hieu ung an popup chi thuc hien bang cach toggle `_contentPanel` ben trong. Setup window se co validation assert de kiem tra tinh trang activeSelf cua dialogue popup.
> *   Typewriter effect su dung `CancellationTokenSource` duoc lien ket truc tiep (Linked CTS) voi `this.GetCancellationTokenOnDestroy()` de giai phong triet de CPU/RAM khi GameObject bi disable/destroy giua chung.
> *   **Chong Tranh Chap Reentrancy Bang Version Guard**: Khi co dialogue moi goi den don dap, dialogue sau se tang so phien ban `_currentDialogueVersion` de vo hieu hoa cac tac vu chay ngam phien ban cu. Truoc moi ky tu chay va o block cleanup, chung ta so sanh `version == _currentDialogueVersion` de ngan chan tuyet doi viec phien ban cu ghi de van ban phien ban moi hoac set sai co `_isTyping`.
> *   **Tuan Thu Phien Ban Khi Nguoi Dung Skip**: Trong ham `SkipOrNext()`, khi user bam bo qua luc dang chay chu, chúng ta se chu dong tang so phien ban `_currentDialogueVersion++` giong nhu khi start dialogue moi de lam mat hieu luc hoan toan cua tac vu async cu, dong thoi gan luon full text va tat co typing de dat do an toan bang mat thoi gian tuyet doi.
> 
> **Data-driven Dialogue Mapping bang QuestId va NPC Data:**
> *   Khong kiem tra quest hoan thanh bang chuoi string Title de tranh brittle/de gay loi khi doi ten quest.
> *   Thiet ke serializable class **`QuestDialogueMapping` lam nested class ngay ben trong `CozyDialoguePopup.cs`** de gom nhom gon gang, tranh tao phat sinh meta files thua o ngoai.
> *   NPC tren scene dung script `CozyNPCWidget.cs` chua truc tiep serialized array cac doan thoai cozy am ap thong qua wrapper class **`NpcDialogueLine` duoc khai bao nested ngay ben trong `CozyNPCWidget.cs`** de tao ra TextArea (3, 5) bat mat va phu hop nhat trong Inspector cua Unity.

---

## II. Chi Tiet Cac Thay Doi De Xuat (Proposed Changes)

### Task 1: CozyDialoguePopup - Khung Thoai Am Ap Voi Lifecycle & Safe CTS

**Files:**
- Create: [CozyDialoguePopup.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozyDialoguePopup.cs)
- Modify: [GameLifetimeScope.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs)
- Modify: [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)

**Step 1: Code CozyDialoguePopup**
*   Tạo component `CozyDialoguePopup` chua nested class `QuestDialogueMapping` ngay ben trong de dong goi du lieu sach se.
*   Inject `IQuestService` trong `Construct()`, dam bao chan double-subscribe bằng cach luon `-=` truoc khi `+=`.
*   Su dung UniTask de viet typewriter effect ho tro Skip, phien ban hoa `version guard` va hoan toan safe lifecycle:
```csharp
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using VContainer;
using CozyLifeSim.Core;
using TMPro;

namespace CozyLifeSim.UI
{
    public class CozyDialoguePopup : MonoBehaviour
    {
        [System.Serializable]
        public class QuestDialogueMapping
        {
            public int QuestId;
            public string NpcName;
            [TextArea(3, 5)] public string DialogueText;
            public Sprite Portrait;
        }

        [SerializeField] private RectTransform _contentPanel;
        [SerializeField] private Image _portrait;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private Button _nextButton;
        
        [SerializeField] private List<QuestDialogueMapping> _questDialogues = new List<QuestDialogueMapping>();

        private IQuestService _questService;
        private CancellationTokenSource _dialogueCts;
        private bool _isTyping = false;
        private string _fullText = "";
        private int _currentDialogueVersion = 0;

        [Inject]
        public void Construct(IQuestService questService)
        {
            if (_questService != null)
            {
                // Unsubscribe de phong ngua double-subscription neu bi re-inject luc chay test
                _questService.OnQuestCompleted -= HandleQuestCompleted;
            }

            _questService = questService;

            if (_questService != null)
            {
                // Dang ky ngay trong Construct de dam bao luon lang nghe ke ca khi popup bat dau o trang thai inactive
                _questService.OnQuestCompleted += HandleQuestCompleted;
            }
        }

        private void Start()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(SkipOrNext);
            }
            
            if (_contentPanel != null)
            {
                _contentPanel.gameObject.SetActive(false);
            }
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            var mapping = _questDialogues.Find(x => x.QuestId == quest.QuestId);
            if (mapping != null)
            {
                ShowDialogue(mapping.NpcName, mapping.DialogueText, mapping.Portrait).Forget();
            }
        }

        public async UniTask ShowDialogue(string npcName, string text, Sprite portrait)
        {
            if (_contentPanel != null)
            {
                _contentPanel.gameObject.SetActive(true);
            }
            
            _isTyping = true;
            _fullText = text;
            _nameText.text = npcName;
            
            if (_portrait != null)
            {
                _portrait.gameObject.SetActive(portrait != null);
                _portrait.sprite = portrait;
            }
            
            _dialogueText.text = "";
            
            // Reentrancy Version Guard: tang so phien ban de vo hieu hoa cac luong chay cu immediately
            int version = ++_currentDialogueVersion;
            
            // Cancel luong chay truoc do de gia lap text moi immediately.
            // Khong goi Dispose o day de tranh ObjectDisposedException khi delay task cu dang unwinding.
            if (_dialogueCts != null)
            {
                _dialogueCts.Cancel();
            }
            
            // Linked CTS voi destroy token cua object de bao ve lifecycle
            _dialogueCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            var token = _dialogueCts.Token;

            try
            {
                for (int i = 0; i <= text.Length; i++)
                {
                    token.ThrowIfCancellationRequested();
                    
                    // Neu da co phien ban moi hon tuong tac chiem quyen, thoat ngay lap tuc
                    if (version != _currentDialogueVersion) return;
                    
                    _dialogueText.text = text.Substring(0, i);
                    await UniTask.Delay(30, cancellationToken: token);
                }
            }
            catch (System.OperationCanceledException)
            {
                // Tra ve ngay lap tuc neu bi cancel, khong ghi de text cu (Race Condition Guard)
                return;
            }
            
            // Chi phien ban moi nhat moi duoc phep hoan thanh hien thi chu
            if (version == _currentDialogueVersion)
            {
                _dialogueText.text = text;
                _isTyping = false;
            }
        }

        public void SkipOrNext()
        {
            if (_isTyping)
            {
                // Tang so phien ban tai day de bao ve phien ban async dang unwinding khong ghi lung tung
                _currentDialogueVersion++;
                
                _dialogueCts?.Cancel();
                _dialogueText.text = _fullText;
                _isTyping = false;
            }
            else
            {
                if (_contentPanel != null)
                {
                    _contentPanel.gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (_questService != null)
            {
                _questService.OnQuestCompleted -= HandleQuestCompleted;
            }
            
            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveListener(SkipOrNext);
            }

            _dialogueCts?.Cancel();
            _dialogueCts?.Dispose();
        }
    }
}
```

**Step 2: Dang ky DI va Auto setup trong GameLifetimeScope**
*   Trong `GameLifetimeScope.cs`, them `[SerializeField] private CozyDialoguePopup _dialoguePopup;` va dang ky `builder.RegisterComponent(_dialoguePopup);` co null guard va fallback tuong tu utility.
```csharp
if (_dialoguePopup != null)
{
    builder.RegisterComponent(_dialoguePopup);
}
else
{
    var foundPopup = FindFirstObjectByType<CozyDialoguePopup>();
    if (foundPopup != null)
    {
        builder.RegisterComponent(foundPopup);
    }
    else
    {
        Debug.LogWarning("[CozySim] CozyDialoguePopup is missing from the scene!");
    }
}
```
*   Trong [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs), setup window se tu dong instantiate template `CozyDialoguePopup` nam ben duoi canvas vung Popup, va thiet lap parent luon luon Active (`dialoguePopup.gameObject.SetActive(true);`), chi wire an `_contentPanel` ben trong idempotent.

---

### Task 2: Interactive NPC Clicks in Scene Voi Data-driven & Pointer Check

**Files:**
- Create: [CozyNPCWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozyNPCWidget.cs)
- Modify: [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozySceneSetupWindow.cs)

**Step 1: Code CozyNPCWidget**
*   Widget nay ke thua MonoBehaviour, co he thong Collider2D va bat click chuot ngoai world.
*   Dinh nghia structure cho list ngau nhien co hop `[TextArea(3, 5)]` nested ngay ben trong `CozyNPCWidget` de lam Inspector gon gang sach se:
```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace CozyLifeSim.UI
{
    [RequireComponent(typeof(Collider2D))]
    public class CozyNPCWidget : MonoBehaviour
    {
        [System.Serializable]
        public class NpcDialogueLine
        {
            [TextArea(3, 5)] public string Line;
        }

        [System.Serializable]
        public class NpcDialogueData
        {
            public string NpcName;
            public List<NpcDialogueLine> Dialogues = new List<NpcDialogueLine>();
            public Sprite Portrait;
        }

        [SerializeField] private NpcDialogueData _npcData;
        private CozyDialoguePopup _dialoguePopup;

        private void Start()
        {
            if (_dialoguePopup == null)
            {
                _dialoguePopup = FindFirstObjectByType<CozyDialoguePopup>();
            }
        }

        private void OnMouseDown()
        {
            // Pointer Guard: block click neu chuot dang đè len UI (vi du shop button, close button)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (_dialoguePopup != null && _npcData.Dialogues != null && _npcData.Dialogues.Count > 0)
            {
                int randomIndex = Random.Range(0, _npcData.Dialogues.Count);
                string randomDialogue = _npcData.Dialogues[randomIndex].Line;
                _dialoguePopup.ShowDialogue(_npcData.NpcName, randomDialogue, _npcData.Portrait).Forget();
            }
        }
    }
}
```

**Step 2: Sinh NPC trong scene setup**
*   `CozySceneSetupWindow.cs` se tu dong sinh ra GameObject `NPC_BaNgoai` dung sprite chicken hoac sprite default co san trong pen, gan `CozyNPCWidget` va boot mot so cau thoai am ap mac dinh ve que huong.

---

## III. Verification Plan (Quy Trinh Kiem Thu & Tieu Chi Nghiem Thu)

### Kiem Thu Tu Dong (Automated Tests)
1.  **Test 0: Dialogue Compilation Check**
    *   Verify toan bo ma nguon compile PASS 100%.
2.  **Test 1: Idempotency Verification**
    *   Chay setup silent lan 2 verify Main.unity van 0 git diffs.
3.  **Test 2: Play Mode Runtime Dialogue Test**
    *   Gia lap mot Quest hoan thanh -> xac nhan dialogue tu dong bat len neu co mapping QuestId, ngay ca khi content panel dang inactive.
    *   Assert activeSelf cua parent CozyDialoguePopup GameObject:
        `Assert.IsTrue(dialoguePopup.gameObject.activeSelf, "CozyDialoguePopup GameObject must always be active in the scene hierarchy!");`
    *   Gia lap type đang chay -> test skip click bat full text ngay.
    *   Gia lap destroy object giua typewriter -> assert khong xay ra bat ky runtime error hay UniTask leak nao nho Linked CTS.
    *   Gia lap pointer de tren UI khi click NPC -> assert NPC click khong bi trigger.
    *   **DI Graph Null Fallback Validation**: Assert thi thieu `_dialoguePopup` duoc wire tren `GameLifetimeScope`, qua trinh build DI container cua scene van dien ra thanh cong tot dep, khong bi crash DI Graph.
    *   **NPC Runtime Lookup Fallback Validation**: Assert thieu component `CozyDialoguePopup` trong scene, click NPC van duoc bo qua mot cach em dep chu khong crash game.

### Kiem Thu Thu Cong (Manual Verification)
1.  Bấm vào NPC hiên nhà: bong bóng hội thoại chữ chạy typewriter. Click tiếp thì bỏ qua hiệu ứng chữ chạy và hiện đầy đủ chữ ngay.
2.  Hoàn thành nhiệm vụ di sản: Dialogue câu chuyện tương ứng được kích hoạt ngay lập tức sau khi hoàn thành.

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
            if (_dialogueCts != null)
            {
                _dialogueCts.Cancel();
            }

            // Keep a local reference to link the CTS and ensure it's disposed cleanly in finally
            var localCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _dialogueCts = localCts;
            var token = localCts.Token;

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

                // Chi phien ban moi nhat moi duoc phep hoan thanh hien thi chu
                if (version == _currentDialogueVersion)
                {
                    _dialogueText.text = text;
                    _isTyping = false;
                }
            }
            catch (System.OperationCanceledException)
            {
                // Tra ve ngay lap tuc neu bi cancel, khong ghi de text cu (Race Condition Guard)
                return;
            }
            finally
            {
                // Clean up CTS safely without causing leaks or ObjectDisposedException on active awaits
                if (_dialogueCts == localCts)
                {
                    _dialogueCts = null;
                }
                localCts.Cancel();
                localCts.Dispose();
            }
        }

        public void SkipOrNext()
        {
            if (_isTyping)
            {
                // Tang so phien ban tai day de bao ve phien ban async dang unwinding khong ghi lung tung
                _currentDialogueVersion++;

                if (_dialogueCts != null)
                {
                    _dialogueCts.Cancel();
                }
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

            if (_dialogueCts != null)
            {
                _dialogueCts.Cancel();
                _dialogueCts = null;
            }
        }
    }
}

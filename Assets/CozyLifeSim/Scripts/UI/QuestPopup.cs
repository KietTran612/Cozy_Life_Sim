using UnityEngine;
using TMPro;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI
{
    public class QuestPopup : CozyPopup
    {
        [Header("Quest Specifics")]
        [SerializeField] private TextMeshProUGUI _questItemTemplate;

        private IQuestService _questService;
        private bool _isSubscribed;
        private readonly List<TextMeshProUGUI> _spawnedTexts = new List<TextMeshProUGUI>();

        [Inject]
        public void Construct(IQuestService questService)
        {
            if (_isSubscribed && _questService != null)
            {
                _questService.OnQuestProgressed -= OnQuestChanged;
                _questService.OnQuestCompleted -= OnQuestChanged;
                _questService.OnQuestsReloaded -= OnQuestsReloadedHandler;
                _isSubscribed = false;
            }

            _questService = questService;

            if (_questService != null)
            {
                _questService.OnQuestProgressed += OnQuestChanged;
                _questService.OnQuestCompleted += OnQuestChanged;
                _questService.OnQuestsReloaded += OnQuestsReloadedHandler;
                _isSubscribed = true;
            }
        }

        protected override void Start()
        {
            base.Start();

            if (_questItemTemplate != null)
            {
                _questItemTemplate.gameObject.SetActive(false); // Hide template initially
            }

            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }

            RefreshQuests();
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            RefreshQuests();
        }

        private void OnQuestChanged(QuestData quest)
        {
            RefreshQuests();
        }

        private void OnQuestsReloadedHandler()
        {
            RefreshQuests();
        }

        private void RefreshQuests()
        {
            if (_questService == null || _questItemTemplate == null) return;

            var activeQuests = _questService.ActiveQuests;

            // Spawn enough texts for all active quests
            while (_spawnedTexts.Count < activeQuests.Count)
            {
                var newText = Instantiate(_questItemTemplate, _questItemTemplate.transform.parent);
                newText.gameObject.SetActive(true);
                _spawnedTexts.Add(newText);
            }

            // Update text labels and colors
            for (int i = 0; i < _spawnedTexts.Count; i++)
            {
                if (i < activeQuests.Count)
                {
                    var quest = activeQuests[i];
                    _spawnedTexts[i].gameObject.SetActive(true);

                    if (quest.IsCompleted)
                    {
                        _spawnedTexts[i].text = $"<s>- {quest.Title} (Completed!)</s>";
                        _spawnedTexts[i].color = Color.gray;
                    }
                    else
                    {
                        _spawnedTexts[i].text = $"- {quest.Title}: {quest.CurrentCount}/{quest.TargetCount}";
                        _spawnedTexts[i].color = Color.white;
                    }
                }
                else
                {
                    _spawnedTexts[i].gameObject.SetActive(false);
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_isSubscribed && _questService != null)
            {
                _questService.OnQuestProgressed -= OnQuestChanged;
                _questService.OnQuestCompleted -= OnQuestChanged;
                _questService.OnQuestsReloaded -= OnQuestsReloadedHandler;
                _isSubscribed = false;
            }
        }
    }
}

using UnityEngine;
using TMPro;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using CozyLifeSim.Core;

namespace CozyLifeSim.UI
{
    public class QuestHudWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _questItemTemplate;

        private IQuestService _questService;
        private bool _isSubscribed;
        private readonly List<TextMeshProUGUI> _spawnedTexts = new List<TextMeshProUGUI>();

        [Inject]
        public void Construct(IQuestService questService)
        {
            // Unsubscribe from previous service if currently subscribed
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

                // Initial UI update
                RefreshQuests();
            }
        }

        private void Start()
        {
            // Disable raycastTarget to allow clicks to pass through and prevent blocking sticker drag-and-drop
            if (_titleText != null) _titleText.raycastTarget = false;
            if (_questItemTemplate != null)
            {
                _questItemTemplate.raycastTarget = false;
                _questItemTemplate.gameObject.SetActive(false); // Hide the template
            }

            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }
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

            // Make sure we have enough text widgets spawned to show all active database quests
            while (_spawnedTexts.Count < activeQuests.Count)
            {
                var newText = Instantiate(_questItemTemplate, _questItemTemplate.transform.parent);
                newText.raycastTarget = false;
                _spawnedTexts.Add(newText);
            }

            // Sync states and set visibility
            for (int i = 0; i < _spawnedTexts.Count; i++)
            {
                if (i < activeQuests.Count)
                {
                    var quest = activeQuests[i];
                    _spawnedTexts[i].gameObject.SetActive(true);

                    if (quest.IsCompleted)
                    {
                        // Use rich-text strike-through for completed quests
                        _spawnedTexts[i].text = $"<s>• {quest.Title} (Completed!)</s>";
                        _spawnedTexts[i].color = Color.gray;
                    }
                    else
                    {
                        _spawnedTexts[i].text = $"• {quest.Title}: {quest.CurrentCount}/{quest.TargetCount}";
                        _spawnedTexts[i].color = Color.white;
                    }
                }
                else
                {
                    _spawnedTexts[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
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

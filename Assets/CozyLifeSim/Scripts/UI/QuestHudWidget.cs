using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private CozyQuestItemWidget _questItemTemplate;

        [Header("Quest UI Sprites")]
        [SerializeField] private Sprite _itemBgSprite;
        [SerializeField] private Sprite _questWaterIcon;
        [SerializeField] private Sprite _questHarvestIcon;
        [SerializeField] private Sprite _questPetIcon;
        [SerializeField] private Sprite _questCompletedStamp;

        private IQuestService _questService;
        private bool _isSubscribed;
        private readonly List<CozyQuestItemWidget> _spawnedWidgets = new List<CozyQuestItemWidget>();

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
                _questItemTemplate.SetRaycastTargetEnabled(false);
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

            // Make sure we have enough widgets spawned to show all active database quests
            while (_spawnedWidgets.Count < activeQuests.Count)
            {
                var newWidget = Instantiate(_questItemTemplate, _questItemTemplate.transform.parent);
                newWidget.SetRaycastTargetEnabled(false);
                _spawnedWidgets.Add(newWidget);
            }

            // Sync states and set visibility
            for (int i = 0; i < _spawnedWidgets.Count; i++)
            {
                if (i < activeQuests.Count)
                {
                    var quest = activeQuests[i];
                    _spawnedWidgets[i].gameObject.SetActive(true);
                    _spawnedWidgets[i].Setup(quest, _itemBgSprite, GetTypeIcon(quest.Type), _questCompletedStamp);
                }
                else
                {
                    _spawnedWidgets[i].gameObject.SetActive(false);
                }
            }
        }


        private Sprite GetTypeIcon(QuestType type)
        {
            switch (type)
            {
                case QuestType.WaterCrops: return _questWaterIcon;
                case QuestType.HarvestCrops: return _questHarvestIcon;
                case QuestType.PetAnimal: return _questPetIcon;
                default: return null;
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

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer; // Required DI import
using VContainer.Unity; // Required LifetimeScope import
using CozyLifeSim.UI.Presenters; // Required Presenter import
using CozyLifeSim.UI.Settings; // Import Settings

namespace CozyLifeSim.UI
{
    public class AnimalWidget : MonoBehaviour
    {
        [SerializeField] private int _animalId = 1;
        [SerializeField] private Image _animalVisual;
        [SerializeField] private Button _interactionButton;
        [SerializeField] private RectTransform _heartPrefab;
        [SerializeField] private Transform _spawnRoot;

        private readonly List<Sequence> _heartSequences = new List<Sequence>();
        private Tween _breathTween;
        private Sequence _petSequence;
        private Vector3 _baseScale;
        private bool _isPetting;
        private AnimalPresenter _presenter;
        private CozyJuiceUtility _juiceUtility;
        private AnimalDatabase _animalDatabase;
        private AnimalTemplate _animalTemplate;
        private CozyLifeSim.UI.Style.IStyleService _styleService;

        [Inject]
        public void Construct(AnimalPresenter presenter, AnimalDatabase animalDatabase = null, CozyLifeSim.UI.Style.IStyleService styleService = null)
        {
            _presenter = presenter;
            _animalDatabase = animalDatabase;
            _animalTemplate = _animalDatabase != null ? _animalDatabase.GetAnimal(_animalId) : null;
            _styleService = styleService;
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                var scope = LifetimeScope.Find<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    scope.Container.Inject(this);
                }
            }

            Transform animTarget = _animalVisual != null ? _animalVisual.transform : transform;
            _baseScale = animTarget.localScale;

            if (_juiceUtility == null)
            {
                _juiceUtility = FindFirstObjectByType<CozyJuiceUtility>();
            }

            if (_presenter != null)
            {
                _presenter.OnPetRewardGiven += PlayPetCoinFly;
            }

            // Apply dynamic sprite or flat fallback from database
            if (_animalVisual != null && _animalTemplate != null)
            {
                Style.UIStyleConfig config = null;
                if (_styleService != null) config = _styleService.CurrentConfig;

                if (Style.CozyProceduralUI.ShouldUseFlatFallback(_animalTemplate.Sprite, config))
                {
                    Color tint = Style.CozyProceduralUI.GetColorForAnimal(_animalId);
                    Style.CozyProceduralUI.ApplyFlatFallback(_animalVisual, tint);
                }
                else if (_animalTemplate.Sprite != null)
                {
                    _animalVisual.sprite = _animalTemplate.Sprite;
                    _animalVisual.color = Color.white;
                    var outline = _animalVisual.GetComponent<Outline>();
                    if (outline != null) Destroy(outline);
                    var shadow = _animalVisual.GetComponent<Shadow>();
                    if (shadow != null) Destroy(shadow);
                }
            }

            // Clamped breathing configuration
            float multiplier = Mathf.Max(1.001f, _animalTemplate != null ? _animalTemplate.BreathScaleY : 1.03f);
            float duration = Mathf.Max(0.05f, _animalTemplate != null ? _animalTemplate.BreathDuration : 1.5f);

            _breathTween = animTarget.DOScaleY(_baseScale.y * multiplier, duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            if (_interactionButton != null)
            {
                _interactionButton.onClick.AddListener(PetAnimal);
            }
        }

        private void PetAnimal()
        {
            if (_isPetting)
            {
                return;
            }

            _isPetting = true;
            _breathTween?.Pause();
            _petSequence?.Kill();

            _petSequence = DOTween.Sequence();
            Transform animTarget = _animalVisual != null ? _animalVisual.transform : transform;
            RectTransform rect = animTarget as RectTransform;

            // Clamped jump configuration
            float jumpHeight = Mathf.Max(0f, _animalTemplate != null ? _animalTemplate.PetJumpHeight : 25f);
            float jumpDur = Mathf.Max(0.05f, _animalTemplate != null ? _animalTemplate.PetJumpDuration : 0.4f);

            if (rect != null)
            {
                Vector2 startPosition = rect.anchoredPosition;
                _petSequence.Append(TweenAnchoredY(rect, startPosition.y + jumpHeight, jumpDur * 0.5f).SetEase(Ease.OutQuad));
                _petSequence.Append(TweenAnchoredY(rect, startPosition.y, jumpDur * 0.5f).SetEase(Ease.InQuad));
            }
            else
            {
                _petSequence.Append(animTarget.DOJump(animTarget.position, jumpHeight * 0.01f, 1, jumpDur));
            }

            _presenter?.PetAnimal();
            SpawnHeartReaction();

            _petSequence.OnComplete(() =>
            {
                _isPetting = false;
                animTarget.localScale = _baseScale;
                _breathTween?.Play();
            });
        }

        private void SpawnHeartReaction()
        {
            if (_heartPrefab == null || _spawnRoot == null)
            {
                return;
            }

            RectTransform heart = Instantiate(_heartPrefab, _spawnRoot);
            heart.localPosition = Vector3.zero;
            heart.localScale = Vector3.zero;

            // Apply custom heart pop sprite from database if set
            if (_animalTemplate != null && _animalTemplate.HeartFeedbackSprite != null)
            {
                var img = heart.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = _animalTemplate.HeartFeedbackSprite;
                }
            }

            if (!heart.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup = heart.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 1f;

            Sequence heartSequence = DOTween.Sequence();
            _heartSequences.Add(heartSequence);

            heartSequence.Append(heart.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
            heartSequence.Join(heart.DOLocalMoveY(60f, 0.6f).SetEase(Ease.OutQuad));
            heartSequence.Insert(0.4f, canvasGroup.DOFade(0f, 0.2f));
            heartSequence.OnComplete(() =>
            {
                _heartSequences.Remove(heartSequence);
                if (heart != null)
                {
                    Destroy(heart.gameObject);
                }
            });
        }

        private static Tween TweenAnchoredY(RectTransform rect, float endValue, float duration)
        {
            return DOTween.To(
                () => rect.anchoredPosition.y,
                y =>
                {
                    Vector2 position = rect.anchoredPosition;
                    position.y = y;
                    rect.anchoredPosition = position;
                },
                endValue,
                duration);
        }

        private void PlayPetCoinFly(int rewardCoinsAmount)
        {
            if (_juiceUtility != null && _animalVisual != null)
            {
                _juiceUtility.PlayCoinFlyAnimation(_animalVisual.transform.position, rewardCoinsAmount);
            }
        }

        private void OnDestroy()
        {
            if (_presenter != null)
            {
                _presenter.OnPetRewardGiven -= PlayPetCoinFly;
            }

            _breathTween?.Kill();
            _petSequence?.Kill();

            for (int i = _heartSequences.Count - 1; i >= 0; i--)
            {
                _heartSequences[i]?.Kill();
            }

            _heartSequences.Clear();

            if (_interactionButton != null)
            {
                _interactionButton.onClick.RemoveListener(PetAnimal);
            }
        }
    }
}

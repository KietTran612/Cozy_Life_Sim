using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer; // Required DI import
using VContainer.Unity; // Required LifetimeScope import
using CozyLifeSim.UI.Presenters; // Required Presenter import

namespace CozyLifeSim.UI
{
    public class AnimalWidget : MonoBehaviour
    {
        [SerializeField] private Button _interactionButton;
        [SerializeField] private RectTransform _heartPrefab;
        [SerializeField] private Transform _spawnRoot;

        private readonly List<Sequence> _heartSequences = new List<Sequence>();
        private Tween _breathTween;
        private Sequence _petSequence;
        private Vector3 _baseScale;
        private bool _isPetting;
        private AnimalPresenter _presenter;

        [Inject]
        public void Construct(AnimalPresenter presenter)
        {
            _presenter = presenter;
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

            _baseScale = transform.localScale;
            _breathTween = transform.DOScaleY(_baseScale.y * 1.03f, 1.5f)
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
            RectTransform rect = transform as RectTransform;

            if (rect != null)
            {
                Vector2 startPosition = rect.anchoredPosition;
                _petSequence.Append(TweenAnchoredY(rect, startPosition.y + 25f, 0.2f).SetEase(Ease.OutQuad));
                _petSequence.Append(TweenAnchoredY(rect, startPosition.y, 0.2f).SetEase(Ease.InQuad));
            }
            else
            {
                _petSequence.Append(transform.DOJump(transform.position, 0.25f, 1, 0.4f));
            }

            _presenter?.PetAnimal();
            SpawnHeartReaction();

            _petSequence.OnComplete(() =>
            {
                _isPetting = false;
                transform.localScale = _baseScale;
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

        private void OnDestroy()
        {
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

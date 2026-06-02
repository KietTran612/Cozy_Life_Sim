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

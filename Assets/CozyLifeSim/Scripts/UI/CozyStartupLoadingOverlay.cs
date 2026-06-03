using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CozyLifeSim.UI
{
    public sealed class CozyStartupLoadingOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float minVisibleSeconds = 0.35f;
        [SerializeField] private float fadeOutSeconds = 0.2f;

        private CancellationTokenSource hideCts;
        private Tween fadeTween;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            ShowImmediate();
        }

        private void Start()
        {
            hideCts = new CancellationTokenSource();
            HideAfterStartupAsync(hideCts.Token).Forget();
        }

        private void OnDestroy()
        {
            if (hideCts != null)
            {
                hideCts.Cancel();
                hideCts.Dispose();
                hideCts = null;
            }
            fadeTween?.Kill();
            fadeTween = null;
        }

        public void ShowImmediate()
        {
            gameObject.SetActive(true);
            if (messageText != null)
            {
                messageText.text = "Dang vao nong trai...";
            }

            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private async UniTaskVoid HideAfterStartupAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken);
                await UniTask.Delay(TimeSpan.FromSeconds(minVisibleSeconds), cancellationToken: cancellationToken);
                Hide();
            }
            catch (OperationCanceledException)
            {
                // Object teardown cancels startup hiding; no runtime action is needed.
            }
        }

        public void Hide()
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            fadeTween?.Kill();
            fadeTween = canvasGroup
                .DOFade(0f, fadeOutSeconds)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}

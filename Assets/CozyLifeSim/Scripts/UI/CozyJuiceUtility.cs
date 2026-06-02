using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CozyLifeSim.UI
{
    public class CozyJuiceUtility : MonoBehaviour
    {
        [SerializeField] private RectTransform _coinTargetTransform;
        [SerializeField] private GameObject _coinPrefab;

        private readonly Queue<GameObject> _coinPool = new Queue<GameObject>();
        private readonly List<GameObject> _allPooledCoins = new List<GameObject>();
        private GameObject _activeTemplate;

        private void Start()
        {
            DOTween.SetTweensCapacity(500, 50);
            EnsureTemplate();
            PrewarmCoins(30);
        }

        public void PlayCoinFlyAnimation(Vector3 startWorldPos, int coinsAmount)
        {
            if (_coinTargetTransform == null) return;
            PlayCoinFlyAnimation(startWorldPos, _coinTargetTransform.position, coinsAmount);
        }

        public void PlayCoinFlyAnimation(Vector3 startWorldPos, Vector3 endWorldPos, int coinsAmount)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureTemplate();

            int coinCount = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1, coinsAmount) / 5f), 3, 12);

            for (int i = 0; i < coinCount; i++)
            {
                GameObject coin = GetCoin();
                coin.transform.position = startWorldPos;
                coin.transform.localScale = Vector3.one * 0.8f;
                coin.SetActive(true);

                Vector3 offset = new Vector3(
                    Random.Range(-35f, 35f),
                    Random.Range(20f, 70f),
                    0f);

                Sequence seq = DOTween.Sequence().SetTarget(coin);
                seq.AppendInterval(i * 0.035f);
                seq.Append(coin.transform.DOMove(startWorldPos + offset, 0.18f).SetEase(Ease.OutQuad));
                seq.Append(coin.transform.DOMove(endWorldPos, 0.42f).SetEase(Ease.InQuad));
                seq.Join(coin.transform.DOScale(Vector3.one * 0.35f, 0.42f).SetEase(Ease.InQuad));
                seq.OnComplete(() => ReturnCoin(coin));
            }
        }

        private void EnsureTemplate()
        {
            if (_activeTemplate != null)
            {
                return;
            }

            if (_coinPrefab != null)
            {
                _activeTemplate = _coinPrefab;
                return;
            }

            GameObject template = new GameObject("ProceduralCoinTemplate");
            template.transform.SetParent(transform, false);

            RectTransform rect = template.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(24f, 24f);

            Image image = template.AddComponent<Image>();
            image.color = new Color(1f, 0.82f, 0.2f, 1f);
            Style.CozyProceduralUI.ApplyFlatFallback(image, new Color(1f, 0.82f, 0.2f, 1f));

            template.SetActive(false);
            _activeTemplate = template;
        }

        private void PrewarmCoins(int count)
        {
            while (_allPooledCoins.Count < count)
            {
                ReturnCoin(CreateCoin());
            }
        }

        private GameObject GetCoin()
        {
            return _coinPool.Count > 0 ? _coinPool.Dequeue() : CreateCoin();
        }

        private GameObject CreateCoin()
        {
            GameObject coin = Instantiate(_activeTemplate, transform, false);
            coin.name = "Pooled_Coin";
            _allPooledCoins.Add(coin);
            return coin;
        }

        private void ReturnCoin(GameObject coin)
        {
            if (coin == null)
            {
                return;
            }

            DOTween.Kill(coin);
            coin.SetActive(false);
            coin.transform.SetParent(transform, false);
            coin.transform.localScale = Vector3.one;
            _coinPool.Enqueue(coin);
        }

        private void OnDestroy()
        {
            foreach (GameObject coin in _allPooledCoins)
            {
                DOTween.Kill(coin);
            }

            _coinPool.Clear();
            _allPooledCoins.Clear();
        }
    }
}

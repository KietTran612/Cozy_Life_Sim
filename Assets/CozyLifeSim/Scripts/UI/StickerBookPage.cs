using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using CozyLifeSim.Core;
using CozyLifeSim.UI.Presenters;
using DG.Tweening;

namespace CozyLifeSim.UI
{
    [RequireComponent(typeof(Image))]
    public class StickerBookPage : MonoBehaviour
    {
        [SerializeField] private int _pageIndex;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Sprite[] _backgroundStyles;
        [SerializeField] private int _fallbackStyleCount = 3;
        [SerializeField] private CozyDiaryNote _diaryNotePrefabTemplate;

        public int PageIndex => _pageIndex;
        public int StyleCount => _backgroundStyles != null && _backgroundStyles.Length > 0 ? _backgroundStyles.Length : Mathf.Max(0, _fallbackStyleCount);

        private readonly List<CozyDiaryNote> _spawnedDiaryNotes = new List<CozyDiaryNote>();
        private StickerBookPresenter _presenter;
        private bool _isSubscribed;
        private readonly List<Tween> _noteTweens = new List<Tween>();

        public bool TryPlaceSticker(CozySticker sticker, Vector2 screenPosition, Camera eventCamera)
        {
            RectTransform pageRect = GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(pageRect, screenPosition, eventCamera, out Vector2 localPoint))
            {
                // Confirm drop is fully within page boundaries
                if (pageRect.rect.Contains(localPoint))
                {
                    sticker.FinalizePlacement(transform, localPoint, _pageIndex);
                    return true;
                }
            }
            return false;
        }

        public void Initialize(StickerBookPresenter presenter, CozyDiaryNote diaryNotePrefabTemplate)
        {
            if (_presenter != null && _presenter != presenter && _isSubscribed)
            {
                UnsubscribePresenterEvents();
            }

            _presenter = presenter;
            if (diaryNotePrefabTemplate != null)
            {
                _diaryNotePrefabTemplate = diaryNotePrefabTemplate;
            }

            SubscribePresenterEvents();
        }

        public void RestoreDiaryNotes(IReadOnlyList<DiaryNotePlacedData> notes)
        {
            ClearDiaryNotes();
            if (notes == null) return;

            foreach (var note in notes)
            {
                if (note.PageIndex == _pageIndex)
                {
                    SpawnDiaryNote(note, false);
                }
            }
        }

        public void ApplyPageStyle(int styleIndex)
        {
            if (_backgroundImage == null)
            {
                _backgroundImage = GetComponent<Image>();
            }

            if (_backgroundImage == null || StyleCount == 0) return;

            int clamped = Mathf.Clamp(styleIndex, 0, StyleCount - 1);
            if (_backgroundStyles != null && clamped < _backgroundStyles.Length && _backgroundStyles[clamped] != null)
            {
                _backgroundImage.sprite = _backgroundStyles[clamped];
                _backgroundImage.color = Color.white;
                _backgroundImage.type = Image.Type.Sliced;
            }
            else
            {
                _backgroundImage.sprite = null;
                _backgroundImage.type = Image.Type.Simple;
                Color[] fallbackColors =
                {
                    new Color(0.98f, 0.92f, 0.76f, 1f),
                    new Color(0.86f, 0.94f, 0.88f, 1f),
                    new Color(0.94f, 0.88f, 0.98f, 1f)
                };
                _backgroundImage.color = fallbackColors[clamped % fallbackColors.Length];
            }
        }

        private void SubscribePresenterEvents()
        {
            if (_presenter == null || _isSubscribed) return;

            _presenter.OnDiaryNoteAdded += HandleDiaryNoteAdded;
            _presenter.OnDiaryNotePositionUpdated += HandleDiaryNotePositionUpdated;
            _presenter.OnDiaryNoteRemoved += HandleDiaryNoteRemoved;
            _presenter.OnPageStyleChanged += HandlePageStyleChanged;
            _isSubscribed = true;
        }

        private void UnsubscribePresenterEvents()
        {
            if (_presenter == null || !_isSubscribed) return;

            _presenter.OnDiaryNoteAdded -= HandleDiaryNoteAdded;
            _presenter.OnDiaryNotePositionUpdated -= HandleDiaryNotePositionUpdated;
            _presenter.OnDiaryNoteRemoved -= HandleDiaryNoteRemoved;
            _presenter.OnPageStyleChanged -= HandlePageStyleChanged;
            _isSubscribed = false;
        }

        private void HandleDiaryNoteAdded(DiaryNotePlacedData data)
        {
            if (data.PageIndex == _pageIndex)
            {
                SpawnDiaryNote(data, true);
            }
        }

        private void HandleDiaryNotePositionUpdated(DiaryNotePlacedData data)
        {
            if (data.PageIndex != _pageIndex) return;

            CozyDiaryNote note = FindDiaryNote(data.NoteId);
            if (note != null)
            {
                note.ApplySavedPosition(new Vector2(data.PositionX, data.PositionY));
            }
        }

        private void HandleDiaryNoteRemoved(string noteId)
        {
            CozyDiaryNote note = FindDiaryNote(noteId);
            if (note == null) return;

            _spawnedDiaryNotes.Remove(note);
            RectTransform noteRect = null;
            note.TryGetComponent(out noteRect);
            if (noteRect != null)
            {
                var tween = noteRect.DOScale(0f, 0.16f)
                    .SetEase(Ease.InBack)
                    .SetTarget(this)
                    .OnComplete(() =>
                {
                    if (note != null)
                    {
                        Destroy(note.gameObject);
                    }
                });
                _noteTweens.Add(tween);
            }
            else
            {
                Destroy(note.gameObject);
            }
        }

        private void HandlePageStyleChanged(PageStyleData data)
        {
            if (data.PageIndex == _pageIndex)
            {
                ApplyPageStyle(data.StyleIndex);
            }
        }

        private CozyDiaryNote SpawnDiaryNote(DiaryNotePlacedData data, bool animate)
        {
            if (_diaryNotePrefabTemplate == null || _presenter == null) return null;

            CozyDiaryNote note = Instantiate(_diaryNotePrefabTemplate, transform);
            note.gameObject.SetActive(true);
            note.Setup(_presenter, data);
            if (animate)
            {
                note.transform.localScale = Vector3.zero;
                var tween = note.transform.DOScale(1f, 0.18f)
                    .SetEase(Ease.OutBack)
                    .SetTarget(this);
                _noteTweens.Add(tween);
            }
            _spawnedDiaryNotes.Add(note);
            return note;
        }

        private CozyDiaryNote FindDiaryNote(string noteId)
        {
            if (string.IsNullOrEmpty(noteId)) return null;

            for (int i = _spawnedDiaryNotes.Count - 1; i >= 0; i--)
            {
                var note = _spawnedDiaryNotes[i];
                if (note == null)
                {
                    _spawnedDiaryNotes.RemoveAt(i);
                    continue;
                }
                if (note.NoteId == noteId)
                {
                    return note;
                }
            }
            return null;
        }

        private void ClearDiaryNotes()
        {
            foreach (var note in _spawnedDiaryNotes)
            {
                if (note != null)
                {
                    Destroy(note.gameObject);
                }
            }
            _spawnedDiaryNotes.Clear();
        }

        private void OnDestroy()
        {
            foreach (var tween in _noteTweens)
            {
                tween?.Kill();
            }
            _noteTweens.Clear();
            UnsubscribePresenterEvents();
        }
    }
}

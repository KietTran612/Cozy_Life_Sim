using UnityEngine;
using VContainer;
using CozyLifeSim.UI.Style;

namespace CozyLifeSim.UI
{
    [ExecuteAlways]
    public class CozyWidgetPlaceholder : MonoBehaviour
    {
        public enum WidgetType
        {
            PrimaryButton,
            PanelBackground
        }

        [SerializeField] private WidgetType _widgetType;
        [SerializeField] private UIStyleConfig _editorOnlyConfig; // Used in editor mode to preview
        
        private IStyleService _styleService;
        private GameObject _previewInstance;
        private GameObject _runtimeInstance;

        [Inject]
        public void Construct(IStyleService styleService)
        {
            _styleService = styleService;
            _styleService.OnStyleChanged += RefreshWidget;
            RefreshWidget();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                GenerateEditorPreview();
            }
        }

        private void GenerateEditorPreview()
        {
            ClearPreview();
            if (_editorOnlyConfig == null) return;

            GameObject prefab = GetPrefabFromConfig(_editorOnlyConfig);
            if (prefab == null) return;

            _previewInstance = Instantiate(prefab, transform);
            // HideFlags.DontSave prevents this object from being written into Scene/Prefab asset files
            _previewInstance.hideFlags = HideFlags.DontSave;
        }

        private void RefreshWidget()
        {
            if (!Application.isPlaying) return;

            // Runtime Swapping
            ClearRuntimeInstance();
            if (_styleService == null || _styleService.CurrentConfig == null) return;

            GameObject prefab = GetPrefabFromConfig(_styleService.CurrentConfig);
            if (prefab == null) return;

            // Spawn actual theme prefab at runtime
            _runtimeInstance = Instantiate(prefab, transform);
            
            // Transfer RectTransform constraints
            RectTransform sourceRect = GetComponent<RectTransform>();
            RectTransform targetRect = _runtimeInstance.GetComponent<RectTransform>();
            if (sourceRect != null && targetRect != null)
            {
                targetRect.anchorMin = sourceRect.anchorMin;
                targetRect.anchorMax = sourceRect.anchorMax;
                targetRect.anchoredPosition = sourceRect.anchoredPosition;
                targetRect.sizeDelta = sourceRect.sizeDelta;
                targetRect.pivot = sourceRect.pivot;
                targetRect.localScale = sourceRect.localScale;
            }
        }

        private GameObject GetPrefabFromConfig(UIStyleConfig config)
        {
            return _widgetType switch
            {
                WidgetType.PrimaryButton => config.PrimaryButtonPrefab,
                WidgetType.PanelBackground => config.PanelBackgroundPrefab,
                _ => null
            };
        }

        private void ClearPreview()
        {
            if (_previewInstance != null)
            {
                DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.gameObject.hideFlags == HideFlags.DontSave)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void ClearRuntimeInstance()
        {
            if (_runtimeInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_runtimeInstance);
                }
                else
                {
                    DestroyImmediate(_runtimeInstance);
                }
                _runtimeInstance = null;
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                ClearPreview();
            }
        }

        private void OnDestroy()
        {
            if (_styleService != null)
            {
                _styleService.OnStyleChanged -= RefreshWidget;
            }
            ClearRuntimeInstance();
        }
    }
}

using UnityEngine;
using VContainer;
using VContainer.Unity;
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
        private bool _isInjected;

        [Inject]
        public void Construct(IStyleService styleService)
        {
            if (_isInjected) return;
            _isInjected = true;

            _styleService = styleService;
            _styleService.OnStyleChanged += RefreshWidget;
            RefreshWidget();
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
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                GenerateEditorPreview();
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null) GenerateEditorPreview();
                };
            }
        }
        #endif

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
            
            // Stretch to fill placeholder parent completely to avoid double offset multiplier bug
            RectTransform sourceRect = GetComponent<RectTransform>();
            RectTransform targetRect = _runtimeInstance.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                targetRect.anchorMin = Vector2.zero;
                targetRect.anchorMax = Vector2.one;
                targetRect.anchoredPosition = Vector2.zero;
                targetRect.sizeDelta = Vector2.zero;
                targetRect.pivot = sourceRect != null ? sourceRect.pivot : new Vector2(0.5f, 0.5f);
                targetRect.localScale = Vector3.one;
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

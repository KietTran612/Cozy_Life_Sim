using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using CozyLifeSim.UI;

namespace CozyLifeSim.Editor
{
    public partial class CozySceneSetupWindow
    {
        private RectTransform SetupPanel(Transform parent, string name, ref bool isDirty)
        {
            GameObject go = null;
            if (parent != null)
            {
                Transform found = parent.Find(name);
                if (found != null)
                {
                    go = found.gameObject;
                }
            }
            else
            {
                go = GameObject.Find(name);
            }

            bool createdNew = false;
            if (go == null)
            {
                go = new GameObject(name);
                createdNew = true;
                if (parent != null)
                {
                    go.transform.SetParent(parent, false);
                }
                isDirty = true;
            }
            else if (parent != null && go.transform.parent != parent)
            {
                go.transform.SetParent(parent, false);
                isDirty = true;
                createdNew = true;
            }

            if (createdNew && !Application.isPlaying)
            {
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
                isDirty = true;
            }

            // Ensure Image component is present for background panels to raycast/drag drop
            if (name.Contains("Plot") || name.Contains("Pen") || name.Contains("Panel") || name.Contains("Tray") || name.Contains("Page_"))
            {
                Image img = go.GetComponent<Image>();
                if (img == null)
                {
                    img = go.AddComponent<Image>();
                    img.color = new Color(0f, 0f, 0f, 0.15f); // Semi transparent backing
                    isDirty = true;
                }
                else
                {
                    Color targetColor = new Color(0f, 0f, 0f, 0.15f);
                    if (name == "Sidebar_Panel") targetColor = new Color(0f, 0f, 0f, 0.25f);
                    else if (name.Contains("Content_Panel")) targetColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);

                    if (img.color != targetColor)
                    {
                        img.color = targetColor;
                        isDirty = true;
                    }
                }
            }

            return rect;
        }

        private void ClearChildren(RectTransform parent)
        {
            if (parent == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private TextMeshProUGUI SetupText(Transform parent, string name, string defaultText, string styleKey, ref bool isDirty)
        {
            RectTransform panel = SetupPanel(parent, name, ref isDirty);
            TextMeshProUGUI tmp = panel.gameObject.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = panel.gameObject.AddComponent<TextMeshProUGUI>();
                isDirty = true;
            }
            if (tmp.fontSize <= 0f || Mathf.Approximately(tmp.fontSize, -99f))
            {
                tmp.fontSize = 16f;
                isDirty = true;
                UnityEditor.EditorUtility.SetDirty(tmp);
            }

            // Fallback default font to prevent TMPro NullReferenceException during layout rendering
            if (tmp.font == null)
            {
                tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (tmp.font == null)
                {
                    string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                    if (fontGuids.Length > 0)
                    {
                        tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
                    }
                }
                isDirty = true;
            }

            if (tmp.text != defaultText)
            {
                tmp.text = defaultText;
                isDirty = true;
            }

            TextAlignmentOptions targetAlignment = TextAlignmentOptions.Center;
            if (name.Contains("Count")) targetAlignment = TextAlignmentOptions.BottomRight;
            else if (name.Contains("Template")) targetAlignment = TextAlignmentOptions.Left;

            if (tmp.alignment != targetAlignment)
            {
                tmp.alignment = targetAlignment;
                isDirty = true;
            }

            if (!string.IsNullOrEmpty(styleKey))
            {
                UIStyleElement element = panel.gameObject.GetComponent<UIStyleElement>();
                if (element == null)
                {
                    element = panel.gameObject.AddComponent<UIStyleElement>();
                    isDirty = true;
                }

                SerializedObject soElement = new SerializedObject(element);
                bool elementDirty = false;
                SerializedProperty propKey = soElement.FindProperty("_styleKey");
                if (propKey != null && propKey.stringValue != styleKey)
                {
                    propKey.stringValue = styleKey;
                    elementDirty = true;
                }
                if (elementDirty)
                {
                    soElement.ApplyModifiedProperties();
                    isDirty = true;
                }
            }

            return tmp;
        }

        private Image SetupImage(Transform parent, string name, ref bool isDirty)
        {
            RectTransform panel = SetupPanel(parent, name, ref isDirty);
            Image img = panel.gameObject.GetComponent<Image>();
            if (img == null)
            {
                img = panel.gameObject.AddComponent<Image>();
                isDirty = true;
            }
            if (img.color != Color.white)
            {
                img.color = Color.white;
                isDirty = true;
            }
            return img;
        }

        private Button SetupButton(Transform parent, string name, string labelText, ref bool isDirty)
        {
            RectTransform panel = SetupPanel(parent, name, ref isDirty);
            Button btn = panel.gameObject.GetComponent<Button>();
            if (btn == null)
            {
                btn = panel.gameObject.AddComponent<Button>();
                isDirty = true;
            }

            CozyButtonJuice juice = btn.gameObject.GetComponent<CozyButtonJuice>();
            if (juice == null)
            {
                juice = btn.gameObject.AddComponent<CozyButtonJuice>();
                isDirty = true;
            }

            // Ensure Image component exists on Button for transition target graphic and raycast target
            Image btnImg = panel.gameObject.GetComponent<Image>();
            if (btnImg == null)
            {
                btnImg = panel.gameObject.AddComponent<Image>();
                btnImg.color = new Color(1f, 1f, 1f, 0.8f); // Default light white background
                isDirty = true;
            }

            // Wire target graphic for proper transitions and click bounds
            if (btn.targetGraphic != btnImg)
            {
                btn.targetGraphic = btnImg;
                isDirty = true;
            }

            // Add text label inside button
            TextMeshProUGUI label = panel.gameObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                GameObject labelGo = new GameObject("Label");
                labelGo.transform.SetParent(panel, false);
                label = labelGo.AddComponent<TextMeshProUGUI>();
                RectTransform labelRect = label.GetComponent<RectTransform>();
                StretchToFill(labelRect, ref isDirty);
                isDirty = true;
            }

            // Fallback default font to prevent TMPro NullReferenceException during layout rendering
            if (label.font == null)
            {
                label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (label.font == null)
                {
                    string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                    if (fontGuids.Length > 0)
                    {
                        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
                    }
                }
                isDirty = true;
            }

            if (label.text != labelText)
            {
                label.text = labelText;
                isDirty = true;
            }

            if (label.alignment != TextAlignmentOptions.Center)
            {
                label.alignment = TextAlignmentOptions.Center;
                isDirty = true;
            }

            if (!Mathf.Approximately(label.fontSize, 24f))
            {
                label.fontSize = 24f;
                isDirty = true;
            }

            if (label.color != Color.black)
            {
                label.color = Color.black;
                isDirty = true;
            }

            return btn;
        }

        private void StretchToFill(RectTransform rect, ref bool isDirty)
        {
            SafeSetAnchor(rect, Vector2.zero, Vector2.one, ref isDirty);
            SafeSetSizeDelta(rect, Vector2.zero, ref isDirty);
            SafeSetAnchoredPosition(rect, Vector2.zero, ref isDirty);
        }

        private static void SafeSetAnchor(RectTransform rect, Vector2 min, Vector2 max, ref bool isDirty)
        {
            if (rect.anchorMin != min)
            {
                rect.anchorMin = min;
                isDirty = true;
            }
            if (rect.anchorMax != max)
            {
                rect.anchorMax = max;
                isDirty = true;
            }
        }

        private static void SafeSetSizeDelta(RectTransform rect, Vector2 sizeDelta, ref bool isDirty)
        {
            if (rect.sizeDelta != sizeDelta)
            {
                rect.sizeDelta = sizeDelta;
                isDirty = true;
            }
        }

        private static void SafeSetAnchoredPosition(RectTransform rect, Vector2 anchoredPosition, ref bool isDirty)
        {
            if (rect.anchoredPosition != anchoredPosition)
            {
                rect.anchoredPosition = anchoredPosition;
                isDirty = true;
            }
        }

        private static void SafeSetPivot(RectTransform rect, Vector2 pivot, ref bool isDirty)
        {
            if (rect.pivot != pivot)
            {
                rect.pivot = pivot;
                isDirty = true;
            }
        }

        private static void SafeSetObjectReference(SerializedProperty prop, UnityEngine.Object target, ref bool isDirty)
        {
            if (prop != null && prop.objectReferenceValue != target)
            {
                prop.objectReferenceValue = target;
                isDirty = true;
            }
        }

        private static void SafeSetInt(SerializedProperty prop, int value, ref bool isDirty)
        {
            if (prop != null && prop.intValue != value)
            {
                prop.intValue = value;
                isDirty = true;
            }
        }

        private static void SafeSetFloat(SerializedProperty prop, float value, ref bool isDirty)
        {
            if (prop != null && !Mathf.Approximately(prop.floatValue, value))
            {
                prop.floatValue = value;
                isDirty = true;
            }
        }

        private static void SafeSetString(SerializedProperty prop, string value, ref bool isDirty)
        {
            if (prop != null && prop.stringValue != value)
            {
                prop.stringValue = value;
                isDirty = true;
            }
        }

        private void ConfigureCloseButton(Button btn, Sprite closeSprite, ref bool isDirty)
        {
            if (btn == null) return;
            Image btnImg = btn.GetComponent<Image>();
            if (btnImg != null)
            {
                if (btnImg.sprite != closeSprite)
                {
                    btnImg.sprite = closeSprite;
                    isDirty = true;
                }
                if (btnImg.type != Image.Type.Simple)
                {
                    btnImg.type = Image.Type.Simple;
                    isDirty = true;
                }
                if (btnImg.color != Color.white)
                {
                    btnImg.color = Color.white;
                    isDirty = true;
                }
            }
            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null && label.gameObject.activeSelf)
            {
                label.gameObject.SetActive(false);
                isDirty = true;
            }
        }

        private void SetupButtonIcon(Button btn, string iconName, Sprite iconSprite, ref bool isDirty)
        {
            if (btn == null) return;
            RectTransform iconPanel = SetupPanel(btn.transform, iconName, ref isDirty);
            Image img = iconPanel.gameObject.GetComponent<Image>();
            if (img == null)
            {
                img = iconPanel.gameObject.AddComponent<Image>();
                isDirty = true;
            }
            if (img.sprite != iconSprite)
            {
                img.sprite = iconSprite;
                isDirty = true;
            }
            if (img.type != Image.Type.Simple)
            {
                img.type = Image.Type.Simple;
                isDirty = true;
            }
            if (img.color != Color.white)
            {
                img.color = Color.white;
                isDirty = true;
            }
            SafeSetAnchor(iconPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ref isDirty);
            SafeSetPivot(iconPanel, new Vector2(0.5f, 0.5f), ref isDirty);
            SafeSetSizeDelta(iconPanel, new Vector2(40f, 40f), ref isDirty);
            SafeSetAnchoredPosition(iconPanel, Vector2.zero, ref isDirty);

            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null && label.gameObject.activeSelf)
            {
                label.gameObject.SetActive(false);
                isDirty = true;
            }
        }

        private CozyQuestItemWidget SetupQuestItemWidgetTemplate(Transform parent, string name, ref bool isDirty)
        {
            RectTransform templateRect = SetupPanel(parent, name, ref isDirty);
            SafeSetSizeDelta(templateRect, new Vector2(400f, 60f), ref isDirty);

            CozyQuestItemWidget widget = templateRect.gameObject.GetComponent<CozyQuestItemWidget>();
            if (widget == null)
            {
                widget = templateRect.gameObject.AddComponent<CozyQuestItemWidget>();
                isDirty = true;
            }

            Image bgImg = SetupImage(templateRect, "Bg_Image", ref isDirty);
            RectTransform bgRect = bgImg.GetComponent<RectTransform>();
            StretchToFill(bgRect, ref isDirty);

            Image typeIconImg = SetupImage(templateRect, "Type_Icon", ref isDirty);
            RectTransform typeIconRect = typeIconImg.GetComponent<RectTransform>();
            SafeSetAnchor(typeIconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), ref isDirty);
            SafeSetPivot(typeIconRect, new Vector2(0f, 0.5f), ref isDirty);
            SafeSetSizeDelta(typeIconRect, new Vector2(40f, 40f), ref isDirty);
            SafeSetAnchoredPosition(typeIconRect, new Vector2(10f, 0f), ref isDirty);

            TextMeshProUGUI questText = SetupText(templateRect, "Quest_Text", "Quest Description", "", ref isDirty);
            RectTransform questTextRect = questText.GetComponent<RectTransform>();
            SafeSetAnchor(questTextRect, new Vector2(0f, 0f), new Vector2(1f, 1f), ref isDirty);
            SafeSetPivot(questTextRect, new Vector2(0f, 0.5f), ref isDirty);
            SafeSetSizeDelta(questTextRect, new Vector2(-150f, 0f), ref isDirty);
            SafeSetAnchoredPosition(questTextRect, new Vector2(60f, 0f), ref isDirty);
            if (questText.alignment != TextAlignmentOptions.Left)
            {
                questText.alignment = TextAlignmentOptions.Left;
                isDirty = true;
            }
            if (!Mathf.Approximately(questText.fontSize, 18f))
            {
                questText.fontSize = 18f;
                isDirty = true;
            }

            Image stampOverlayImg = SetupImage(templateRect, "Stamp_Overlay", ref isDirty);
            RectTransform stampOverlayRect = stampOverlayImg.GetComponent<RectTransform>();
            SafeSetAnchor(stampOverlayRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), ref isDirty);
            SafeSetPivot(stampOverlayRect, new Vector2(1f, 0.5f), ref isDirty);
            SafeSetSizeDelta(stampOverlayRect, new Vector2(50f, 50f), ref isDirty);
            SafeSetAnchoredPosition(stampOverlayRect, new Vector2(-10f, 0f), ref isDirty);

            SerializedObject soWidget = new SerializedObject(widget);
            bool widgetDirty = false;
            SafeSetObjectReference(soWidget.FindProperty("_bgImage"), bgImg, ref widgetDirty);
            SafeSetObjectReference(soWidget.FindProperty("_typeIcon"), typeIconImg, ref widgetDirty);
            SafeSetObjectReference(soWidget.FindProperty("_questText"), questText, ref widgetDirty);
            SafeSetObjectReference(soWidget.FindProperty("_stampOverlay"), stampOverlayImg, ref widgetDirty);
            if (widgetDirty)
            {
                soWidget.ApplyModifiedProperties();
                isDirty = true;
            }

            return widget;
        }

        private static void ConfigureBuildAndPlayerSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var existingIndex = scenes.FindIndex(scene => scene.path == MainScenePath);

            if (existingIndex >= 0)
            {
                var scene = scenes[existingIndex];
                if (existingIndex != 0 || !scene.enabled)
                {
                    scenes.RemoveAt(existingIndex);
                    scenes.Insert(0, new EditorBuildSettingsScene(MainScenePath, true));
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
            }
            else
            {
                scenes.Insert(0, new EditorBuildSettingsScene(MainScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        private void ConfigureHeritageTextures()
        {
            string folderPath = "Assets/CozyLifeSim/Textures/Heritage";
            if (!System.IO.Directory.Exists(folderPath))
            {
                Debug.LogWarning($"[CozySim] Heritage textures folder not found at '{folderPath}'");
                return;
            }

            string[] pngFiles = System.IO.Directory.GetFiles(folderPath, "*.png");
            foreach (string file in pngFiles)
            {
                string assetPath = file.Replace("\\", "/");
                string fileName = System.IO.Path.GetFileName(assetPath);

                int expectedMaxSize = GetExpectedMaxSize(fileName);
                bool expectedTrim = GetExpectedTrim(fileName);
                Vector4 expectedBorder = GetExpectedBorder(fileName);

                CozyAssetImporterUtility.ConfigureAsSpriteWithBorder(assetPath, expectedBorder, expectedMaxSize, expectedTrim);
            }
        }

        private int GetExpectedMaxSize(string fileName)
        {
            if (fileName == "UI_Panel_Frame_Wood.png" ||
                fileName == "UI_Dialogue_Bubble.png" ||
                fileName == "UI_Scrapbook_Notebook_Open.png" ||
                fileName == "UI_Scrapbook_Cover.png")
            {
                return 1024;
            }

            if (fileName.StartsWith("Scrapbook_Bg_") ||
                fileName == "World_Shop_Stall.png" ||
                fileName == "World_Quest_Board.png" ||
                fileName.StartsWith("World_Character_") ||
                fileName == "World_Soil_Plot.png" ||
                fileName == "World_Animal_Pen.png" ||
                fileName.StartsWith("Animal_") ||
                fileName.StartsWith("Sticker_") ||
                fileName.StartsWith("NPC_Portrait_") ||
                fileName == "UI_Quest_Item_Bg.png" ||
                fileName == "Scrapbook_StickyNote_Yellow.png" ||
                fileName == "UI_Banner_LevelUp.png")
            {
                return 512;
            }

            return 256;
        }

        private bool GetExpectedTrim(string fileName)
        {
            if (fileName == "UI_Panel_Frame_Wood.png" ||
                fileName == "UI_Dialogue_Bubble.png" ||
                fileName == "UI_Scrapbook_Notebook_Open.png" ||
                fileName == "UI_Tab_Button_Bg.png" ||
                fileName == "UI_Quest_Item_Bg.png" ||
                fileName == "Scrapbook_StickyNote_Yellow.png" ||
                fileName == "UI_Banner_LevelUp.png")
            {
                return false;
            }
            if (fileName.StartsWith("Scrapbook_Bg_") || fileName == "UI_Scrapbook_Cover.png")
            {
                return false;
            }

            return true;
        }

        private Vector4 GetExpectedBorder(string fileName)
        {
            if (fileName == "UI_Panel_Frame_Wood.png") return new Vector4(64, 64, 64, 64);
            if (fileName == "UI_Dialogue_Bubble.png") return new Vector4(64, 64, 64, 64);
            if (fileName == "UI_Scrapbook_Notebook_Open.png") return new Vector4(128, 128, 128, 128);
            if (fileName == "UI_Tab_Button_Bg.png") return new Vector4(32, 32, 32, 32);
            if (fileName == "UI_Quest_Item_Bg.png") return new Vector4(32, 32, 32, 32);
            if (fileName == "Scrapbook_StickyNote_Yellow.png") return new Vector4(64, 64, 64, 64);
            if (fileName == "UI_Banner_LevelUp.png") return new Vector4(64, 64, 64, 64);
            return Vector4.zero;
        }
    }
}

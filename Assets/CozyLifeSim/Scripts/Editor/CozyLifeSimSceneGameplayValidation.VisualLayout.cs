using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CozyLifeSim.Editor
{
    public static partial class CozyLifeSimSceneGameplayValidation
    {
        private static void ValidateVisualLayout(List<string> errors, List<string> warnings, List<string> passes)
        {
            ValidateHomeScreenShell(errors, warnings, passes);
            ValidateHomeGameplayWidgets(errors, warnings, passes);
            ValidateHomeWorldObjectsAndCamera(errors, warnings, passes);
            ValidateQuestHudAndPopup(errors, warnings, passes);
            ValidateShopPopup(errors, warnings, passes);
            ValidateDialoguePopup(errors, warnings, passes);
            ValidateDiaryAndScrapbook(errors, warnings, passes);
            ValidateAggregateVisualLayout(errors, warnings, passes);
        }

        private static RectTransform FindSceneRectTransform(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (path.StartsWith("Canvas/"))
            {
                var canvasGo = GameObject.Find("Canvas");
                if (canvasGo != null)
                {
                    string[] parts = path.Substring(7).Split('/');
                    Transform current = canvasGo.transform;
                    foreach (string part in parts)
                    {
                        current = current.Find(part);
                        if (current == null) break;
                    }
                    return current != null ? current.GetComponent<RectTransform>() : null;
                }
            }
            var go = GameObject.Find(path);
            return go != null ? go.GetComponent<RectTransform>() : null;
        }

        private static Image FindSceneImage(string path)
        {
            var rect = FindSceneRectTransform(path);
            return rect != null ? rect.GetComponent<Image>() : null;
        }

        private static void ValidateImagePolicy(string path, Image.Type expectedType, bool expectedPreserveAspect, List<string> errors, List<string> passes)
        {
            Image image = FindSceneImage(path);
            if (image == null)
            {
                errors.Add($"Visual image '{path}' is missing.");
                return;
            }

            if (image.type != expectedType)
            {
                errors.Add($"Visual image '{path}' type is {image.type}, expected {expectedType}.");
                return;
            }

            if (image.preserveAspect != expectedPreserveAspect)
            {
                errors.Add($"Visual image '{path}' preserveAspect is {image.preserveAspect}, expected {expectedPreserveAspect}.");
                return;
            }

            passes.Add($"Visual image '{path}' has expected type/aspect policy.");
        }

        private static void ValidateRectSizeRange(string path, float minWidth, float maxWidth, float minHeight, float maxHeight, List<string> errors, List<string> passes)
        {
            RectTransform rect = FindSceneRectTransform(path);
            if (rect == null)
            {
                errors.Add($"RectTransform '{path}' is missing.");
                return;
            }

            float width = rect.rect.width;
            float height = rect.rect.height;

            if (width < minWidth || width > maxWidth)
            {
                errors.Add($"RectTransform '{path}' width is {width:0.0}, expected between {minWidth:0.0} and {maxWidth:0.0}.");
                return;
            }

            if (height < minHeight || height > maxHeight)
            {
                errors.Add($"RectTransform '{path}' height is {height:0.0}, expected between {minHeight:0.0} and {maxHeight:0.0}.");
                return;
            }

            passes.Add($"RectTransform '{path}' size {width:0.0}x{height:0.0} is within expected range.");
        }

        private static void ValidateWithinCanvas(string path, RectTransform canvasRect, List<string> errors, List<string> passes)
        {
            RectTransform rect = FindSceneRectTransform(path);
            if (rect == null || canvasRect == null)
            {
                if (rect == null) errors.Add($"RectTransform '{path}' is missing for offscreen check.");
                return;
            }

            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, rect);
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 canvasMin = canvasRect.rect.min;
            Vector3 canvasMax = canvasRect.rect.max;

            float tolerance = 2.0f;

            if (min.x < canvasMin.x - tolerance || max.x > canvasMax.x + tolerance ||
                min.y < canvasMin.y - tolerance || max.y > canvasMax.y + tolerance)
            {
                errors.Add($"RectTransform '{path}' is offscreen. Relative bounds: min={min}, max={max}. Canvas bounds: min={canvasMin}, max={canvasMax}.");
            }
            else
            {
                passes.Add($"RectTransform '{path}' is inside canvas bounds.");
            }
        }

        private static void ValidateNoOverlap(string pathA, string pathB, RectTransform canvasRect, List<string> errors, List<string> passes)
        {
            RectTransform rectA = FindSceneRectTransform(pathA);
            RectTransform rectB = FindSceneRectTransform(pathB);
            if (rectA == null || rectB == null || canvasRect == null) return;

            Bounds boundsA = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, rectA);
            Bounds boundsB = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, rectB);

            float areaA = boundsA.size.x * boundsA.size.y;
            float areaB = boundsB.size.x * boundsB.size.y;

            if (areaA < 0.001f)
            {
                errors.Add($"Overlap check failed: RectTransform '{pathA}' has zero or collapsed area ({boundsA.size.x:0.0}x{boundsA.size.y:0.0}).");
                return;
            }
            if (areaB < 0.001f)
            {
                errors.Add($"Overlap check failed: RectTransform '{pathB}' has zero or collapsed area ({boundsB.size.x:0.0}x{boundsB.size.y:0.0}).");
                return;
            }

            bool overlaps = boundsA.min.x < boundsB.max.x && boundsA.max.x > boundsB.min.x &&
                            boundsA.min.y < boundsB.max.y && boundsA.max.y > boundsB.min.y;

            if (overlaps)
            {
                float overlapWidth = Mathf.Min(boundsA.max.x, boundsB.max.x) - Mathf.Max(boundsA.min.x, boundsB.min.x);
                float overlapHeight = Mathf.Min(boundsA.max.y, boundsB.max.y) - Mathf.Max(boundsA.min.y, boundsB.min.y);
                float overlapArea = overlapWidth * overlapHeight;

                float ratioA = overlapArea / areaA;
                float ratioB = overlapArea / areaB;

                if (ratioA > 0.1f || ratioB > 0.1f)
                {
                    errors.Add($"Severe overlap detected between '{pathA}' and '{pathB}'. Overlap ratio: {Mathf.Max(ratioA, ratioB) * 100f:0.0}%.");
                    return;
                }
            }

            passes.Add($"No severe overlap between '{pathA}' and '{pathB}'.");
        }

        private static void ValidateWorldSpriteBounds(string objectName, float minHeight, float maxHeight, List<string> errors, List<string> passes)
        {
            var go = GameObject.Find(objectName);
            if (go == null)
            {
                errors.Add($"World object '{objectName}' is missing.");
                return;
            }

            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                errors.Add($"World object '{objectName}' has no SpriteRenderer.");
                return;
            }

            if (renderer.sprite == null)
            {
                errors.Add($"World object '{objectName}' has no sprite assigned to SpriteRenderer.");
                return;
            }

            Bounds bounds = renderer.bounds;
            float renderedHeight = bounds.size.y;

            if (renderedHeight < minHeight || renderedHeight > maxHeight)
            {
                errors.Add($"World object '{objectName}' rendered height is {renderedHeight:0.00} units, expected between {minHeight:0.00} and {maxHeight:0.00}.");
                return;
            }

            passes.Add($"World object '{objectName}' rendered height is {renderedHeight:0.00} units, within expected range.");

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 center = bounds.center;
                Vector3 viewportPos = cam.WorldToViewportPoint(center);
                bool isVisible = viewportPos.x >= 0f && viewportPos.x <= 1f && viewportPos.y >= 0f && viewportPos.y <= 1f && viewportPos.z > 0f;

                if (!isVisible)
                {
                    errors.Add($"World object '{objectName}' center is not visible in the camera viewport (ViewportPos: {viewportPos}).");
                }
                else
                {
                    passes.Add($"World object '{objectName}' is visible in main camera.");
                }

                float cameraHeightSpan = cam.orthographicSize * 2f;
                float heightRatio = renderedHeight / cameraHeightSpan;
                if (heightRatio > 0.45f)
                {
                    errors.Add($"World object '{objectName}' rendered height covers {heightRatio * 100f:0.0}% of camera vertical span (expected <= 45%).");
                }
                else
                {
                    passes.Add($"World object '{objectName}' vertical span ratio ({heightRatio * 100f:0.0}%) is within limit.");
                }
            }
        }

        private static void ValidateHomeScreenShell(List<string> errors, List<string> warnings, List<string> passes)
        {
            RectTransform canvasRect = FindSceneRectTransform("Canvas");
            if (canvasRect == null) return;

            ValidateWithinCanvas("Canvas/UI_Root/Header_Panel", canvasRect, errors, passes);
            ValidateWithinCanvas("Canvas/UI_Root/Sidebar_Panel", canvasRect, errors, passes);
            ValidateWithinCanvas("Canvas/UI_Root/Gameplay_Area", canvasRect, errors, passes);

            string[] hudIcons = {
                "Canvas/UI_Root/Header_Panel/Coins_Text/Coin_Icon",
                "Canvas/UI_Root/Header_Panel/Seeds_Text/Seeds_Icon",
                "Canvas/UI_Root/Header_Panel/Crops_Text/Crops_Icon",
                "Canvas/UI_Root/Header_Panel/Autosave_Icon",
                "Canvas/UI_Root/Header_Panel/Progression_HUD/Level_Text/Level_Star_Icon"
            };

            foreach (var path in hudIcons)
            {
                ValidateImagePolicy(path, Image.Type.Simple, true, errors, passes);
                ValidateRectSizeRange(path, 22f, 36f, 22f, 36f, errors, passes);
            }

            string[] sidebarButtons = {
                "Canvas/UI_Root/Sidebar_Panel/Quest_Button",
                "Canvas/UI_Root/Sidebar_Panel/Shop_Button"
            };
            foreach (var btn in sidebarButtons)
            {
                ValidateRectSizeRange(btn, 0f, 250f, 48f, 72f, errors, passes);
                ValidateImagePolicy(btn, Image.Type.Sliced, false, errors, passes);
            }

            string[] sidebarIcons = {
                "Canvas/UI_Root/Sidebar_Panel/Quest_Button/Quest_Icon",
                "Canvas/UI_Root/Sidebar_Panel/Shop_Button/Shop_Icon"
            };
            foreach (var icon in sidebarIcons)
            {
                ValidateImagePolicy(icon, Image.Type.Simple, true, errors, passes);
                ValidateRectSizeRange(icon, 28f, 44f, 28f, 44f, errors, passes);
            }

            ValidateNoOverlap("Canvas/UI_Root/Header_Panel", "Canvas/UI_Root/Gameplay_Area", canvasRect, errors, passes);
            ValidateNoOverlap("Canvas/UI_Root/Sidebar_Panel", "Canvas/UI_Root/Gameplay_Area/Farm_Plot", canvasRect, errors, passes);
            ValidateNoOverlap("Canvas/UI_Root/Sidebar_Panel", "Canvas/UI_Root/Gameplay_Area/Animal_Pen", canvasRect, errors, passes);
            ValidateNoOverlap("Canvas/UI_Root/Sidebar_Panel", "Canvas/UI_Root/Gameplay_Area/StickerBook_Panel", canvasRect, errors, passes);
        }

        private static void ValidateHomeGameplayWidgets(List<string> errors, List<string> warnings, List<string> passes)
        {
            RectTransform canvasRect = FindSceneRectTransform("Canvas");
            if (canvasRect == null) return;

            ValidateRectSizeRange("Canvas/UI_Root/Gameplay_Area/Farm_Plot", 300f, 380f, 380f, 520f, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Gameplay_Area/Animal_Pen", 300f, 380f, 380f, 520f, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Gameplay_Area/StickerBook_Panel", 420f, 560f, 420f, 560f, errors, passes);

            ValidateNoOverlap("Canvas/UI_Root/Gameplay_Area/Farm_Plot", "Canvas/UI_Root/Gameplay_Area/Animal_Pen", canvasRect, errors, passes);
            ValidateNoOverlap("Canvas/UI_Root/Gameplay_Area/Farm_Plot", "Canvas/UI_Root/Gameplay_Area/StickerBook_Panel", canvasRect, errors, passes);
            ValidateNoOverlap("Canvas/UI_Root/Gameplay_Area/Animal_Pen", "Canvas/UI_Root/Gameplay_Area/StickerBook_Panel", canvasRect, errors, passes);

            ValidateImagePolicy("Canvas/UI_Root/Gameplay_Area/Farm_Plot/Crop_Visual", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Gameplay_Area/Farm_Plot/Crop_Visual", 110f, 180f, 110f, 180f, errors, passes);

            ValidateImagePolicy("Canvas/UI_Root/Gameplay_Area/Farm_Plot/Watering_Can", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Gameplay_Area/Farm_Plot/Watering_Can", 50f, 80f, 50f, 80f, errors, passes);

            ValidateImagePolicy("Canvas/UI_Root/Gameplay_Area/Farm_Plot/Water_Status_Icon", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Gameplay_Area/Farm_Plot/Water_Status_Icon", 28f, 40f, 28f, 40f, errors, passes);

            ValidateImagePolicy("Canvas/UI_Root/Gameplay_Area/Animal_Pen/Animal_Visual", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Gameplay_Area/Animal_Pen/Animal_Visual", 110f, 180f, 110f, 180f, errors, passes);

            ValidateImagePolicy("Canvas/Prefabs_Holder/Heart_Feedback_Template", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange("Canvas/Prefabs_Holder/Heart_Feedback_Template", 32f, 56f, 32f, 56f, errors, passes);

            ValidateImagePolicy("Canvas/UI_Root/Gameplay_Area/StickerBook_Panel", Image.Type.Sliced, false, errors, passes);

            ValidateImagePolicy("Canvas/UI_Root/Gameplay_Area/StickerBook_Panel/Prev_Button", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Gameplay_Area/StickerBook_Panel/Prev_Button", 36f, 64f, 36f, 64f, errors, passes);

            ValidateImagePolicy("Canvas/UI_Root/Gameplay_Area/StickerBook_Panel/Next_Button", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Gameplay_Area/StickerBook_Panel/Next_Button", 36f, 64f, 36f, 64f, errors, passes);

            ValidateImagePolicy("Canvas/UI_Root/Gameplay_Area/StickerBook_Panel/Trash_Can", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Gameplay_Area/StickerBook_Panel/Trash_Can", 36f, 64f, 36f, 64f, errors, passes);

            ValidateImagePolicy("Canvas/Prefabs_Holder/Sticker_Template/Visual_Image", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange("Canvas/Prefabs_Holder/Sticker_Template/Visual_Image", 70f, 105f, 70f, 105f, errors, passes);

            ValidateImagePolicy("Canvas/Prefabs_Holder/Sticker_Template/Shadow_Offset", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange("Canvas/Prefabs_Holder/Sticker_Template/Shadow_Offset", 70f, 105f, 70f, 105f, errors, passes);

            var trayRect = FindSceneRectTransform("Canvas/UI_Root/Inventory_Tray");
            if (trayRect != null)
            {
                var grid = trayRect.GetComponent<GridLayoutGroup>();
                if (grid != null)
                {
                    float cellW = grid.cellSize.x;
                    float cellH = grid.cellSize.y;
                    if (cellW < 90f || cellW > 130f || cellH < 90f || cellH > 130f)
                    {
                        errors.Add($"Inventory tray cellSize is {cellW}x{cellH}, expected between 90x90 and 130x130.");
                    }
                    else
                    {
                        passes.Add("Inventory tray grid cellSize is within expected range.");
                    }
                }
            }
        }

        private static void ValidateHomeWorldObjectsAndCamera(List<string> errors, List<string> warnings, List<string> passes)
        {
            ValidateWorldSpriteBounds("NPC_BaNgoai", 3.1f, 3.8f, errors, passes);
            ValidateWorldSpriteBounds("Quest_Board", 1.2f, 1.8f, errors, passes);
            ValidateWorldSpriteBounds("Shop_Stall", 1.4f, 2.0f, errors, passes);
        }

        private static void ValidateQuestHudAndPopup(List<string> errors, List<string> warnings, List<string> passes)
        {
            RectTransform canvasRect = FindSceneRectTransform("Canvas");
            if (canvasRect == null) return;

            // Quest Popup Content Panel
            ValidateWithinCanvas("Canvas/UI_Root/Quest_Popup/Content_Panel", canvasRect, errors, passes);
            ValidateImagePolicy("Canvas/UI_Root/Quest_Popup/Content_Panel", Image.Type.Sliced, false, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Quest_Popup/Content_Panel", 450f, 550f, 450f, 550f, errors, passes);

            // Close button
            ValidateRectSizeRange("Canvas/UI_Root/Quest_Popup/Content_Panel/Close_Button", 36f, 44f, 36f, 44f, errors, passes);
            ValidateImagePolicy("Canvas/UI_Root/Quest_Popup/Content_Panel/Close_Button", Image.Type.Simple, true, errors, passes);

            // Quest Item Template
            string templatePath = "Canvas/UI_Root/Quest_Popup/Content_Panel/Quest_Items_List/Quest_Item_Template";
            ValidateRectSizeRange(templatePath, 380f, 420f, 50f, 70f, errors, passes);
            ValidateImagePolicy(templatePath + "/Bg_Image", Image.Type.Sliced, false, errors, passes);
            ValidateImagePolicy(templatePath + "/Type_Icon", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange(templatePath + "/Type_Icon", 28f, 56f, 28f, 56f, errors, passes);
            ValidateImagePolicy(templatePath + "/Stamp_Overlay", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange(templatePath + "/Stamp_Overlay", 28f, 56f, 28f, 56f, errors, passes);
        }

        private static void ValidateShopPopup(List<string> errors, List<string> warnings, List<string> passes)
        {
            RectTransform canvasRect = FindSceneRectTransform("Canvas");
            if (canvasRect == null) return;

            // Shop Popup Content Panel
            ValidateWithinCanvas("Canvas/UI_Root/Shop_Popup/Content_Panel", canvasRect, errors, passes);
            ValidateImagePolicy("Canvas/UI_Root/Shop_Popup/Content_Panel", Image.Type.Sliced, false, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Shop_Popup/Content_Panel", 800f, 900f, 550f, 650f, errors, passes);

            // Close button
            ValidateRectSizeRange("Canvas/UI_Root/Shop_Popup/Content_Panel/Close_Button", 36f, 44f, 36f, 44f, errors, passes);
            ValidateImagePolicy("Canvas/UI_Root/Shop_Popup/Content_Panel/Close_Button", Image.Type.Simple, true, errors, passes);

            // Shop tabs
            string[] tabs = {
                "Canvas/UI_Root/Shop_Popup/Content_Panel/Tab_Seeds",
                "Canvas/UI_Root/Shop_Popup/Content_Panel/Tab_Stickers",
                "Canvas/UI_Root/Shop_Popup/Content_Panel/Tab_Crops"
            };
            foreach (var tab in tabs)
            {
                ValidateImagePolicy(tab, Image.Type.Sliced, false, errors, passes);
                ValidateRectSizeRange(tab, 100f, 140f, 30f, 45f, errors, passes);
            }

            // Shop Item Template
            string templatePath = "Canvas/Prefabs_Holder/Shop_Item_Template";
            ValidateRectSizeRange(templatePath, 180f, 220f, 70f, 90f, errors, passes);
            ValidateImagePolicy(templatePath + "/Item_Icon", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange(templatePath + "/Item_Icon", 40f, 60f, 40f, 60f, errors, passes);
        }

        private static void ValidateDialoguePopup(List<string> errors, List<string> warnings, List<string> passes)
        {
            RectTransform canvasRect = FindSceneRectTransform("Canvas");
            if (canvasRect == null) return;

            // Content Panel
            ValidateWithinCanvas("Canvas/UI_Root/Dialogue_Popup/Content_Panel", canvasRect, errors, passes);
            ValidateImagePolicy("Canvas/UI_Root/Dialogue_Popup/Content_Panel", Image.Type.Sliced, false, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Dialogue_Popup/Content_Panel", 750f, 850f, 160f, 200f, errors, passes);

            // Portrait
            ValidateImagePolicy("Canvas/UI_Root/Dialogue_Popup/Content_Panel/Portrait", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Dialogue_Popup/Content_Panel/Portrait", 120f, 160f, 120f, 160f, errors, passes);

            // Indicator
            ValidateImagePolicy("Canvas/UI_Root/Dialogue_Popup/Content_Panel/Dialogue_Indicator", Image.Type.Simple, true, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Dialogue_Popup/Content_Panel/Dialogue_Indicator", 20f, 30f, 20f, 30f, errors, passes);
        }

        private static void ValidateDiaryAndScrapbook(List<string> errors, List<string> warnings, List<string> passes)
        {
            RectTransform canvasRect = FindSceneRectTransform("Canvas");
            if (canvasRect == null) return;

            // Diary Input Popup
            ValidateWithinCanvas("Canvas/UI_Root/Diary_Input_Popup/Content_Panel", canvasRect, errors, passes);
            ValidateImagePolicy("Canvas/UI_Root/Diary_Input_Popup/Content_Panel", Image.Type.Sliced, false, errors, passes);
            ValidateRectSizeRange("Canvas/UI_Root/Diary_Input_Popup/Content_Panel", 400f, 440f, 240f, 280f, errors, passes);

            // Cancel/Close Button
            ValidateRectSizeRange("Canvas/UI_Root/Diary_Input_Popup/Content_Panel/Cancel_Button", 100f, 140f, 30f, 50f, errors, passes);

            // Diary Note Template
            ValidateImagePolicy("Canvas/Prefabs_Holder/Diary_Note_Template", Image.Type.Sliced, false, errors, passes);
            ValidateRectSizeRange("Canvas/Prefabs_Holder/Diary_Note_Template", 130f, 170f, 100f, 140f, errors, passes);
        }

        private static void ValidateAggregateVisualLayout(List<string> errors, List<string> warnings, List<string> passes)
        {
            passes.Add("[Skeleton] ValidateAggregateVisualLayout shell executed.");
        }
    }
}

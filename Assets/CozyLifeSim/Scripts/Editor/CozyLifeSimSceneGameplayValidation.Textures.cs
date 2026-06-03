using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CozyLifeSim.Editor
{
    public static partial class CozyLifeSimSceneGameplayValidation
    {
        private static void ValidateTextureImporterMemorySettings(List<string> errors, List<string> passed)
        {
            string folderPath = "Assets/CozyLifeSim/Textures/Heritage";
            if (!System.IO.Directory.Exists(folderPath))
            {
                errors.Add($"Heritage textures folder not found at '{folderPath}'.");
                return;
            }

            string[] pngFiles = System.IO.Directory.GetFiles(folderPath, "*.png");
            foreach (string file in pngFiles)
            {
                string assetPath = file.Replace("\\", "/");
                string fileName = System.IO.Path.GetFileName(assetPath);

                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    errors.Add($"TextureImporter not found for '{fileName}'.");
                    continue;
                }

                // Check 1: isReadable == false
                if (importer.isReadable)
                {
                    errors.Add($"Asset '{fileName}' has isReadable == true. Must be false for memory optimization.");
                }
                else
                {
                    passed.Add($"Asset '{fileName}' has isReadable == false.");
                }

                // Check 2: maxTextureSize matches policy
                int expectedMaxSize = GetExpectedMaxSize(fileName);
                if (importer.maxTextureSize != expectedMaxSize)
                {
                    errors.Add($"Asset '{fileName}' maxTextureSize is {importer.maxTextureSize}, expected {expectedMaxSize}.");
                }
                else
                {
                    passed.Add($"Asset '{fileName}' has correct maxTextureSize of {expectedMaxSize}.");
                }

                // Check 3: Sliced assets have non-zero borders
                Vector4 expectedBorder = GetExpectedBorder(fileName);
                Vector4 actualBorder = Vector4.zero;
                if (importer.spriteImportMode == SpriteImportMode.Multiple)
                {
                    if (importer.spritesheet != null && importer.spritesheet.Length > 0)
                    {
                        actualBorder = importer.spritesheet[0].border;
                    }
                }
                else
                {
                    actualBorder = importer.spriteBorder;
                }

                if (expectedBorder != Vector4.zero)
                {
                    if (actualBorder == Vector4.zero)
                    {
                        errors.Add($"Sliced asset '{fileName}' expected non-zero border, but got zero border.");
                    }
                    else if (actualBorder != expectedBorder)
                    {
                        errors.Add($"Sliced asset '{fileName}' border mismatch! Expected: {expectedBorder}, got: {actualBorder}.");
                    }
                    else
                    {
                        passed.Add($"Sliced asset '{fileName}' has correct border {actualBorder}.");
                    }
                }
                else
                {
                    if (actualBorder != Vector4.zero)
                    {
                        errors.Add($"Non-sliced asset '{fileName}' expected zero border, but got {actualBorder}.");
                    }
                }

                // Check 4: AutoTrim matches policy
                bool expectedTrim = GetExpectedTrim(fileName);
                if (expectedTrim)
                {
                    Rect expectedRect = ComputeAlphaBounds(assetPath);
                    bool expectsMultiple = (expectedRect.width < 1024 || expectedRect.height < 1024);

                    if (expectsMultiple)
                    {
                        if (importer.spriteImportMode != SpriteImportMode.Multiple)
                        {
                            errors.Add($"Trimmed asset '{fileName}' expected Multiple mode due to trim bounds {expectedRect}, but got '{importer.spriteImportMode}'.");
                        }
                        else
                        {
                            if (importer.spritesheet != null && importer.spritesheet.Length > 0)
                            {
                                Rect actualRect = importer.spritesheet[0].rect;
                                if (actualRect != expectedRect)
                                {
                                    errors.Add($"Trimmed asset '{fileName}' rect mismatch! Expected: {expectedRect}, got: {actualRect}.");
                                }
                                else
                                {
                                    passed.Add($"Trimmed asset '{fileName}' rect {actualRect} matches trim bounds.");
                                }
                            }
                            else
                            {
                                errors.Add($"Trimmed asset '{fileName}' has Multiple mode but empty spritesheet.");
                            }
                        }
                    }
                    else
                    {
                        if (importer.spriteImportMode != SpriteImportMode.Single)
                        {
                            errors.Add($"Trimmed asset '{fileName}' has no trim bounds, expected Single, but got '{importer.spriteImportMode}'.");
                        }
                        else
                        {
                            passed.Add($"Trimmed asset '{fileName}' has full rect (no alpha bounds to trim) and is Single.");
                        }
                    }
                }
                else
                {
                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        errors.Add($"Non-trimmed asset '{fileName}' has spriteImportMode '{importer.spriteImportMode}', expected Single.");
                    }
                    else
                    {
                        passed.Add($"Non-trimmed asset '{fileName}' has correct Single import mode.");
                    }
                }
            }
        }

        private static int GetExpectedMaxSize(string fileName)
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

        private static bool GetExpectedTrim(string fileName)
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

        private static Vector4 GetExpectedBorder(string fileName)
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

        private static Rect ComputeAlphaBounds(string assetPath)
        {
            if (!System.IO.File.Exists(assetPath))
                return new Rect(0, 0, 1024, 1024);

            try
            {
                byte[] bytes = System.IO.File.ReadAllBytes(assetPath);
                Texture2D tempTex = new Texture2D(2, 2);
                tempTex.LoadImage(bytes);

                int width = tempTex.width;
                int height = tempTex.height;
                Rect rect = new Rect(0, 0, width, height);

                Color32[] pixels = tempTex.GetPixels32();
                int minX = width;
                int maxX = -1;
                int minY = height;
                int maxY = -1;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[y * width + x].a > 0)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                if (maxX != -1)
                {
                    rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
                }

                UnityEngine.Object.DestroyImmediate(tempTex);
                return rect;
            }
            catch
            {
                return new Rect(0, 0, 1024, 1024);
            }
        }
    }
}

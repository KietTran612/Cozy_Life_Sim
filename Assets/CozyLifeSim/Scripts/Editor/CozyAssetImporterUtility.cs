#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CozyLifeSim.Editor
{
    public static class CozyAssetImporterUtility
    {
        public static void ConfigureAsSprite(string assetPath)
        {
            // Ensure the asset database is aware of the file
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                if (importer.spriteImportMode == SpriteImportMode.None)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }
                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        public static void ConfigureAsSpriteWithBorder(string assetPath, Vector4 border, int maxTextureSize = 1024, bool autoTrim = true)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            // Load texture directly from disk to scan original pixels
            int originalWidth = 1024;
            int originalHeight = 1024;
            Rect spriteRect = new Rect(0, 0, originalWidth, originalHeight);

            if (System.IO.File.Exists(assetPath))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(assetPath);
                Texture2D tempTex = new Texture2D(2, 2);
                if (tempTex.LoadImage(bytes))
                {
                    originalWidth = tempTex.width;
                    originalHeight = tempTex.height;
                    spriteRect = new Rect(0, 0, originalWidth, originalHeight);

                    if (autoTrim)
                    {
                        Color32[] pixels = tempTex.GetPixels32();
                        int minX = originalWidth;
                        int maxX = -1;
                        int minY = originalHeight;
                        int maxY = -1;

                        for (int y = 0; y < originalHeight; y++)
                        {
                            for (int x = 0; x < originalWidth; x++)
                            {
                                if (pixels[y * originalWidth + x].a > 0)
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
                            spriteRect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
                        }
                    }
                }
                UnityEngine.Object.DestroyImmediate(tempTex);
            }

            // Clamp borders
            Vector4 safeBorder = border;
            if (border.x + border.z >= spriteRect.width || border.y + border.w >= spriteRect.height)
            {
                Debug.LogWarning($"[CozyAssetImporter] Invalid border {border} for sprite '{assetPath}' (rect size: {spriteRect.width}x{spriteRect.height}). Border disabled to prevent broken sliced meshes.");
                safeBorder = Vector4.zero;
            }

            // Set final settings
            importer.textureType = TextureImporterType.Sprite;

            bool isTrimmed = autoTrim && (spriteRect.x > 0 || spriteRect.y > 0 || spriteRect.width < originalWidth || spriteRect.height < originalHeight);
            if (isTrimmed)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;

                SpriteMetaData[] sheet = new SpriteMetaData[1];
                sheet[0] = new SpriteMetaData();
                sheet[0].name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                sheet[0].rect = spriteRect;
                sheet[0].border = safeBorder;
                sheet[0].alignment = 0; // Center

                importer.spritesheet = sheet;
            }
            else
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = safeBorder;
            }

            importer.alphaIsTransparency = true;
            importer.isReadable = false;

            // Set target max size
            TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
            defaultSettings.maxTextureSize = maxTextureSize;
            importer.SetPlatformTextureSettings(defaultSettings);

            importer.SaveAndReimport();
        }
    }
}
#endif

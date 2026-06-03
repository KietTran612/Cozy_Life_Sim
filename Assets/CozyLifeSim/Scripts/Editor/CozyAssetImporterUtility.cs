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
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            // Save original compression to restore later
            TextureImporterCompression originalCompression = importer.textureCompression;

            // Pass 1: Make it readable, uncompressed, and full resolution (1024) to read pixels
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            
            TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
            int originalPlatformMaxSize = defaultSettings.maxTextureSize;
            defaultSettings.maxTextureSize = 1024;
            importer.SetPlatformTextureSettings(defaultSettings);
            
            importer.SaveAndReimport();

            // Load texture to scan pixels
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Rect spriteRect = new Rect(0, 0, 1024, 1024);
            if (tex != null)
            {
                int width = tex.width;
                int height = tex.height;
                spriteRect = new Rect(0, 0, width, height);

                if (autoTrim)
                {
                    Color32[] pixels = tex.GetPixels32();
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
                        spriteRect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
                    }
                }
            }

            // Clamp borders
            Vector4 safeBorder = border;
            if (border.x + border.z >= spriteRect.width || border.y + border.w >= spriteRect.height)
            {
                Debug.LogWarning($"[CozyAssetImporter] Invalid border {border} for sprite '{assetPath}' (rect size: {spriteRect.width}x{spriteRect.height}). Border disabled to prevent broken sliced meshes.");
                safeBorder = Vector4.zero;
            }

            // Pass 2: Set settings, mode, and reimport
            importer.textureType = TextureImporterType.Sprite;

            if (autoTrim && tex != null && (spriteRect.x > 0 || spriteRect.y > 0 || spriteRect.width < tex.width || spriteRect.height < tex.height))
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
            importer.textureCompression = originalCompression;

            // Restore target max size
            defaultSettings.maxTextureSize = maxTextureSize;
            importer.SetPlatformTextureSettings(defaultSettings);

            importer.SaveAndReimport();
        }
    }
}
#endif

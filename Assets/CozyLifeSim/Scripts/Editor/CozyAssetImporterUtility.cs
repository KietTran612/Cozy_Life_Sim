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
                if (importer.textureType != TextureImporterType.Sprite ||
                    importer.spriteImportMode != SpriteImportMode.Single ||
                    !importer.alphaIsTransparency)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
#endif

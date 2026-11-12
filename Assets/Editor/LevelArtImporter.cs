using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Настраивает импорт сгенерированных ассетов первого уровня.</summary>
public static class LevelArtImporter
{
    private const string Root = "Assets/Resources/Level01";

    [MenuItem("Tools/Hamster/Configure Level Art")]
    public static void Configure()
    {
        if (!Directory.Exists(Root))
        {
            Debug.LogError("[LevelArt] Нет папки " + Root);
            return;
        }

        string[] files = Directory.GetFiles(Root, "*.png", SearchOption.TopDirectoryOnly);
        foreach (string path in files)
        {
            string assetPath = path.Replace('\\', '/');
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 1;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[LevelArt] Импорт ассетов первого уровня настроен.");
    }
}

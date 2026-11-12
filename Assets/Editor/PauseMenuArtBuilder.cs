using UnityEditor;
using UnityEngine;

/// <summary>Настраивает импорт доски для меню паузы.</summary>
public static class PauseMenuArtBuilder
{
    private const string BoardPath = "Assets/Resources/UI/PauseMenu_Signboard.png";

    [MenuItem("Tools/Hamster/Configure Pause Menu Art")]
    public static void Configure()
    {
        AssetDatabase.ImportAsset(BoardPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(BoardPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[PauseMenu] Не найден импортер доски: " + BoardPath);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        Debug.Log("[PauseMenu] Импорт доски настроен.");
    }
}

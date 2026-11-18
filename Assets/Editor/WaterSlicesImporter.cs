using UnityEditor;
using UnityEngine;

/// <summary>Импортирует нарезанные водные слайсы как спрайты (PPU 100, пивот по низу).</summary>
public static class WaterSlicesImporter
{
    private const string Folder = "Assets/Art/Level01/WaterSlices";

    [MenuItem("Tools/Хомяк/Арт: импортировать водные слайсы")]
    public static void Run()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });
        int changed = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            changed++;
        }

        Debug.Log("[WaterSlices] импортировано спрайтов: " + changed);
    }
}


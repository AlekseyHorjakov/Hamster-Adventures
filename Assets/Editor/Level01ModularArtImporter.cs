using UnityEditor;
using UnityEngine;

/// <summary>Единые настройки импорта новых модульных ассетов первого уровня.</summary>
public sealed class Level01ModularArtImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        bool underArt = assetPath.StartsWith("Assets/Art/Level01/");
        bool underRuntimeZone = assetPath.StartsWith("Assets/Resources/Level01/Zone1/");
        if (!underArt && !underRuntimeZone)
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = IsTileable(assetPath) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.spritePixelsPerUnit = 100f;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteExtrude = 1;
        settings.spriteAlignment = (int)SpriteAlignment.Center;

        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        if (name == "ground_singlelayer")
        {
            // 9-slice: сохраняем округлые края и верхнюю траву,
            // растягивая только центральную часть грунта.
            settings.spriteBorder = new Vector4(110f, 70f, 110f, 185f);
        }

        importer.SetTextureSettings(settings);
    }

    private static bool IsTileable(string path)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        return name.Contains("tile") || name.Contains("surface") || name.Contains("waterfall");
    }
}


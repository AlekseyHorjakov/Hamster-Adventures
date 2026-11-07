using UnityEditor;
using UnityEngine;

/// <summary>
/// Импортирует плашки здоровья и маски заливки как UI-спрайты:
/// полный прямоугольник, без сжатия, с прозрачностью.
/// Запуск: Tools/Hamster/Build Health Plates или batchmode
/// -executeMethod HealthPlateBuilder.BuildAll
/// </summary>
public static class HealthPlateBuilder
{
    private const string Folder = "Assets/Resources/UI/HealthPlates";
    private static readonly string[] Names =
    {
        "Hamster_HealthPlate",
        "Hamster_HealthFill",
        "Hamster_SuperFill",
        "Girl_HealthPlate",
        "Girl_HealthFill",
        "Girl_SuperFill"
    };

    [MenuItem("Tools/Hamster/Build Health Plates")]
    public static void BuildAll()
    {
        foreach (string name in Names)
        {
            string path = Folder + "/" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[HealthPlates] Не найден " + path);
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Debug.Log("[HealthPlates] " + name + " -> " +
                      (sprite != null ? sprite.rect.width + "x" + sprite.rect.height : "НЕ СПРАЙТ"));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HealthPlates] Готово: 6 спрайтов");
    }

    [MenuItem("Tools/Hamster/Check Health HUD")]
    public static void CheckHud()
    {
        foreach (string name in Names)
        {
            Sprite sprite = Resources.Load<Sprite>("UI/HealthPlates/" + name);
            Debug.Log("[HealthHud] " + name + " -> " +
                      (sprite != null ? sprite.rect.width + "x" + sprite.rect.height : "НЕ НАЙДЕН"));
        }

        GameObject go = new GameObject("HealthHudSmokeTest");
        HealthHud hud = go.AddComponent<HealthHud>();
        hud.Initialize();
        Canvas canvas = go.GetComponentInChildren<Canvas>();
        int plates = canvas != null ? canvas.transform.childCount : 0;
        Debug.Log("[HealthHud] Canvas=" + (canvas != null) + ", плашек=" + plates);
        UnityEngine.Object.DestroyImmediate(go);
    }
}

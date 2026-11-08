using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Собирает префаб горящего следа после рывка: 12 кадров (6 возгорание,
/// 6 затухание), SpriteRenderer на слое Ground и компонент FireTrail.
/// Подключает префаб в trailPrefab обоих героев.
///
/// Запуск: Tools/Hamster/Build Fire Trail или
/// Unity.exe -batchmode -quit -executeMethod FireTrailBuilder.BuildAll
/// </summary>
public static class FireTrailBuilder
{
    private const string ArtFolder = "Assets/Art/VFX/FireTrail";
    private const string PrefabPath = "Assets/VFX/Prefabs/FireTrail.prefab";
    private const int FrameCount = 12;
    private const float Ppu = 362f;
    private const float FrameSeconds = 0.1f;

    [MenuItem("Tools/Hamster/Build Fire Trail")]
    public static void BuildAll()
    {
        Sprite[] frames = ImportFrames();
        if (frames.Length != FrameCount)
        {
            Debug.LogError("[FireTrail] Не хватает кадров: " + frames.Length + " из " + FrameCount);
            return;
        }

        GameObject root = new GameObject("FireTrail");
        SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = frames[0];
        spriteRenderer.sortingLayerName = "Ground";
        spriteRenderer.sortingOrder = 1;

        FireTrail trail = root.AddComponent<FireTrail>();
        SerializedObject serialized = new SerializedObject(trail);
        serialized.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
        serialized.FindProperty("frameSeconds").floatValue = FrameSeconds;

        SerializedProperty array = serialized.FindProperty("frames");
        array.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];

        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            AssetDatabase.DeleteAsset(PrefabPath);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Wire("Assets/Prefabs/Hamster.prefab", prefab);
        Wire("Assets/Prefabs/HamsterGirl.prefab", prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FireTrail] Готово: " + FrameCount + " кадров, префаб " + PrefabPath);
    }

    private static Sprite[] ImportFrames()
    {
        Sprite[] frames = new Sprite[FrameCount];

        for (int i = 0; i < FrameCount; i++)
        {
            string path = ArtFolder + "/FireTrail_" + (i + 1).ToString("00") + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[FireTrail] Не найден " + path);
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Ppu;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            frames[i] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (frames[i] == null)
                Debug.LogError("[FireTrail] Не загрузился спрайт " + path);
        }

        return frames;
    }

    private static void Wire(string prefabPath, GameObject trailPrefab)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        AttackController attack = contents.GetComponent<AttackController>();

        if (attack == null)
        {
            Debug.LogError("[FireTrail] Нет AttackController в " + prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            return;
        }

        SerializedObject serialized = new SerializedObject(attack);
        serialized.FindProperty("trailPrefab").objectReferenceValue = trailPrefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}

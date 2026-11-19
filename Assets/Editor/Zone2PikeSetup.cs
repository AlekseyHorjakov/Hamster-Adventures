using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Зона II: тонущая секция моста Bridge_Section_02 и щука в промежутке до Bridge_Section_01.
/// Идемпотентно: повторный запуск обновляет объект щуки и не дублирует платформу.
/// </summary>
public static class Zone2PikeSetup
{
    private const string ScenePath = "Assets/Scenes/Level01_Full.unity";
    private const string PikeName = "Pike_CrossingGap";
    private const int PikeSortingOrder = 15;

    [MenuItem("Tools/Хомяк/Зона II: щука и тонущая секция")]
    public static void Run()
    {
        PikeSlicesImporter.Run();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        MarkSinkingSection();
        BuildPike();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Zone2Pike] готово: Bridge_Section_02 тонет, щука в промежутке 337.5–356.");
    }

    private static void MarkSinkingSection()
    {
        GameObject section = FindByName("Bridge_Section_02", 337.55f);
        if (section == null)
        {
            Debug.LogError("[Zone2Pike] не нашёл Bridge_Section_02 около x=337.5");
            return;
        }

        if (section.GetComponent<SinkingPlatform>() == null)
        {
            section.AddComponent<SinkingPlatform>();
            Debug.Log("[Zone2Pike] SinkingPlatform добавлен: " + section.name);
        }
    }

    private static void BuildPike()
    {
        Sprite[] frames = LoadFrames();
        if (frames.Length == 0)
        {
            Debug.LogError("[Zone2Pike] кадры щуки не загрузились из " + PikeSlicesImporter.Folder);
            return;
        }

        GameObject root = FindByName(PikeName, 0f, exactOnly: true);
        if (root == null)
            root = new GameObject(PikeName);

        // Низ кадра — линия воды (верх Z2_WaterSurface = -2.27), промежуток между секциями 343.2–349.7.
        root.transform.position = new Vector3(346.8f, -2.27f, 0f);
        root.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingLayerName = "Ground";
        sr.sortingOrder = PikeSortingOrder;
        sr.enabled = false;

        PikeLeap leap = root.GetComponent<PikeLeap>();
        if (leap == null)
            leap = root.AddComponent<PikeLeap>();

        var so = new SerializedObject(leap);
        so.FindProperty("visual").objectReferenceValue = sr;
        // Зона удара — на уровне моста: центры секций -0.65/-0.68, игрок идёт с ногами по -0.4.
        so.FindProperty("hitCenterOffset").vector2Value = new Vector2(0f, 1.75f);
        so.FindProperty("hitRadius").floatValue = 1.4f;

        SerializedProperty list = so.FindProperty("frames");
        list.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[Zone2Pike] щука: " + root.transform.position + ", кадров " + frames.Length);
    }

    private static Sprite[] LoadFrames()
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { PikeSlicesImporter.Folder });
        var frames = new Sprite[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[i]));

        // FindAssets не гарантирует порядок — сортируем по имени Pike_Jump_01..12.
        System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
        return frames;
    }

    private static GameObject FindByName(string name, float nearX, bool exactOnly = false)
    {
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.name != name)
                continue;
            if (!exactOnly && Mathf.Abs(go.transform.position.x - nearX) > 3f)
                continue;
            return go;
        }
        return null;
    }
}

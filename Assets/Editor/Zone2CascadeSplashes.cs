using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Три анимации «вода падает на камни» на водопаде: верхняя, средняя и нижняя.
/// Кадры берутся из нарезки каскадного листа (Cascade_1..3_01..06).
/// </summary>
public static class Zone2CascadeSplashes
{
    private const string ScenePath = "Assets/Scenes/Level01_Full.unity";
    private const string Slices = "Assets/Art/Level01/WaterSlices";

    private static readonly (string row, float fx, float fy, float widthFactor, float fps)[] Spots =
    {
        ("Cascade_1_", 0.532f, 0.313f, 0.30f, 10f),
        ("Cascade_2_", 0.160f, 0.467f, 0.24f, 10f),
        ("Cascade_3_", 0.712f, 0.641f, 0.34f, 10f)
    };

    [MenuItem("Tools/Хомяк/Зона II: анимации воды на водопаде")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject waterfall = GameObject.Find("Z2_Waterfall");
        if (waterfall == null)
        {
            Debug.LogWarning("[Cascade] Z2_Waterfall не найден");
            return;
        }

        var waterfallRenderer = waterfall.GetComponent<SpriteRenderer>();
        Bounds bounds = waterfallRenderer.bounds;

        Purge();

        for (int i = 0; i < Spots.Length; i++)
        {
            var spot = Spots[i];
            string[] frames = Enumerable.Range(1, 6)
                .Select(n => spot.row + n.ToString("00"))
                .ToArray();
            Sprite first = Load(frames[0]);
            if (first == null)
                continue;

            float x = bounds.min.x + bounds.size.x * spot.fx;
            float y = bounds.max.y - bounds.size.y * spot.fy;

            GameObject go = new GameObject("Z2C_Cascade_" + (i + 1));
            go.transform.position = new Vector3(x, y, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Ground";
            renderer.sortingOrder = 15;

            float width = bounds.size.x * spot.widthFactor;
            float scale = width / first.bounds.size.x;
            go.transform.localScale = Vector3.one * scale;

            Animate(go, renderer, frames, spot.fps);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Cascade] анимации воды расставлены: " + Spots.Length);
    }

    private static void Purge()
    {
        var old = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t != null && t.name.StartsWith("Z2C_"))
            .Select(t => t.gameObject)
            .ToArray();
        foreach (GameObject go in old)
            if (go != null)
                Object.DestroyImmediate(go);
    }

    private static void Animate(GameObject target, SpriteRenderer renderer, string[] frameNames, float fps)
    {
        Sprite[] frames = frameNames.Select(Load).Where(s => s != null).ToArray();
        if (frames.Length == 0)
            return;

        renderer.sprite = frames[0];
        SpriteFrameAnimator animator = target.GetComponent<SpriteFrameAnimator>();
        if (animator == null)
            animator = target.AddComponent<SpriteFrameAnimator>();

        SerializedObject so = new SerializedObject(animator);
        so.FindProperty("target").objectReferenceValue = renderer;
        so.FindProperty("framesPerSecond").floatValue = fps;
        so.FindProperty("loop").boolValue = true;
        SerializedProperty list = so.FindProperty("frames");
        list.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(Slices + "/" + name + ".png");
    }
}

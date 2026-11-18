using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Водный проход Зоны II: анимация водопада, брызги, туман, камни-платформы 07,
/// растительность в воде и озерный фон. Работает по текущей сцене, комнаты не пересобирает.
/// </summary>
public static class Zone2WaterPass
{
    private const string ScenePath = "Assets/Scenes/Level01_Full.unity";
    private const string Slices = "Assets/Art/Level01/WaterSlices";

    [MenuItem("Tools/Hamster/Вода и водопад Зоны II")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        PurgeOld();

        BuildWaterfallEffects();
        BuildShallowStones();
        BuildLakeDecor();
        BuildLakeBackdrop();
        RemoveDeadObjects();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[WaterPass] готово");
    }

    // ---------- водопад ----------

    private static void BuildWaterfallEffects()
    {
        GameObject waterfall = GameObject.Find("Z2_Waterfall");
        if (waterfall == null)
            return;

        var renderer = waterfall.GetComponent<SpriteRenderer>();
        string[] frames = { "Fall_01", "Fall_02", "Fall_03", "Fall_04" };
        Animate(waterfall, renderer, frames, 9f);

        Bounds b = renderer.bounds;
        float baseY = b.min.y + 0.3f;
        float centerX = b.center.x;

        // Брызги от удара воды о камни.
        GameObject splash = new GameObject("Z2W_Splash");
        splash.transform.position = new Vector3(centerX, baseY, 0f);
        var splashRenderer = splash.AddComponent<SpriteRenderer>();
        splashRenderer.sortingLayerName = "Ground";
        splashRenderer.sortingOrder = 12;
        splash.transform.localScale = Vector3.one * 3.2f;
        Animate(splash, splashRenderer, Sequence("Splash2_", 8), 12f);

        // Туман/дымка под брызгами.
        GameObject mist = new GameObject("Z2W_Mist");
        mist.transform.position = new Vector3(centerX, baseY - 0.9f, 0f);
        var mistRenderer = mist.AddComponent<SpriteRenderer>();
        mistRenderer.sortingLayerName = "Ground";
        mistRenderer.sortingOrder = 11;
        mistRenderer.color = new Color(0.85f, 0.95f, 1f, 0.4f);
        mist.transform.localScale = new Vector3(4.5f, 1.6f, 1f);
        Animate(mist, mistRenderer, Sequence("Mist_", 8), 5f);

        // Камни, в которые бьёт вода.
        string[] rocks = { "RiverRock_01", "RiverRock_02", "RiverRock_03", "RiverRock_04" };
        for (int i = 0; i < rocks.Length; i++)
        {
            Sprite sprite = Load(rocks[i]);
            if (sprite == null)
                continue;
            GameObject rock = new GameObject("Z2W_FallRock_" + (i + 1));
            rock.transform.position = new Vector3(centerX - 1.8f + i * 1.2f, baseY + 0.1f, 0f);
            var rockRenderer = rock.AddComponent<SpriteRenderer>();
            rockRenderer.sprite = sprite;
            rockRenderer.sortingLayerName = "Ground";
            rockRenderer.sortingOrder = 13;
            rock.transform.localScale = Vector3.one * (1.6f / sprite.bounds.size.y) * (i % 2 == 0 ? 1f : 1.25f);
        }
    }

    // ---------- камни 07 ----------

    private static void BuildShallowStones()
    {
        GameObject room = GameObject.Find("Room_07_Shallows");
        if (room == null)
            return;

        SetStoneVisual(room.transform, "ShoreStone", "Stone_13", 1.1f);
        SetStoneVisual(room.transform, "Stone_A", "Stone_14", 1.2f);
        SetStoneVisual(room.transform, "Stone_B", "Stone_15", 1.3f);
        SetStoneVisual(room.transform, "Stone_C", "Stone_16", 1.4f);

        AddStepStone(room.transform, "StepStone_1", 245.2f, 248.8f, 0.5f, "Stone_17");
        AddStepStone(room.transform, "StepStone_2", 261.2f, 265.8f, 1.75f, "Stone_18");

        GameObject islands = GameObject.Find("Room_08_Islands");
        if (islands != null)
        {
            SetStoneVisual(islands.transform, "Island_1", "Stone_08", 4.2f);
            SetStoneVisual(islands.transform, "Island_2", "Stone_09", 4.9f);
            SetStoneVisual(islands.transform, "Island_3", "Stone_10", 5.7f);
            SetStoneVisual(islands.transform, "Island_Final", "Stone_11", 3.5f);
        }

        GameObject falls = GameObject.Find("Room_10_Waterfall");
        if (falls != null)
        {
            SetStoneVisual(falls.transform, "FallLedge_1", "Stone_12", 2.6f);
            SetStoneVisual(falls.transform, "FallLedge_2", "Stone_19", 2.6f);
            SetStoneVisual(falls.transform, "FallLedge_3", "Stone_20", 2.6f);
        }
    }

    private static void SetStoneVisual(Transform room, string name, string spriteName, float height)
    {
        Transform stone = room.Find(name);
        if (stone == null)
            return;
        Collider2D col = stone.GetComponent<Collider2D>();
        if (col == null)
            return;

        Transform old = stone.Find("Visual");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        Sprite sprite = Load(spriteName);
        if (sprite == null)
            return;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(stone, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 12;

        float width = col.bounds.size.x;
        float scaleX = width / sprite.bounds.size.x;
        float scaleY = height / sprite.bounds.size.y;
        visual.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        // Верх спрайта ровно на верх коллайдера.
        float topLocal = col.bounds.max.y - stone.position.y;
        visual.transform.localPosition = new Vector3(0f, topLocal - height * 0.5f, 0f);
    }

    private static void AddStepStone(Transform room, string name, float left, float right, float top, string spriteName)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing);

        Sprite sprite = Load(spriteName);
        if (sprite == null)
            return;

        GameObject go = new GameObject(name);
        go.transform.SetParent(room, false);
        go.transform.position = new Vector3((left + right) * 0.5f, top - 0.3f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(right - left, 0.6f);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 12;
        float height = 1.2f;
        visual.transform.localScale = new Vector3((right - left) / sprite.bounds.size.x, height / sprite.bounds.size.y, 1f);
        visual.transform.localPosition = new Vector3(0f, 0.3f - height * 0.5f, 0f);
    }

    // ---------- декор ----------

    private static void BuildLakeDecor()
    {
        GameObject art = GameObject.Find("BakedZone2Art");
        if (art == null)
            return;

        // Камыш на берегу 06.
        Place(art.transform, "Reed_01", 213.5f, 0.1f, 1.3f, 14);
        Place(art.transform, "Reed_02", 227.5f, 0.1f, 1.1f, 14);
        Place(art.transform, "Reed_03", 235.5f, 0.1f, 1.2f, 14);

        // Кувшинки и водоросли в воде 07-08.
        float[] lilyX = { 243f, 255.5f, 267f, 286f, 300f };
        for (int i = 0; i < lilyX.Length; i++)
            Place(art.transform, "Lily_0" + (i % 3 + 1), lilyX[i], -1.15f, 0.28f, 10);

        float[] plantX = { 240f, 252.5f, 264f, 283f, 296f, 309f };
        for (int i = 0; i < plantX.Length; i++)
            Place(art.transform, "WaterPlant_0" + (i % 6 + 1), plantX[i], -1.3f, 0.9f, 9);

        Place(art.transform, "RiverRock_05", 249f, -1.2f, 0.7f, 10);
        Place(art.transform, "RiverRock_06", 262f, -1.2f, 0.6f, 10);
        Place(art.transform, "WaterPlant_07", 275f, -1.25f, 0.8f, 9);
        Place(art.transform, "WaterPlant_08", 291f, -1.25f, 0.85f, 9);
    }

    private static void Place(Transform parent, string spriteName, float x, float bottomY, float height, int order)
    {
        Sprite sprite = Load(spriteName);
        if (sprite == null)
            return;
        GameObject go = new GameObject("Z2W_" + spriteName + "_" + x.ToString("0"));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, bottomY + height * 0.5f, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = order;
        go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
    }

    // ---------- фон ----------

    private static void BuildLakeBackdrop()
    {
        Sprite lake = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Level01/Lake.png");
        if (lake == null)
            return;

        GameObject backdrop = new GameObject("Z2W_LakeBackdrop");
        backdrop.transform.position = new Vector3(294f, 0.1f, 0f);
        SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
        renderer.sprite = lake;
        renderer.sortingLayerName = "Mid";
        renderer.sortingOrder = -19;
        backdrop.transform.localScale = new Vector3(200f / lake.bounds.size.x, 22f / lake.bounds.size.y, 1f);
    }

    // ---------- прочее ----------

    private static void RemoveDeadObjects()
    {
        foreach (string name in new[] { "IslandGate", "Encounter_Островки", "Encounter_Мелководье" })
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
                Object.DestroyImmediate(go);
        }
    }

    private static void PurgeOld()
    {
        var old = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t != null && t.name.StartsWith("Z2W_"))
            .Select(t => t.gameObject)
            .ToArray();
        foreach (GameObject go in old)
            if (go != null)
                Object.DestroyImmediate(go);
    }

    private static void Animate(GameObject target, SpriteRenderer renderer, string[] frameNames, float fps)
    {
        if (renderer == null || frameNames == null || frameNames.Length == 0)
            return;

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

    private static string[] Sequence(string prefix, int count)
    {
        return Enumerable.Range(1, count).Select(i => prefix + i.ToString("00")).ToArray();
    }

    private static Sprite Load(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        return AssetDatabase.LoadAssetAtPath<Sprite>(Slices + "/" + name + ".png");
    }
}

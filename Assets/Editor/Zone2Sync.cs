using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Неразрушающая синхронизация Зоны II: подтягивает коллайдеры к визуалу платформ
/// (ровно один коллайдер на объект) и переносит бой на второй экран комнаты 06.
/// Не пересобирает комнаты, поэтому ручные перемещения объектов сохраняются.
/// </summary>
public static class Zone2Sync
{
    private const string ScenePath = "Assets/Scenes/Level01_Full.unity";

    private static readonly string[] Platforms =
    {
        "Island_1", "Island_2", "Island_3", "Island_Final",
        "CrossPlatform_1", "CrossPlatform_2", "CrossPlatform_3",
        "FallLedge_1", "FallLedge_2", "FallLedge_3",
        "Stone_A", "Stone_B", "Stone_C", "ShoreStone", "LakeDock"
    };

    private static readonly (string sprite, float fraction)[] FlatLines =
    {
        ("SteppingStone_01", 0.167f), ("SteppingStone_02", 0.104f), ("SteppingStone_03", 0.348f),
        ("SteppingStone_04", 0.368f), ("SteppingStone_05", 0.371f), ("SteppingStone_06", 0.083f),
        ("SteppingStone_07", 0.092f), ("SteppingStone_08", 0.091f),
        ("Platform_Wide_A", 0.232f), ("Platform_Wide_B", 0.182f)
    };

    [MenuItem("Tools/Hamster/Синхронизировать коллайдеры и бой Зоны II")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int fixedPlatforms = SyncPlatforms();
        int hyenas = ResetHyenas();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Zone2Sync] платформ подтянуто: " + fixedPlatforms + ", гиен в комнате 06: " + hyenas);
    }

    private static int SyncPlatforms()
    {
        int count = 0;
        foreach (string name in Platforms)
        {
            GameObject root = GameObject.Find(name);
            if (root == null)
                continue;

            SpriteRenderer visual = null;
            Transform visualTransform = root.transform.Find("Visual");
            if (visualTransform != null)
                visual = visualTransform.GetComponent<SpriteRenderer>();

            // Если визуал платформы лежит отдельно от коллайдера (док), ищем его в арте по имени.
            if (visual == null)
            {
                GameObject artRoot = GameObject.Find("BakedZone2Art");
                if (artRoot != null)
                {
                    Transform artVisual = artRoot.transform.Find(name + "_Visual");
                    if (artVisual != null)
                        visual = artVisual.GetComponent<SpriteRenderer>();
                }
            }

            foreach (Collider2D old in root.GetComponents<Collider2D>())
                Object.DestroyImmediate(old);

            if (visual == null || visual.sprite == null)
                continue;

            if (name == "LakeDock" || !IsRock(visual.sprite.name))
            {
                // Плоская доска: один прямоугольник по верху визуала.
                BoxCollider2D box = root.AddComponent<BoxCollider2D>();
                box.size = new Vector2(visual.bounds.size.x, 0.4f);
                box.offset = new Vector2(0f, 0.1f);
            }
            else
            {
                PolygonCollider2D shape = root.AddComponent<PolygonCollider2D>();
                shape.pathCount = 0;
                shape.CreateFromSprite(visual.sprite);
                if (shape.pathCount == 0)
                {
                    Object.DestroyImmediate(shape);
                    BoxCollider2D box = root.AddComponent<BoxCollider2D>();
                    box.size = new Vector2(visual.bounds.size.x, 0.4f);
                    box.offset = new Vector2(0f, 0.1f);
                }
                else
                {
                    float fraction = FlatFraction(visual.sprite.name);
                    float height = visual.sprite.bounds.size.y * visual.transform.lossyScale.y;
                    float flatLine = (0.5f - fraction) * height;
                    for (int path = 0; path < shape.pathCount; path++)
                    {
                        Vector2[] points = shape.GetPath(path);
                        for (int i = 0; i < points.Length; i++)
                        {
                            float y = Mathf.Min(points[i].y * visual.transform.lossyScale.y, flatLine);
                            points[i] = new Vector2(points[i].x * visual.transform.lossyScale.x, y);
                        }
                        shape.SetPath(path, points);
                    }

                    // Плоская линия травы = верх коллайдера (0.3 в локальных координатах корня).
                    shape.offset = new Vector2(visual.transform.localPosition.x,
                        0.3f - (0.5f - fraction) * height);
                }
            }

            count++;
        }

        return count;
    }

    private static int ResetHyenas()
    {
        // Убираем всех гиен Зоны II и мусор от прошлых тестов.
        var victims = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t != null)
            .Where(t => t.name.Contains("IslandHyena") || t.name.Contains("CrossHyena") ||
                        t.name.Contains("FallHyena") || t.name.Contains("ShallowsHyena") ||
                        t.name.Contains("BeachHyena") || t.name.StartsWith("Z2_Test"))
            .Select(t => t.gameObject)
            .ToArray();
        foreach (GameObject victim in victims)
            if (victim != null)
                Object.DestroyImmediate(victim);

        GameObject room06 = GameObject.Find("Room_06_LakeShore");
        if (room06 == null)
            return 0;

        GameObject oldEncounter = GameObject.Find("Encounter_Берег");
        if (oldEncounter != null)
            Object.DestroyImmediate(oldEncounter);

        var hyenas = new GameObject[3];
        float[] xs = { 232f, 234.5f, 237f };
        for (int i = 0; i < xs.Length; i++)
        {
            hyenas[i] = SpawnHyena(room06.transform, "BeachHyena_" + (i + 1), xs[i]);
            SetLeash(hyenas[i], 226f, 238f);
        }

        GameObject encounterObject = new GameObject("Encounter_Берег");
        encounterObject.transform.SetParent(room06.transform, false);
        encounterObject.transform.position = new Vector3(229.5f, 2.5f, 0f);
        BoxCollider2D trigger = encounterObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(16f, 10f);

        LevelFlowController level = Object.FindFirstObjectByType<LevelFlowController>();
        RoomDefinition room = AssetDatabase.LoadAssetAtPath<RoomDefinition>("Assets/Level/Rooms/Zone2/Room_06.asset");
        EncounterController encounter = encounterObject.AddComponent<EncounterController>();
        SerializedObject so = new SerializedObject(encounter);
        so.FindProperty("level").objectReferenceValue = level;
        so.FindProperty("room").objectReferenceValue = room;
        so.FindProperty("encounterName").stringValue = "Берег озера";
        SerializedProperty enemies = so.FindProperty("enemies");
        enemies.arraySize = hyenas.Length;
        for (int i = 0; i < hyenas.Length; i++)
            enemies.GetArrayElementAtIndex(i).objectReferenceValue = hyenas[i];
        SerializedProperty gates = so.FindProperty("gates");
        gates.arraySize = 0;
        so.ApplyModifiedPropertiesWithoutUndo();

        return hyenas.Length;
    }

    private static GameObject SpawnHyena(Transform parent, string name, float x)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hyena.prefab");
        if (prefab == null)
            return null;

        const float beachTop = 0.1f;
        GameObject hyena = Object.Instantiate(prefab);
        hyena.name = name;
        hyena.SetActive(false);
        hyena.transform.SetParent(parent, false);
        hyena.transform.position = new Vector3(x, beachTop + 2f, 0f);

        Collider2D col = hyena.GetComponent<Collider2D>();
        if (col != null)
        {
            float delta = beachTop - col.bounds.min.y;
            hyena.transform.position = new Vector3(x, hyena.transform.position.y + delta, 0f);
        }

        return hyena;
    }

    private static void SetLeash(GameObject enemy, float minX, float maxX)
    {
        if (enemy == null)
            return;
        Component ai = enemy.GetComponent("HyenaAI");
        if (ai == null)
            return;
        SerializedObject so = new SerializedObject(ai);
        so.FindProperty("leashMinX").floatValue = minX;
        so.FindProperty("leashMaxX").floatValue = maxX;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool IsRock(string spriteName) => spriteName.StartsWith("SteppingStone");

    private static float FlatFraction(string spriteName)
    {
        foreach (var entry in FlatLines)
            if (entry.sprite == spriteName)
                return entry.fraction;
        return 0.1f;
    }
}

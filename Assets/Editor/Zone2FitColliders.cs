using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Подгоняет коллайдеры под текущее (ручное) расположение платформ Зоны II,
/// добавляет коллайдеры новым мостам, чинит слой досок и настраивает тонущую доску.
/// Комнаты не пересобирает.
/// </summary>
public static class Zone2FitColliders
{
    private const string ScenePath = "Assets/Scenes/Level01_Full.unity";

    private static readonly (string name, float fraction)[] BridgePieces =
    {
        ("Bridge_Patch_04", 0.35f),
        ("Bridge_Section_01", 0.38f),
        ("Bridge_Section_02", 0.38f)
    };

    private static readonly string[] Platforms =
    {
        "Island_1", "Island_2", "Island_3", "Island_Final",
        "CrossPlatform_1", "CrossPlatform_2", "CrossPlatform_3",
        "FallLedge_1", "FallLedge_2", "FallLedge_3",
        "Stone_A", "Stone_B", "Stone_C", "ShoreStone", "StepStone_1", "StepStone_2", "LakeDock"
    };

    private static readonly (string sprite, float fraction)[] FlatLines =
    {
        ("SteppingStone_01", 0.167f), ("SteppingStone_02", 0.104f), ("SteppingStone_03", 0.348f),
        ("SteppingStone_04", 0.368f), ("SteppingStone_05", 0.371f), ("SteppingStone_06", 0.083f),
        ("SteppingStone_07", 0.092f), ("SteppingStone_08", 0.091f),
        ("Platform_Wide_A", 0.232f), ("Platform_Wide_B", 0.182f),
        ("Bridge_Section_01", 0.38f), ("Bridge_Section_02", 0.38f), ("Bridge_Patch_04", 0.35f)
    };

    [MenuItem("Tools/Хомяк/Зона II: подогнать коллайдеры и доски")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        int shifted = FitExistingPlatforms();
        int bridges = BuildBridgeColliders();
        SetupSinkingBoard();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FitColliders] платформ подтянуто: " + shifted + ", мостов: " + bridges);
    }

    private static int FitExistingPlatforms()
    {
        int count = 0;
        foreach (string name in Platforms)
        {
            GameObject root = GameObject.Find(name);
            if (root == null)
                continue;
            Collider2D col = root.GetComponent<Collider2D>();
            if (col == null)
                continue;

            SpriteRenderer visual = root.GetComponentInChildren<SpriteRenderer>(true);
            if (visual == null || visual.sprite == null)
                continue;

            float fraction = FlatFraction(visual.sprite.name);
            float flatY = visual.bounds.max.y - fraction * visual.bounds.size.y;
            Vector2 center = col.bounds.center;
            float dx = visual.bounds.center.x - center.x;
            float dy = flatY - col.bounds.max.y;
            if (Mathf.Abs(dx) < 0.05f && Mathf.Abs(dy) < 0.05f)
                continue;

            Vector3 pos = root.transform.position;
            Vector3 scale = root.transform.lossyScale;
            root.transform.position = new Vector3(pos.x + dx, pos.y + dy, pos.z);
            count++;
        }
        return count;
    }

    private static int BuildBridgeColliders()
    {
        int count = 0;
        foreach (var piece in BridgePieces)
        {
            GameObject go = GameObject.Find(piece.name);
            if (go == null)
                continue;

            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
                continue;

            // Доска/мост должны быть выше воды: слой Ground и порядок над водой.
            renderer.sortingLayerName = "Ground";
            renderer.sortingOrder = 13;

            foreach (Collider2D old in go.GetComponents<Collider2D>())
                Object.DestroyImmediate(old);

            Bounds bounds = renderer.bounds;
            float flatY = bounds.max.y - piece.fraction * bounds.size.y;
            float heightWorld = Mathf.Max(0.45f, bounds.max.y - flatY);
            Vector3 scale = go.transform.lossyScale;
            float widthLocal = bounds.size.x * 0.94f / Mathf.Max(0.01f, scale.x);
            float heightLocal = heightWorld / Mathf.Max(0.01f, scale.y);
            float offsetY = (flatY - go.transform.position.y) / Mathf.Max(0.01f, scale.y) - heightLocal * 0.5f;

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(widthLocal, heightLocal);
            box.offset = new Vector2(0f, offsetY);

            count++;
        }
        return count;
    }

    private static void SetupSinkingBoard()
    {
        GameObject board = GameObject.Find("Bridge_Patch_04");
        if (board == null)
            return;

        SinkingPlatform sinking = board.GetComponent<SinkingPlatform>();
        if (sinking == null)
            sinking = board.AddComponent<SinkingPlatform>();

        SerializedObject so = new SerializedObject(sinking);
        so.FindProperty("sinkDelay").floatValue = 1f;
        so.FindProperty("hiddenTime").floatValue = 1.5f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static float FlatFraction(string spriteName)
    {
        foreach (var entry in FlatLines)
            if (entry.sprite == spriteName)
                return entry.fraction;
        return 0.05f;
    }
}

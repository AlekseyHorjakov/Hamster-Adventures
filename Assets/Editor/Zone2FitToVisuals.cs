using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Коллайдеры по фактическому визуалу платформ: ничего не двигает,
/// только ставит прямоугольник по линии стойки спрайта (по низу травы).
/// </summary>
public static class Zone2FitToVisuals
{
    private const string ScenePath = "Assets/Scenes/Level01_Full.unity";

    private static readonly string[] Platforms =
    {
        "Island_1", "Island_2", "Island_3", "Island_Final",
        "CrossPlatform_1", "CrossPlatform_2", "CrossPlatform_3",
        "FallLedge_1", "FallLedge_2", "FallLedge_3",
        "Stone_A", "Stone_B", "Stone_C", "ShoreStone",
        "StepStone_1", "StepStone_2", "LakeDock",
        "Bridge_Patch_04", "Bridge_Section_01", "Bridge_Section_02"
    };

    [MenuItem("Tools/Хомяк/Зона II: коллайдеры строго по визуалу (ничего не двигать)")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Убираем мою лишнюю подложку дока, если она есть.
        GameObject dockVisual = GameObject.Find("LakeDock_Visual");
        if (dockVisual != null)
            Object.DestroyImmediate(dockVisual);
        Transform dockChild = GameObject.Find("LakeDock")?.transform.Find("Visual");
        if (dockChild != null)
            Object.DestroyImmediate(dockChild.gameObject);

        EnsureDockVisual();

        int count = 0;
        foreach (string name in Platforms)
        {
            GameObject root = GameObject.Find(name);
            if (root == null)
                continue;

            SpriteRenderer visual = root.GetComponentsInChildren<SpriteRenderer>(true)
                .FirstOrDefault(sr => sr != null && sr.sprite != null);
            if (visual == null)
                continue;

            // Если визуала нет — коллайдер не трогаем вообще (иначе платформа теряет опору).
            foreach (Collider2D old in root.GetComponents<Collider2D>())
                Object.DestroyImmediate(old);

            float fraction = Fraction(visual.sprite.name);
            Bounds b = visual.bounds;
            float flatY = b.max.y - fraction * b.size.y;
            Vector3 scale = root.transform.lossyScale;

            float widthLocal = b.size.x * 0.96f / Mathf.Max(0.01f, scale.x);
            float heightLocal = 0.5f / Mathf.Max(0.01f, scale.y);
            float offsetX = (b.center.x - root.transform.position.x) / Mathf.Max(0.01f, scale.x);
            float offsetY = (flatY - root.transform.position.y) / Mathf.Max(0.01f, scale.y) - heightLocal * 0.5f;

            BoxCollider2D box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(widthLocal, heightLocal);
            box.offset = new Vector2(offsetX, offsetY);

            // Мосты и доски — односторонние: снизу можно запрыгнуть, сверху стоишь.
            bool oneWay = name.StartsWith("Bridge_");
            box.usedByEffector = oneWay;
            if (oneWay)
            {
                PlatformEffector2D effector = root.GetComponent<PlatformEffector2D>();
                if (effector == null)
                    effector = root.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.useOneWayGrouping = true;
                effector.surfaceArc = 170f;
            }

            visual.sortingLayerName = "Ground";
            if (visual.sortingOrder < 11)
                visual.sortingOrder = 12;
            count++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FitToVisuals] платформ обработано: " + count);
    }

    private static void EnsureDockVisual()
    {
        GameObject dock = GameObject.Find("LakeDock");
        if (dock == null)
            return;
        if (dock.GetComponentsInChildren<SpriteRenderer>(true).Any(sr => sr != null && sr.sprite != null))
            return;

        Collider2D col = dock.GetComponent<Collider2D>();
        Sprite wood = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Level01/ModularSlices/Bridge_Patch_01.png");
        if (col == null || wood == null)
            return;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(dock.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = wood;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 12;

        float width = col.bounds.size.x;
        float height = 0.9f;
        visual.transform.localScale = new Vector3(width / wood.bounds.size.x, height / wood.bounds.size.y, 1f);
        float topLocal = col.bounds.max.y - dock.transform.position.y;
        visual.transform.localPosition = new Vector3(0f, topLocal - height * 0.5f, 0f);
    }

    private static float Fraction(string sprite)
    {
        if (sprite.StartsWith("Bridge_Section"))
            return 0.38f;
        if (sprite.StartsWith("Bridge_Patch"))
            return 0.35f;
        if (sprite == "Platform_Wide_A")
            return 0.232f;
        if (sprite == "Platform_Wide_B")
            return 0.182f;
        return 0.05f;
    }
}

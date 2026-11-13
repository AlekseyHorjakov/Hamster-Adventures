using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Подгоняет коллайдеры площадок под их визуалы и добавляет маски на швы лагеря.</summary>
public static class LevelSceneFixups
{
    private const string ScenePath = "Assets/Scenes/Level01_VerticalSlice.unity";
    private const float GroundArtLift = 1.1f;

    [MenuItem("Tools/Hamster/Align Ground Colliders To Visuals")]
    public static void AlignGroundColliders()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        AlignAll();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
        Debug.Log("[Fixup] Коллайдеры площадок выровнены по визуалам.");
    }

    [MenuItem("Tools/Hamster/Apply Level Fixups")]
    public static void ApplyAll()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        AlignAll();
        AddCampSeamMasks();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
        Debug.Log("[Fixup] Коллайдеры выровнены, маски лагеря добавлены.");
    }

    private static void AlignAll()
    {
        PlatformEffector2D[] platforms = Object.FindObjectsByType<PlatformEffector2D>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (PlatformEffector2D platform in platforms)
        {
            Transform root = platform.transform;
            Transform visual = root.Find("Visual");
            if (visual == null)
                continue;

            SpriteRenderer sprite = visual.GetComponent<SpriteRenderer>();
            BoxCollider2D box = root.GetComponent<BoxCollider2D>();
            if (sprite == null || box == null)
                continue;

            Bounds visualBounds = sprite.bounds;
            Vector3 savedVisualPosition = visual.position;
            Vector3 savedVisualScale = visual.localScale;

            float topY = visualBounds.max.y - GroundArtLift;
            root.position = new Vector3(visualBounds.center.x, topY - 0.5f, root.position.z);

            Vector3 lossy = root.lossyScale;
            float scaleX = Mathf.Max(0.001f, Mathf.Abs(lossy.x));
            box.offset = Vector2.zero;
            box.size = new Vector2(visualBounds.size.x / scaleX, 1f);

            visual.position = savedVisualPosition;
            visual.localScale = savedVisualScale;
        }
    }

    private static void AddCampSeamMasks()
    {
        GameObject artRoot = GameObject.Find("BakedLevelArt");
        GameObject camp = GameObject.Find("CampWorldVisual");
        if (artRoot == null || camp == null)
        {
            Debug.LogWarning("[Fixup] Не найден BakedLevelArt или CampWorldVisual.");
            return;
        }

        Transform oldTree = artRoot.transform.Find("CampSeamTree");
        Transform oldBush = artRoot.transform.Find("CampSeamBush");
        if (oldTree != null) Object.DestroyImmediate(oldTree.gameObject);
        if (oldBush != null) Object.DestroyImmediate(oldBush.gameObject);

        SpriteRenderer campRenderer = camp.GetComponent<SpriteRenderer>();
        if (campRenderer == null)
            return;

        Bounds campBounds = campRenderer.bounds;
        Sprite tree = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Level01/CampSeamTree.png");
        Sprite bush = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Level01/CampSeamBush.png");

        Place(artRoot.transform, "CampSeamTree", tree, campBounds.max.x - 0.1f, 12f, "Mid", -4);
        Place(artRoot.transform, "CampSeamBush", bush, campBounds.max.x + 0.9f, 4.6f, "Mid", -3);
    }

    private static void Place(Transform parent, string name, Sprite sprite, float x, float height,
        string sortingLayer, int sortingOrder)
    {
        if (sprite == null)
            return;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, height * 0.5f, 0f);
        go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Переносит runtime-визуал уровня в обычные объекты сцены для ручного редактирования.</summary>
public static class LevelArtBaker
{
    private const string ScenePath = "Assets/Scenes/Level01_VerticalSlice.unity";

    [MenuItem("Tools/Hamster/Bake Level Art Into Scene")]
    public static void Bake()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            Debug.LogError("[Bake] Не найдена камера.");
            return;
        }

        GameObject oldRoot = GameObject.Find("BakedLevelArt");
        if (oldRoot != null)
            Object.DestroyImmediate(oldRoot);

        Sprite sky = Load("Sky");
        Sprite mountains = Load("Mountains");
        Sprite forest = Load("Forest");
        Sprite foreground = Load("Foreground");
        Sprite lake = Load("Lake");
        Sprite campWorld = Load("CampWorld");
        Sprite arenaFence = Load("ArenaFence");
        Sprite finishGate = Load("FinishGate");

        GameObject root = new GameObject("BakedLevelArt");
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        CreateParallaxLayer(root.transform, cam, "Sky", sky, "Background", -100,
            new Vector3(cam.transform.position.x, cam.transform.position.y, 0f), screenHeight * 1.08f, 1f, 1f, Color.white);
        CreateZoneLayer(root.transform, cam, "LakeZone", lake, "Mid", -15,
            screenWidth * 0.5f, 42f, 132f, 10f, new Color(0.92f, 0.98f, 1f, 1f));
        CreateParallaxLayer(root.transform, cam, "Mountains", mountains, "Far", -20,
            new Vector3(cam.transform.position.x, cam.transform.position.y + 3f, 0f), screenWidth * 0.6f, 0.98f, 0.98f,
            new Color(0.88f, 0.94f, 1f, 1f));
        CreateParallaxLayer(root.transform, cam, "Forest", forest, "Mid", -30,
            new Vector3(cam.transform.position.x, cam.transform.position.y + 2f, 0f), screenWidth * 0.65f, 0.95f, 0.95f,
            new Color(0.82f, 0.95f, 0.86f, 1f));
        CreateParallaxLayer(root.transform, cam, "Foreground", foreground, "Props", 100,
            new Vector3(cam.transform.position.x, cam.transform.position.y - 6f, 0f), screenHeight * 0.9f, 1.02f, 1f,
            new Color(0.74f, 0.9f, 0.74f, 1f));

        PlaceGroundDecoration(root.transform, "CampWorldVisual", campWorld, -6f, 24f, "Mid", -5);
        PlaceGroundDecoration(root.transform, "ArenaFenceLeft", arenaFence, 132f, 6.2f, "Ground", 20);
        PlaceGroundDecoration(root.transform, "ArenaFenceRight", arenaFence, 192f, 6.2f, "Ground", 20);
        PlaceGroundDecoration(root.transform, "FinishGateVisual", finishGate, 194f, 8.2f, "Ground", 25);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Bake] Визуал перенесён в сцену: BakedLevelArt.");
    }

    private static Sprite Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Level01/" + name + ".png");
    }

    private static GameObject CreateParallaxLayer(Transform parent, Camera cam, string name,
        Sprite sprite, string sortingLayer, int sortingOrder, Vector3 position, float targetHeight,
        float horizontalFactor, float verticalFactor, Color tint)
    {
        if (sprite == null)
            return null;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * (targetHeight / sprite.bounds.size.y);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;

        ParallaxLayer parallax = go.AddComponent<ParallaxLayer>();
        SerializedObject so = new SerializedObject(parallax);
        so.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
        so.FindProperty("horizontalFactor").floatValue = horizontalFactor;
        so.FindProperty("verticalFactor").floatValue = verticalFactor;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    private static void CreateZoneLayer(Transform parent, Camera cam, string name,
        Sprite sprite, string sortingLayer, int sortingOrder, float targetHeight,
        float zoneStartX, float zoneEndX, float fade, Color tint)
    {
        GameObject go = CreateParallaxLayer(parent, cam, name, sprite, sortingLayer, sortingOrder,
            new Vector3(cam.transform.position.x, cam.transform.position.y, 0f), targetHeight, 1f, 1f, tint);
        if (go == null)
            return;

        ZoneFadeLayer fadeLayer = go.AddComponent<ZoneFadeLayer>();
        SerializedObject so = new SerializedObject(fadeLayer);
        so.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
        so.FindProperty("baseColor").colorValue = tint;
        so.FindProperty("startX").floatValue = zoneStartX;
        so.FindProperty("endX").floatValue = zoneEndX;
        so.FindProperty("fadeDistance").floatValue = fade;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PlaceGroundDecoration(Transform parent, string name, Sprite sprite,
        float x, float height, string sortingLayer, int sortingOrder)
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

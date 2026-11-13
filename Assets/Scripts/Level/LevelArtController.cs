using UnityEngine;

/// <summary>
/// Первый art-pass первого уровня: фоновый параллакс и замена тестовых
/// прямоугольников на сгенерированные спрайты. Коллайдеры и логика не меняются.
/// </summary>
public class LevelArtController : MonoBehaviour
{
    private Camera cam;
    private Transform artRoot;

    private Sprite sky;
    private Sprite mountains;
    private Sprite forest;
    private Sprite foreground;
    private Sprite lake;
    private Sprite arenaFence;
    private Sprite finishGate;
    private Sprite campWorld;

    private void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[LevelArt] Не найдена камера.");
            return;
        }

        LoadSprites();
        artRoot = new GameObject("GeneratedLevelArt").transform;

        BuildParallax();

        PlaceArenaAndFinish();
        PlaceCampProps();

    }

    private void LoadSprites()
    {
        sky = Resources.Load<Sprite>("Level01/Sky");
        mountains = Resources.Load<Sprite>("Level01/Mountains");
        forest = Resources.Load<Sprite>("Level01/Forest");
        foreground = Resources.Load<Sprite>("Level01/Foreground");
        lake = Resources.Load<Sprite>("Level01/Lake");
        arenaFence = Resources.Load<Sprite>("Level01/ArenaFence");
        finishGate = Resources.Load<Sprite>("Level01/FinishGate");
        campWorld = Resources.Load<Sprite>("Level01/CampWorld");
    }

    private void BuildParallax()
    {
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        CreateLayer("Sky", sky, "Background", -100,
            new Vector3(cam.transform.position.x, cam.transform.position.y, 0f),
            screenHeight * 1.08f, 1f, 1f, Color.white);

        float zoneWidth = screenWidth;
        float zoneHeight = zoneWidth * 0.5f;
        CreateZoneLayer("LakeZone", lake, "Mid", -15, cam.transform, zoneHeight,
            42f, 132f, 10f, new Color(0.92f, 0.98f, 1f, 1f));

        CreateLayer("Mountains", mountains, "Far", -20,
            new Vector3(cam.transform.position.x, cam.transform.position.y + 3f, 0f),
            screenWidth * 0.6f, 0.98f, 0.98f,
            new Color(0.88f, 0.94f, 1f, 1f));

        CreateLayer("Forest", forest, "Mid", -30,
            new Vector3(cam.transform.position.x, cam.transform.position.y + 2f, 0f),
            screenWidth * 0.65f, 0.95f, 0.95f,
            new Color(0.82f, 0.95f, 0.86f, 1f));

        CreateLayer("Foreground", foreground, "Props", 100,
            new Vector3(cam.transform.position.x, cam.transform.position.y - 6f, 0f),
            screenHeight * 0.9f, 1.02f, 1f,
            new Color(0.74f, 0.9f, 0.74f, 1f));
    }

    private GameObject CreateLayer(string name, Sprite sprite, string sortingLayer, int sortingOrder,
        Vector3 position, float targetHeight, float horizontalFactor, float verticalFactor,
        Color tint)
    {
        if (sprite == null)
        {
            Debug.LogWarning("[LevelArt] Не найден спрайт слоя " + name);
            return null;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(artRoot, false);
        go.transform.position = position;

        float scale = targetHeight / sprite.bounds.size.y;
        go.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;

        ParallaxLayer parallax = go.AddComponent<ParallaxLayer>();
        parallax.Initialize(cam.transform, horizontalFactor, verticalFactor);
        return go;
    }

    private void CreateZoneLayer(string name, Sprite sprite, string sortingLayer, int sortingOrder,
        Transform cameraTransform, float targetHeight, float zoneStartX, float zoneEndX,
        float fade, Color tint)
    {
        GameObject go = CreateLayer(name, sprite, sortingLayer, sortingOrder,
            new Vector3(cameraTransform.position.x, cameraTransform.position.y, 0f),
            targetHeight, 1f, 1f, tint);

        if (go == null)
            return;

        ZoneFadeLayer fadeLayer = go.AddComponent<ZoneFadeLayer>();
        fadeLayer.Initialize(cameraTransform, zoneStartX, zoneEndX, fade, tint);
    }

    private void PlaceCampProps()
    {
        if (campWorld != null)
            PlaceGroundDecoration("CampWorldVisual", campWorld, -6f, 24f, "Mid", -5);
    }
    private void PlaceArenaAndFinish()
    {
        if (arenaFence != null)
        {
            PlaceGroundDecoration("ArenaFenceLeft", arenaFence, 132f, 6.2f, "Ground", 20);
            PlaceGroundDecoration("ArenaFenceRight", arenaFence, 192f, 6.2f, "Ground", 20);
        }

        if (finishGate != null)
            PlaceGroundDecoration("FinishGateVisual", finishGate, 194f, 8.2f, "Ground", 25);
    }

    private void PlaceGroundDecoration(string name, Sprite sprite, float x, float height,
        string sortingLayer, int sortingOrder)
    {
        float width = height * (sprite.bounds.size.x / sprite.bounds.size.y);
        GameObject go = new GameObject(name);
        go.transform.SetParent(artRoot, false);
        go.transform.position = new Vector3(x, height * 0.5f, 0f);
        go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;
    }
}

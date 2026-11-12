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
    private Sprite camp;
    private Sprite ground;
    private Sprite platform;
    private Sprite arenaFence;
    private Sprite finishGate;

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
        ReplaceGroundVisuals();
        PlaceArenaAndFinish();
    }

    private void LoadSprites()
    {
        sky = Resources.Load<Sprite>("Level01/Sky");
        mountains = Resources.Load<Sprite>("Level01/Mountains");
        forest = Resources.Load<Sprite>("Level01/Forest");
        foreground = Resources.Load<Sprite>("Level01/Foreground");
        lake = Resources.Load<Sprite>("Level01/Lake");
        camp = Resources.Load<Sprite>("Level01/Camp");
        ground = Resources.Load<Sprite>("Level01/Ground");
        platform = Resources.Load<Sprite>("Level01/Platform");
        arenaFence = Resources.Load<Sprite>("Level01/ArenaFence");
        finishGate = Resources.Load<Sprite>("Level01/FinishGate");
    }

    private void BuildParallax()
    {
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        CreateLayer("Sky", sky, "Background", -100,
            new Vector3(cam.transform.position.x, cam.transform.position.y, 0f),
            screenHeight * 1.08f, 1f, 1f, Color.white);

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

    private void CreateLayer(string name, Sprite sprite, string sortingLayer, int sortingOrder,
        Vector3 position, float targetHeight, float horizontalFactor, float verticalFactor,
        Color tint)
    {
        if (sprite == null)
        {
            Debug.LogWarning("[LevelArt] Не найден спрайт слоя " + name);
            return;
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
    }

    private void ReplaceGroundVisuals()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (SpriteRenderer oldRenderer in renderers)
        {
            if (oldRenderer == null)
                continue;

            string objectName = oldRenderer.gameObject.name;
            if (objectName != "Ground" && !objectName.StartsWith("Ground ("))
                continue;

            BoxCollider2D box = oldRenderer.GetComponent<BoxCollider2D>();
            if (box == null)
                continue;

            oldRenderer.enabled = false;
            Bounds bounds = box.bounds;
            bool longGround = bounds.size.x >= 10f;

            if (longGround && ground != null)
                CreateLongGroundVisual(bounds);
            else if (platform != null)
                CreatePlatformVisual(bounds);
        }
    }

    private void CreateLongGroundVisual(Bounds bounds)
    {
        GameObject go = new GameObject("GroundVisual");
        go.transform.SetParent(artRoot, false);
        go.transform.position = new Vector3(bounds.center.x, bounds.max.y - ground.bounds.size.y * 0.5f, 0f);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = ground;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(bounds.size.x, ground.bounds.size.y);
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 10;
    }

    private void CreatePlatformVisual(Bounds bounds)
    {
        float aspect = platform.bounds.size.x / platform.bounds.size.y;
        float width = bounds.size.x * 1.06f;
        float height = width / aspect;

        GameObject go = new GameObject("PlatformVisual");
        go.transform.SetParent(artRoot, false);
        go.transform.position = new Vector3(bounds.center.x, bounds.max.y - height * 0.5f, 0f);
        go.transform.localScale = Vector3.one * (width / platform.bounds.size.x);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = platform;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 11;
    }

    private void PlaceArenaAndFinish()
    {
        if (arenaFence != null)
        {
            PlaceGroundDecoration("ArenaFenceLeft", arenaFence, 8.5f, 6.2f, "Ground", 20);
            PlaceGroundDecoration("ArenaFenceRight", arenaFence, 43.5f, 6.2f, "Ground", 20);
        }

        if (finishGate != null)
            PlaceGroundDecoration("FinishGateVisual", finishGate, 47f, 8.2f, "Ground", 25);
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

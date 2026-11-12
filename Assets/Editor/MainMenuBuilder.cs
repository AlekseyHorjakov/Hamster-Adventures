using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Собирает стартовую сцену с фоном, табличкой и героями.</summary>
public static class MainMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BackgroundPath = "Assets/Art/UI/MainMenu/MainMenu_Background.png";
    private const string SignboardPath = "Assets/Art/UI/MainMenu/MainMenu_Signboard.png";
    private const string HamsterPath = "Assets/Art/UI/MainMenu/Hamster_Menu.png";
    private const string GirlPath = "Assets/Art/HamsterGirl/Hamster_Girl_NoBackground.png";
    private const string LevelPath = "Assets/Scenes/Level01_VerticalSlice.unity";

    [MenuItem("Tools/Hamster/Build Main Menu")]
    public static void BuildMainMenu()
    {
        ConfigureSprite(BackgroundPath, false);
        ConfigureSprite(SignboardPath, true);
        ConfigureSprite(HamsterPath, true);

        Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        Sprite signboard = AssetDatabase.LoadAssetAtPath<Sprite>(SignboardPath);
        Sprite hamster = AssetDatabase.LoadAssetAtPath<Sprite>(HamsterPath);
        Sprite girl = AssetDatabase.LoadAssetAtPath<Sprite>(GirlPath);
        if (background == null || signboard == null || hamster == null || girl == null)
        {
            Debug.LogError("[MainMenu] Не удалось загрузить один из спрайтов меню.");
            EditorApplication.Exit(1);
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGo = new GameObject("Main Camera", typeof(Camera));
        cameraGo.tag = "MainCamera";
        Camera camera = cameraGo.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.03f, 0.08f);

        var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        InputSystemUIInputModule inputModule = eventSystemGo.GetComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CreateStretchImage("Background", canvas.transform, background, Color.white);


        GameObject boardGo = CreateImage("Signboard", canvas.transform, signboard,
            new Vector2(0f, -120f), new Vector2(920f, 518f), Color.white, true);
        Transform board = boardGo.transform;

        CreateAnchoredImage("HamsterGirl", canvas.transform, girl, new Vector2(0.14f, 0.16f),
            new Vector2(280f, 298f));
        CreateAnchoredImage("Hamster", canvas.transform, hamster, new Vector2(0.86f, 0.16f),
            new Vector2(330f, 380f));
        CreateBoardTitle(board);

        var controllerGo = new GameObject("MainMenuController", typeof(MainMenuController));
        MainMenuController controller = controllerGo.GetComponent<MainMenuController>();

        controller.hamsterButton = CreateMenuButton(board, "HamsterButton",
            "1   Один игрок: Хомяк", new Vector2(0f, 90f));
        controller.girlButton = CreateMenuButton(board, "GirlButton",
            "2   Один игрок: Хомячиха", new Vector2(0f, 20f));
        controller.coopButton = CreateMenuButton(board, "CoopButton",
            "3   Вдвоём на одном компьютере", new Vector2(0f, -50f));
        controller.quitButton = CreateMenuButton(board, "QuitButton",
            "Esc   Выход", new Vector2(0f, -120f));

        CreateVersionLabel(canvas.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();

        Debug.Log("[MainMenu] Готово: " + ScenePath);
    }

    private static void CreateStretchImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    private static GameObject CreateImage(string name, Transform parent, Sprite sprite,
        Vector2 position, Vector2 size, Color color, bool preserveAspect)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return go;
    }

    private static void CreateAnchoredImage(string name, Transform parent, Sprite sprite,
        Vector2 anchor, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }
    private static void CreateBoardTitle(Transform board)
    {
        GameObject go = new GameObject("Title", typeof(RectTransform), typeof(Text));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(board, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 165f);
        rect.sizeDelta = new Vector2(760f, 60f);

        Text text = go.GetComponent<Text>();
        text.text = "ВЫБЕРИ ПРИКЛЮЧЕНИЕ";
        text.font = GetMenuFont();
        text.fontSize = 32;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.93f, 0.75f, 1f);
        text.raycastTarget = false;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.035f, 0.01f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private static Button CreateMenuButton(Transform board, string name, string label,
        Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image),
            typeof(Button), typeof(Outline));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(board, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(480f, 56f);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.16f, 0.055f, 0.02f, 0.9f);

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.63f, 0.18f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.82f, 0.5f, 1f);
        colors.pressedColor = new Color(0.85f, 0.55f, 0.22f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.layer = LayerMask.NameToLayer("UI");
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.SetParent(go.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 4f);
        textRect.offsetMax = new Vector2(-16f, -4f);

        Text text = textGo.GetComponent<Text>();
        text.text = label;
        text.font = GetMenuFont();
        text.fontSize = 26;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.93f, 0.75f, 1f);
        text.raycastTarget = false;

        Outline textOutline = textGo.AddComponent<Outline>();
        textOutline.effectColor = new Color(0.12f, 0.035f, 0.01f, 0.9f);
        textOutline.effectDistance = new Vector2(1.5f, -1.5f);

        return button;
    }

    private static void CreateVersionLabel(Transform canvas)
    {
        GameObject go = new GameObject("BuildLabel", typeof(RectTransform), typeof(Text));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(canvas, false);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-24f, 18f);
        rect.sizeDelta = new Vector2(360f, 28f);

        Text text = go.GetComponent<Text>();
        text.text = "build " + Application.version;
        text.font = GetMenuFont();
        text.fontSize = 17;
        text.alignment = TextAnchor.LowerRight;
        text.color = new Color(1f, 1f, 1f, 0.78f);
        text.raycastTarget = false;
    }

    private static Font GetMenuFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private static void ConfigureSprite(string path, bool alpha)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = alpha;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(LevelPath, true)
        };
    }
}

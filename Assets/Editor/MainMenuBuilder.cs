using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Собирает стартовую сцену с фоном, табличкой, героями и меню.</summary>
public static class MainMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BackgroundPath = "Assets/Art/UI/MainMenu/MainMenu_Background.png";
    private const string SignboardPath = "Assets/Art/UI/MainMenu/MainMenu_Signboard.png";
    private const string HamsterPath = "Assets/Art/UI/MainMenu/Hamster_Menu.png";
    private const string GirlPath = "Assets/Art/UI/MainMenu/Girl_Menu.png";
    private const string LevelPath = "Assets/Scenes/Level01_VerticalSlice.unity";

    private const float CharacterHeight = 430f;

    [MenuItem("Tools/Hamster/Build Main Menu")]
    public static void BuildMainMenu()
    {
        ConfigureSprite(BackgroundPath, false);
        ConfigureSprite(SignboardPath, true);
        ConfigureSprite(HamsterPath, true);
        ConfigureSprite(GirlPath, true);

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

        float girlWidth = CharacterHeight * (girl.rect.width / girl.rect.height);
        float hamsterWidth = CharacterHeight * (hamster.rect.width / hamster.rect.height);

        Button girlCharacterButton = CreateCharacter("HamsterGirl", canvas.transform, girl,
            new Vector2(0.13f, 0.16f), new Vector2(girlWidth, CharacterHeight),
            out Image girlImage, out Outline girlOutline, out GameObject girlGlow);
        Button hamsterCharacterButton = CreateCharacter("Hamster", canvas.transform, hamster,
            new Vector2(0.87f, 0.16f), new Vector2(hamsterWidth, CharacterHeight),
            out Image hamsterImage, out Outline hamsterOutline, out GameObject hamsterGlow);

        Text heading = CreateHeading(board);

        MainMenuController controller = new GameObject("MainMenuController")
            .AddComponent<MainMenuController>();

        GameObject mainPanel = CreatePanel("MainPanel", board);
        controller.newGameButton = CreateMenuButton(mainPanel.transform, "NewGameButton",
            "Новая игра", new Vector2(0f, 150f));
        controller.continueButton = CreateMenuButton(mainPanel.transform, "ContinueButton",
            "Продолжить", new Vector2(0f, 75f));
        controller.campButton = CreateMenuButton(mainPanel.transform, "CampButton",
            "В лагерь", new Vector2(0f, 0f));
        controller.settingsButton = CreateMenuButton(mainPanel.transform, "SettingsButton",
            "Настройки", new Vector2(0f, -75f));
        controller.exitButton = CreateMenuButton(mainPanel.transform, "ExitButton",
            "Выход", new Vector2(0f, -150f));

        GameObject modePanel = CreatePanel("ModePanel", board);
        controller.onePlayerButton = CreateMenuButton(modePanel.transform, "OnePlayerButton",
            "Один игрок", new Vector2(0f, 80f));
        controller.twoPlayerButton = CreateMenuButton(modePanel.transform, "TwoPlayerButton",
            "Два игрока", new Vector2(0f, 0f));
        controller.backFromModesButton = CreateMenuButton(modePanel.transform, "BackButton",
            "Назад", new Vector2(0f, -80f));

        GameObject characterPanel = CreatePanel("CharacterPanel", board);
        controller.girlArrowButton = CreateMenuButton(characterPanel.transform, "GirlArrow",
            "◀", new Vector2(-350f, 30f), new Vector2(72f, 72f), 36);
        controller.hamsterArrowButton = CreateMenuButton(characterPanel.transform, "HamsterArrow",
            "▶", new Vector2(350f, 30f), new Vector2(72f, 72f), 36);
        controller.characterStatusText = CreateText(characterPanel.transform, "Status",
            "Нажми на стрелку или на героя", new Vector2(0f, 130f),
            new Vector2(560f, 44f), 21, TextAnchor.MiddleCenter,
            new Color(1f, 0.93f, 0.75f, 1f));
        controller.startSoloButton = CreateMenuButton(characterPanel.transform, "StartSoloButton",
            "Начать", new Vector2(0f, -80f));
        controller.backFromCharacterButton = CreateMenuButton(characterPanel.transform,
            "BackToModesButton", "Назад", new Vector2(0f, -150f));

        Text confirmMessage;
        Button confirmYes;
        Button confirmNo;
        Button unusedInfo;
        controller.confirmationPopup = CreatePopup(canvas.transform, "ConfirmationPopup",
            "Уверены?", "Начать новую игру? Текущий прогресс будет сброшен.",
            out confirmMessage, out confirmYes, out confirmNo, out unusedInfo);
        controller.confirmYesButton = confirmYes;
        controller.confirmNoButton = confirmNo;

        Text infoMessage;
        Button unusedYes;
        Button unusedNo;
        Button infoOk;
        controller.infoPopup = CreatePopup(canvas.transform, "InfoPopup",
            "Скоро", "", out infoMessage, out unusedYes, out unusedNo, out infoOk,
            false);
        controller.infoMessageText = infoMessage;
        controller.infoOkButton = infoOk;

        controller.headingText = heading;
        controller.mainPanel = mainPanel;
        controller.modePanel = modePanel;
        controller.characterPanel = characterPanel;
        controller.hamsterImage = hamsterImage;
        controller.girlImage = girlImage;
        controller.hamsterOutline = hamsterOutline;
        controller.girlOutline = girlOutline;
        controller.hamsterGlow = hamsterGlow;
        controller.girlGlow = girlGlow;
        controller.hamsterCharacterButton = hamsterCharacterButton;
        controller.girlCharacterButton = girlCharacterButton;

        modePanel.SetActive(false);
        characterPanel.SetActive(false);
        controller.confirmationPopup.SetActive(false);
        controller.infoPopup.SetActive(false);

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

    private static Button CreateCharacter(string name, Transform parent, Sprite sprite,
        Vector2 anchor, Vector2 size, out Image image, out Outline outline, out GameObject glow)
    {
        glow = new GameObject(name + "Glow", typeof(RectTransform), typeof(Image));
        glow.layer = LayerMask.NameToLayer("UI");
        RectTransform glowRect = glow.GetComponent<RectTransform>();
        glowRect.SetParent(parent, false);
        glowRect.anchorMin = anchor;
        glowRect.anchorMax = anchor;
        glowRect.pivot = new Vector2(0.5f, 0f);
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.sizeDelta = size * 1.12f;

        Image glowImage = glow.GetComponent<Image>();
        glowImage.sprite = sprite;
        glowImage.color = new Color(1f, 0.72f, 0.2f, 0.55f);
        glowImage.preserveAspect = true;
        glowImage.raycastTarget = false;
        glow.SetActive(false);

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = true;

        outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.72f, 0.2f, 0.95f);
        outline.effectDistance = new Vector2(4f, -4f);
        outline.enabled = false;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        return button;
    }

    private static Text CreateHeading(Transform board)
    {
        GameObject go = new GameObject("Heading", typeof(RectTransform), typeof(Text));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(board, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 225f);
        rect.sizeDelta = new Vector2(760f, 60f);

        Text text = go.GetComponent<Text>();
        text.text = "ВЫБЕРИ ПРИКЛЮЧЕНИЕ";
        text.font = GetMenuFont();
        text.fontSize = 30;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.93f, 0.75f, 1f);
        text.raycastTarget = false;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.035f, 0.01f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return text;
    }

    private static Text CreateText(Transform parent, string name, string value, Vector2 position,
        Vector2 size, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = go.GetComponent<Text>();
        text.text = value;
        text.font = GetMenuFont();
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreatePanel(string name, Transform board)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(board, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(920f, 518f);
        return go;
    }

    private static Button CreateMenuButton(Transform parent, string name, string label,
        Vector2 position)
    {
        return CreateMenuButton(parent, name, label, position, new Vector2(480f, 56f), 26);
    }

    private static Button CreateMenuButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image),
            typeof(Button), typeof(Outline));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

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
        colors.disabledColor = new Color(0.55f, 0.5f, 0.45f, 0.55f);
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
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.93f, 0.75f, 1f);
        text.raycastTarget = false;

        Outline textOutline = textGo.AddComponent<Outline>();
        textOutline.effectColor = new Color(0.12f, 0.035f, 0.01f, 0.9f);
        textOutline.effectDistance = new Vector2(1.5f, -1.5f);

        return button;
    }

    private static GameObject CreatePopup(Transform canvas, string name, string title,
        string message, out Text messageText, out Button yesButton, out Button noButton,
        out Button okButton, bool confirmation = true)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
        root.layer = LayerMask.NameToLayer("UI");
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(canvas, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image overlay = root.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.68f);
        overlay.raycastTarget = true;

        GameObject panel = new GameObject("PopupPanel", typeof(RectTransform), typeof(Image),
            typeof(Outline));
        panel.layer = LayerMask.NameToLayer("UI");
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(root.transform, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(660f, 330f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.14f, 0.045f, 0.015f, 0.98f);

        Outline panelOutline = panel.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.95f, 0.63f, 0.18f, 0.95f);
        panelOutline.effectDistance = new Vector2(3f, -3f);

        Text titleText = CreateText(panel.transform, "Title", title, new Vector2(0f, 105f),
            new Vector2(560f, 54f), 36, TextAnchor.MiddleCenter,
            new Color(1f, 0.93f, 0.75f, 1f));
        messageText = CreateText(panel.transform, "Message", message, new Vector2(0f, 22f),
            new Vector2(560f, 90f), 22, TextAnchor.MiddleCenter,
            new Color(0.9f, 0.95f, 1f, 1f));
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Truncate;

        yesButton = null;
        noButton = null;
        okButton = null;
        if (confirmation)
        {
            yesButton = CreateMenuButton(panel.transform, "YesButton", "Да",
                new Vector2(-155f, -105f), new Vector2(220f, 58f), 28);
            noButton = CreateMenuButton(panel.transform, "NoButton", "Нет",
                new Vector2(155f, -105f), new Vector2(220f, 58f), 28);
        }
        else
        {
            okButton = CreateMenuButton(panel.transform, "OkButton", "Ок",
                new Vector2(0f, -105f), new Vector2(220f, 58f), 28);
        }

        return root;
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

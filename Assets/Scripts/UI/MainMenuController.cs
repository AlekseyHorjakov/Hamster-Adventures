using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Стартовый экран, выбор режима и героя, продолжение с чекпоинта.</summary>
public class MainMenuController : MonoBehaviour
{
    private const string LevelSceneName = "Level01_VerticalSlice";

    private enum Screen
    {
        Main,
        NewGameMode,
        CharacterSelect
    }

    private enum Modal
    {
        None,
        ConfirmNewGame,
        Info
    }

    private enum SoloCharacter
    {
        None,
        Hamster,
        Girl
    }

    public GameObject mainPanel;
    public GameObject modePanel;
    public GameObject characterPanel;
    public GameObject confirmationPopup;
    public GameObject infoPopup;

    public Text headingText;
    public Text characterStatusText;
    public Text infoMessageText;

    public Button newGameButton;
    public Button continueButton;
    public Button campButton;
    public Button settingsButton;
    public Button exitButton;
    public Button onePlayerButton;
    public Button twoPlayerButton;
    public Button backFromModesButton;
    public Button girlArrowButton;
    public Button hamsterArrowButton;
    public Button girlCharacterButton;
    public Button hamsterCharacterButton;
    public Button startSoloButton;
    public Button backFromCharacterButton;
    public Button confirmYesButton;
    public Button confirmNoButton;
    public Button infoOkButton;

    public Image hamsterImage;
    public Image girlImage;
    public Outline hamsterOutline;
    public Outline girlOutline;
    public GameObject hamsterGlow;
    public GameObject girlGlow;

    private Screen screen = Screen.Main;
    private Modal modal = Modal.None;
    private SoloCharacter selectedCharacter = SoloCharacter.None;
    private bool startingGame;

    private void Awake()
    {
        GameSession.Clear();
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ApplyMenuFont();
        UpdateBuildLabel();
        PrepareCharacterOutlines();
        WireButtons();

        ShowMainMenu();
    }

    private void Update()
    {
        HandleMouseFallback();
        HandleKeyboard();
    }

    private void WireButtons()
    {
        newGameButton?.onClick.AddListener(OpenNewGameConfirmation);
        continueButton?.onClick.AddListener(ContinueGame);
        campButton?.onClick.AddListener(() => ShowInfo("Лагерь появится позже: там можно будет менять суперудар после спасения друзей."));
        settingsButton?.onClick.AddListener(() => ShowInfo("Настройки меню появятся позже."));
        exitButton?.onClick.AddListener(QuitGame);

        onePlayerButton?.onClick.AddListener(OpenCharacterSelect);
        twoPlayerButton?.onClick.AddListener(() => StartNewGame(GameMode.Coop));
        backFromModesButton?.onClick.AddListener(ShowMainMenu);

        girlArrowButton?.onClick.AddListener(SelectGirl);
        hamsterArrowButton?.onClick.AddListener(SelectHamster);
        girlCharacterButton?.onClick.AddListener(SelectGirl);
        hamsterCharacterButton?.onClick.AddListener(SelectHamster);
        startSoloButton?.onClick.AddListener(StartSoloGame);
        backFromCharacterButton?.onClick.AddListener(ShowModeSelect);

        confirmYesButton?.onClick.AddListener(ConfirmNewGame);
        confirmNoButton?.onClick.AddListener(CloseConfirmation);
        infoOkButton?.onClick.AddListener(CloseInfo);
    }

    private void HandleKeyboard()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (modal == Modal.ConfirmNewGame)
        {
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                ConfirmNewGame();
            else if (keyboard.escapeKey.wasPressedThisFrame)
                CloseConfirmation();
            return;
        }

        if (modal == Modal.Info)
        {
            if (keyboard.escapeKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame)
                CloseInfo();
            return;
        }

        switch (screen)
        {
            case Screen.Main:
                if (keyboard.digit1Key.wasPressedThisFrame) OpenNewGameConfirmation();
                else if (keyboard.digit2Key.wasPressedThisFrame) ContinueGame();
                else if (keyboard.digit3Key.wasPressedThisFrame)
                    ShowInfo("Лагерь появится позже: там можно будет менять суперудар после спасения друзей.");
                else if (keyboard.digit4Key.wasPressedThisFrame)
                    ShowInfo("Настройки меню появятся позже.");
                else if (keyboard.digit5Key.wasPressedThisFrame) QuitGame();
                else if (keyboard.escapeKey.wasPressedThisFrame) QuitGame();
                break;

            case Screen.NewGameMode:
                if (keyboard.digit1Key.wasPressedThisFrame) OpenCharacterSelect();
                else if (keyboard.digit2Key.wasPressedThisFrame) StartNewGame(GameMode.Coop);
                else if (keyboard.escapeKey.wasPressedThisFrame) ShowMainMenu();
                break;

            case Screen.CharacterSelect:
                if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                    SelectGirl();
                else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                    SelectHamster();
                else if ((keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                         && selectedCharacter != SoloCharacter.None)
                    StartSoloGame();
                else if (keyboard.escapeKey.wasPressedThisFrame)
                    ShowModeSelect();
                break;
        }
    }

    private void HandleMouseFallback()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Vector2 pointer = mouse.position.ReadValue();
        foreach (Button button in CurrentButtons())
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                    button.GetComponent<RectTransform>(), pointer, null))
            {
                button.onClick.Invoke();
                return;
            }
        }
    }

    private Button[] CurrentButtons()
    {
        if (modal == Modal.ConfirmNewGame)
            return new[] { confirmYesButton, confirmNoButton };
        if (modal == Modal.Info)
            return new[] { infoOkButton };

        switch (screen)
        {
            case Screen.Main:
                return new[] { newGameButton, continueButton, campButton, settingsButton, exitButton };
            case Screen.NewGameMode:
                return new[] { onePlayerButton, twoPlayerButton, backFromModesButton };
            default:
                return new[]
                {
                    girlArrowButton, hamsterArrowButton, girlCharacterButton,
                    hamsterCharacterButton, startSoloButton, backFromCharacterButton
                };
        }
    }

    private void ShowMainMenu()
    {
        screen = Screen.Main;
        modal = Modal.None;
        selectedCharacter = SoloCharacter.None;
        confirmationPopup?.SetActive(false);
        infoPopup?.SetActive(false);
        mainPanel?.SetActive(true);
        modePanel?.SetActive(false);
        characterPanel?.SetActive(false);
        SetHeading("ВЫБЕРИ ПРИКЛЮЧЕНИЕ");
        RefreshContinueButton();
    }

    private void ShowModeSelect()
    {
        screen = Screen.NewGameMode;
        modal = Modal.None;
        confirmationPopup?.SetActive(false);
        infoPopup?.SetActive(false);
        mainPanel?.SetActive(false);
        modePanel?.SetActive(true);
        characterPanel?.SetActive(false);
        SetHeading("НОВАЯ ИГРА");
    }

    private void OpenCharacterSelect()
    {
        screen = Screen.CharacterSelect;
        mainPanel?.SetActive(false);
        modePanel?.SetActive(false);
        characterPanel?.SetActive(true);
        selectedCharacter = SoloCharacter.None;
        SetHeading("ВЫБЕРИ ГЕРОЯ");
        RefreshCharacterSelection();
    }

    private void OpenNewGameConfirmation()
    {
        modal = Modal.ConfirmNewGame;
        confirmationPopup?.SetActive(true);
    }

    private void ConfirmNewGame()
    {
        modal = Modal.None;
        confirmationPopup?.SetActive(false);
        ShowModeSelect();
    }

    private void CloseConfirmation()
    {
        modal = Modal.None;
        confirmationPopup?.SetActive(false);
    }

    private void SelectHamster()
    {
        selectedCharacter = SoloCharacter.Hamster;
        RefreshCharacterSelection();
    }

    private void SelectGirl()
    {
        selectedCharacter = SoloCharacter.Girl;
        RefreshCharacterSelection();
    }

    private void RefreshCharacterSelection()
    {
        bool hasSelection = selectedCharacter != SoloCharacter.None;
        bool hamster = selectedCharacter == SoloCharacter.Hamster;
        bool girl = selectedCharacter == SoloCharacter.Girl;

        ApplyCharacterHighlight(hamsterImage, hamsterOutline, hamsterGlow, hamster, hasSelection);
        ApplyCharacterHighlight(girlImage, girlOutline, girlGlow, girl, hasSelection);

        if (startSoloButton != null)
            startSoloButton.interactable = selectedCharacter != SoloCharacter.None;

        if (characterStatusText != null)
        {
            characterStatusText.text = selectedCharacter == SoloCharacter.None
                ? "Нажми на стрелку или на героя"
                : selectedCharacter == SoloCharacter.Hamster
                    ? "Выбран Хомяк"
                    : "Выбрана Хомячиха";
        }
    }

    private void PrepareCharacterOutlines()
    {
        if (hamsterOutline != null)
            hamsterOutline.effectDistance = new Vector2(8f, -8f);
        if (girlOutline != null)
            girlOutline.effectDistance = new Vector2(8f, -8f);
    }

    private static void ApplyCharacterHighlight(Image image, Outline outline, GameObject glow,
        bool selected, bool hasSelection)
    {
        if (image != null)
        {
            image.color = !hasSelection || selected
                ? Color.white
                : new Color(0.42f, 0.42f, 0.42f, 0.9f);
            image.transform.localScale = selected ? Vector3.one * 1.03f : Vector3.one;
        }

        if (outline != null)
            outline.enabled = selected;
        if (glow != null)
            glow.SetActive(selected);
    }

    private void StartSoloGame()
    {
        if (selectedCharacter == SoloCharacter.None)
            return;

        StartNewGame(selectedCharacter == SoloCharacter.Hamster
            ? GameMode.SoloHamster
            : GameMode.SoloHamsterGirl);
    }

    private void StartNewGame(GameMode mode)
    {
        if (startingGame)
            return;

        startingGame = true;
        GameProgress.StartNew(mode);
        GameSession.SetMode(mode);
        Time.timeScale = 1f;
        SceneManager.LoadScene(LevelSceneName);
    }

    private void ContinueGame()
    {
        if (startingGame)
            return;

        if (!GameProgress.TryBeginContinue(out GameMode mode))
        {
            ShowInfo("Сохранение не найдено. Начни новую игру.");
            return;
        }

        startingGame = true;
        GameSession.SetMode(mode);
        Time.timeScale = 1f;
        SceneManager.LoadScene(LevelSceneName);
    }

    private void RefreshContinueButton()
    {
        if (continueButton == null)
            return;

        continueButton.interactable = GameProgress.HasCheckpoint;
        CanvasGroup group = continueButton.GetComponent<CanvasGroup>();
        if (group == null)
            group = continueButton.gameObject.AddComponent<CanvasGroup>();
        group.alpha = GameProgress.HasCheckpoint ? 1f : 0.45f;
    }

    private void ShowInfo(string message)
    {
        modal = Modal.Info;
        if (infoMessageText != null)
            infoMessageText.text = message;
        infoPopup?.SetActive(true);
    }

    private void CloseInfo()
    {
        modal = Modal.None;
        infoPopup?.SetActive(false);
    }

    private void SetHeading(string text)
    {
        if (headingText != null)
            headingText.text = text;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void UpdateBuildLabel()
    {
        GameObject label = GameObject.Find("BuildLabel");
        if (label != null && label.TryGetComponent(out Text text))
            text.text = "build " + Application.version;
    }

    private void ApplyMenuFont()
    {
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Tahoma" }, 32);
        if (font == null)
            return;

        foreach (Text text in GetComponentsInChildren<Text>(true))
            text.font = font;
    }
}

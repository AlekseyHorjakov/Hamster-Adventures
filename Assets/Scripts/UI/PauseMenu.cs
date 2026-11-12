using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Общая пауза: деревянная доска, стилизованные кнопки и подтверждение выхода.
/// </summary>
[DefaultExecutionOrder(-900)]
public class PauseMenu : MonoBehaviour
{
    private enum Page
    {
        Main,
        Controls,
        Settings
    }

    private const string VolumeKey = "settings.masterVolume";
    private const string FullscreenKey = "settings.fullscreen";
    private const string BoardResource = "UI/PauseMenu_Signboard";
    private const float BoardAspect = 1672f / 941f;

    private Page page = Page.Main;
    private bool confirmationOpen;
    private LevelFlowController levelFlow;
    private float volume = 1f;
    private bool fullscreen;

    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;
    private GUIStyle hintStyle;
    private GUIStyle popupBodyStyle;
    private Texture2D boardTexture;
    private Texture2D buttonNormal;
    private Texture2D buttonHover;
    private Texture2D buttonActive;

    private bool IsOpen => GamePause.IsPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Object.FindFirstObjectByType<PauseMenu>() != null)
            return;

        new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    private void Awake()
    {
        volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
        AudioListener.volume = volume;
        ApplyFullscreen();
        LoadBoardTexture();
    }

    private void Update()
    {
        if (!GameSession.HasSelection)
        {
            if (IsOpen)
                Resume();
            return;
        }

        levelFlow = levelFlow != null
            ? levelFlow
            : FindFirstObjectByType<LevelFlowController>();

        if (levelFlow != null && levelFlow.IsEndScreen)
        {
            if (IsOpen)
                Resume();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        if (IsOpen)
        {
            HandlePauseInput(keyboard, gamepad);
            return;
        }

        bool open = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        if (!open && gamepad != null)
            open = gamepad.startButton.wasPressedThisFrame;

        if (open)
            Open();
    }

    private void HandlePauseInput(Keyboard keyboard, Gamepad gamepad)
    {
        if (confirmationOpen)
        {
            bool confirm = keyboard != null
                && (keyboard.enterKey.wasPressedThisFrame
                    || keyboard.numpadEnterKey.wasPressedThisFrame
                    || keyboard.digit1Key.wasPressedThisFrame);
            bool cancel = keyboard != null
                && (keyboard.escapeKey.wasPressedThisFrame
                    || keyboard.digit2Key.wasPressedThisFrame);

            if (!confirm && gamepad != null)
                confirm = gamepad.buttonSouth.wasPressedThisFrame;
            if (!cancel && gamepad != null)
                cancel = gamepad.buttonEast.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame;

            if (confirm)
                ExitToMenu();
            else if (cancel)
                confirmationOpen = false;
            return;
        }

        if (keyboard == null && gamepad == null)
            return;

        bool back = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        if (!back && gamepad != null)
            back = gamepad.startButton.wasPressedThisFrame;

        if (page != Page.Main)
        {
            if (back)
                page = Page.Main;
            return;
        }

        if (back || keyboard != null && keyboard.digit1Key.wasPressedThisFrame)
        {
            Resume();
            return;
        }

        if (keyboard == null)
            return;

        if (keyboard.digit2Key.wasPressedThisFrame)
            page = Page.Controls;
        else if (keyboard.digit3Key.wasPressedThisFrame)
            page = Page.Settings;
        else if (keyboard.digit4Key.wasPressedThisFrame)
            RestartLevel();
        else if (keyboard.digit5Key.wasPressedThisFrame)
            confirmationOpen = true;
    }

    private void Open()
    {
        page = Page.Main;
        confirmationOpen = false;
        GamePause.SetPaused(true);
        SetPlayerInputEnabled(false);
    }

    private void Resume()
    {
        confirmationOpen = false;
        GamePause.SetPaused(false);
        SetPlayerInputEnabled(true);
        page = Page.Main;
    }

    private static void SetPlayerInputEnabled(bool enabled)
    {
        PlayerInputReader[] readers = FindObjectsByType<PlayerInputReader>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (PlayerInputReader reader in readers)
        {
            if (reader != null)
                reader.SetInputEnabled(enabled);
        }
    }

    private void RestartLevel()
    {
        GameMode mode = GameSession.Mode;
        Resume();
        GameProgress.StartNew(mode);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    private void ExitToMenu()
    {
        Resume();
        GameSession.Clear();
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDisable()
    {
        if (IsOpen)
            Resume();
    }

    private void OnGUI()
    {
        if (!GameSession.HasSelection || !IsOpen)
            return;

        PrepareStyles();

        // Затемнение игрового экрана, а не непрозрачный чёрный фон.
        GUI.color = new Color(0.01f, 0.015f, 0.03f, 0.55f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        bool modal = confirmationOpen;
        bool previousEnabled = GUI.enabled;
        GUI.enabled = !modal;
        DrawBoardPage(GetBoardRect(900f));
        GUI.enabled = previousEnabled;

        if (modal)
            DrawQuitConfirmation();
    }

    private void DrawBoardPage(Rect board)
    {
        DrawBoard(board);

        float centreX = board.center.x;
        float titleY = board.y + 50f;
        string title = page == Page.Main
            ? "ПАУЗА"
            : page == Page.Controls ? "УПРАВЛЕНИЕ" : "НАСТРОЙКИ";
        GUI.Label(new Rect(board.x, titleY, board.width, 58f), title, titleStyle);

        if (page == Page.Main)
            DrawMainPage(board, centreX);
        else if (page == Page.Controls)
            DrawControlsPage(board, centreX);
        else
            DrawSettingsPage(board, centreX);
    }

    private void DrawMainPage(Rect board, float centreX)
    {
        float width = Mathf.Min(480f, board.width * 0.54f);
        float height = 54f;
        float x = centreX - width * 0.5f;
        float y = board.y + 118f;

        if (GUI.Button(new Rect(x, y, width, height), "Продолжить", buttonStyle))
        {
            Resume();
            return;
        }
        y += 72f;
        if (GUI.Button(new Rect(x, y, width, height), "Управление", buttonStyle))
        {
            page = Page.Controls;
            return;
        }
        y += 72f;
        if (GUI.Button(new Rect(x, y, width, height), "Настройки", buttonStyle))
        {
            page = Page.Settings;
            return;
        }
        y += 72f;
        if (GUI.Button(new Rect(x, y, width, height), "Начать сначала", buttonStyle))
        {
            RestartLevel();
            return;
        }
        y += 72f;
        if (GUI.Button(new Rect(x, y, width, height), "В меню", buttonStyle))
        {
            confirmationOpen = true;
            return;
        }

        GUI.Label(new Rect(board.x, board.yMax - 48f, board.width, 26f),
            "1–5 — выбор · Esc — продолжить", hintStyle);
    }

    private void DrawControlsPage(Rect board, float centreX)
    {
        float left = board.x + board.width * 0.18f;
        float width = board.width * 0.64f;
        GUI.Label(new Rect(left, board.y + 128f, width, board.height - 230f),
            "Хомяк\n" +
            "A / D — движение, W — прыжок, F — удар.\n" +
            "Удерживай F — заряженный удар, Q — суперудар.\n" +
            "E — оживить напарника, R — взять или бросить.\n\n" +
            "Хомячиха\n" +
            "H / K — движение, U — прыжок, L — удар.\n" +
            "Удерживай L — заряженный удар, I — суперудар.\n" +
            "O — оживить напарника, P — взять или бросить.\n\n" +
            "Esc или Start — пауза.",
            bodyStyle);

        DrawBackButton(board, centreX);
    }

    private void DrawSettingsPage(Rect board, float centreX)
    {
        float width = Mathf.Min(480f, board.width * 0.54f);
        float x = centreX - width * 0.5f;
        float y = board.y + 125f;

        GUI.Label(new Rect(board.x, y, board.width, 30f),
            "Громкость: " + Mathf.RoundToInt(volume * 100f) + "%", hintStyle);
        y += 38f;
        float newVolume = GUI.HorizontalSlider(new Rect(x, y, width, 24f), volume, 0f, 1f);
        if (!Mathf.Approximately(newVolume, volume))
        {
            volume = newVolume;
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save();
        }

        y += 58f;
        if (GUI.Button(new Rect(x, y, width, 56f),
            "Полный экран: " + (fullscreen ? "Вкл" : "Выкл"), buttonStyle))
        {
            fullscreen = !fullscreen;
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
            ApplyFullscreen();
        }

        y += 74f;
        GUI.Label(new Rect(board.x, y, board.width, 30f),
            "Настройки сохраняются между запусками.", hintStyle);

        DrawBackButton(board, centreX);
    }

    private void DrawBackButton(Rect board, float centreX)
    {
        float width = Mathf.Min(480f, board.width * 0.54f);
        if (GUI.Button(new Rect(centreX - width * 0.5f, board.yMax - 84f, width, 56f),
            "Назад", buttonStyle))
        {
            page = Page.Main;
        }
    }

    private void DrawQuitConfirmation()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float width = Mathf.Min(680f, Screen.width - 80f);
        float height = width / BoardAspect;
        Rect board = new Rect((Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f, width, height);
        DrawBoard(board);

        GUI.Label(new Rect(board.x, board.y + 42f, board.width, 58f), "Уверены?", titleStyle);
        GUI.Label(new Rect(board.x + 50f, board.y + 110f, board.width - 100f, 70f),
            "Несохранённые данные пропадут.", popupBodyStyle);

        float buttonWidth = Mathf.Min(190f, board.width * 0.27f);
        float buttonHeight = 56f;
        float y = board.y + 225f;
        if (GUI.Button(new Rect(board.center.x - buttonWidth - 14f, y,
            buttonWidth, buttonHeight), "Да", buttonStyle))
        {
            ExitToMenu();
            return;
        }

        if (GUI.Button(new Rect(board.center.x + 14f, y,
            buttonWidth, buttonHeight), "Нет", buttonStyle))
            confirmationOpen = false;
    }

    private void DrawBoard(Rect board)
    {
        if (boardTexture != null)
        {
            GUI.DrawTexture(board, boardTexture, ScaleMode.ScaleToFit, true);
            return;
        }

        GUI.color = new Color(0.16f, 0.055f, 0.02f, 0.96f);
        GUI.DrawTexture(board, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private Rect GetBoardRect(float maxWidth)
    {
        float width = Mathf.Min(maxWidth, Screen.width - 80f);
        float height = width / BoardAspect;
        if (height > Screen.height - 80f)
        {
            height = Screen.height - 80f;
            width = height * BoardAspect;
        }

        return new Rect((Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f, width, height);
    }

    private void ApplyFullscreen()
    {
        if (Application.isEditor)
            return;

        Screen.fullScreenMode = fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
    }

    private void LoadBoardTexture()
    {
        Sprite sprite = Resources.Load<Sprite>(BoardResource);
        if (sprite != null)
        {
            boardTexture = sprite.texture;
            return;
        }

        boardTexture = Resources.Load<Texture2D>(BoardResource);
    }

    private void PrepareStyles()
    {
        if (titleStyle != null)
            return;

        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Tahoma" }, 24);
        if (font == null)
            font = GUI.skin.font;

        buttonNormal = CreateButtonTexture(
            new Color(0.16f, 0.055f, 0.02f, 0.92f),
            new Color(0.95f, 0.63f, 0.18f, 0.85f));
        buttonHover = CreateButtonTexture(
            new Color(0.34f, 0.14f, 0.045f, 0.96f),
            new Color(1f, 0.78f, 0.34f, 0.95f));
        buttonActive = CreateButtonTexture(
            new Color(0.48f, 0.21f, 0.07f, 0.98f),
            new Color(1f, 0.86f, 0.52f, 1f));

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.93f, 0.75f, 1f) }
        };

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = 18,
            wordWrap = true,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = new Color(0.92f, 0.96f, 1f, 1f) }
        };

        popupBodyStyle = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = 22,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.92f, 0.96f, 1f, 1f) }
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.9f, 0.95f, 1f, 0.9f) }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            font = font,
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(3, 3, 3, 3),
            padding = new RectOffset(14, 14, 6, 6),
            normal =
            {
                background = buttonNormal,
                textColor = new Color(1f, 0.93f, 0.75f, 1f)
            },
            hover =
            {
                background = buttonHover,
                textColor = Color.white
            },
            active =
            {
                background = buttonActive,
                textColor = Color.white
            },
            focused =
            {
                background = buttonHover,
                textColor = Color.white
            }
        };
    }

    private static Texture2D CreateButtonTexture(Color fill, Color border)
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool edge = x < 2 || x >= size - 2 || y < 2 || y >= size - 2;
                texture.SetPixel(x, y, edge ? border : fill);
            }
        }

        texture.Apply();
        return texture;
    }
}

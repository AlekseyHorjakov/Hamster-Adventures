using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Общая пауза и базовые настройки. Экран временный, выполнен в OnGUI,
/// чтобы не зависеть от отдельного UI-префаба.
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

    private Page page = Page.Main;
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;
    private GUIStyle controlsStyle;
    private LevelFlowController levelFlow;
    private float volume = 1f;
    private bool fullscreen;

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
            HandleMenuInput(keyboard, gamepad);
            return;
        }

        bool open = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        if (!open && gamepad != null)
            open = gamepad.startButton.wasPressedThisFrame;

        if (open)
            Open();
    }

    private void HandleMenuInput(Keyboard keyboard, Gamepad gamepad)
    {
        bool back = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        if (!back && gamepad != null)
            back = gamepad.startButton.wasPressedThisFrame;

        if (back)
        {
            if (page == Page.Main)
                Resume();
            else
                page = Page.Main;
            return;
        }

        if (page != Page.Main || keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            Resume();
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            page = Page.Controls;
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            page = Page.Settings;
        else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
            ExitToMenu();
    }

    private void Open()
    {
        page = Page.Main;
        GamePause.SetPaused(true);
        SetPlayerInputEnabled(false);
    }

    private void Resume()
    {
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

        GUI.color = new Color(0.015f, 0.025f, 0.05f, 0.91f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float width = Mathf.Min(760f, Screen.width - 48f);
        float height = Mathf.Min(650f, Screen.height - 48f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f,
            width, height);

        GUILayout.BeginArea(panel);
        GUILayout.Space(24f);
        GUILayout.Label(page == Page.Main ? "ПАУЗА" : page == Page.Controls ? "УПРАВЛЕНИЕ" : "НАСТРОЙКИ",
            titleStyle);
        GUILayout.Space(22f);

        if (page == Page.Main)
            DrawMainPage();
        else if (page == Page.Controls)
            DrawControlsPage();
        else
            DrawSettingsPage();

        GUILayout.EndArea();
    }

    private void DrawMainPage()
    {
        if (GUILayout.Button("1 — Продолжить", buttonStyle, GUILayout.Height(52f)))
            Resume();
        GUILayout.Space(10f);
        if (GUILayout.Button("2 — Управление", buttonStyle, GUILayout.Height(52f)))
            page = Page.Controls;
        GUILayout.Space(10f);
        if (GUILayout.Button("3 — Настройки", buttonStyle, GUILayout.Height(52f)))
            page = Page.Settings;
        GUILayout.Space(10f);
        if (GUILayout.Button("4 — Выйти в меню", buttonStyle, GUILayout.Height(52f)))
            ExitToMenu();

        GUILayout.FlexibleSpace();
        GUILayout.Label("1–4 — выбор · Esc — продолжить", textStyle);
        GUILayout.Space(12f);
    }

    private void DrawControlsPage()
    {
        GUILayout.Label(
            "Хомяк\n" +
            "A / D — движение, W — прыжок, F — удар.\n" +
            "Удерживай F — заряженный удар, Q — суперудар.\n" +
            "E — оживить напарника, R — взять или бросить.\n\n" +
            "Хомячиха\n" +
            "H / K — движение, U — прыжок, L — удар.\n" +
            "Удерживай L — заряженный удар, I — суперудар.\n" +
            "O — оживить напарника, P — взять или бросить.\n\n" +
            "Общее\n" +
            "Esc или Start — пауза.",
            controlsStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label("Esc — назад", textStyle);
        if (GUILayout.Button("Назад", buttonStyle, GUILayout.Height(48f)))
            page = Page.Main;
        GUILayout.Space(12f);
    }

    private void DrawSettingsPage()
    {
        GUILayout.Label("Громкость: " + Mathf.RoundToInt(volume * 100f) + "%", textStyle);
        float newVolume = GUILayout.HorizontalSlider(volume, 0f, 1f, GUILayout.Height(30f));
        if (!Mathf.Approximately(newVolume, volume))
        {
            volume = newVolume;
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save();
        }

        GUILayout.Space(24f);
        if (GUILayout.Button("Полный экран: " + (fullscreen ? "Вкл" : "Выкл"),
            buttonStyle, GUILayout.Height(48f)))
        {
            fullscreen = !fullscreen;
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
            ApplyFullscreen();
        }

        GUILayout.Space(12f);
        GUILayout.Label("Настройки сохраняются между запусками.", textStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label("Esc — назад", textStyle);
        if (GUILayout.Button("Назад", buttonStyle, GUILayout.Height(48f)))
            page = Page.Main;
        GUILayout.Space(12f);
    }

    private void ApplyFullscreen()
    {
        if (Application.isEditor)
            return;

        Screen.fullScreenMode = fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
    }

    private void PrepareStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.82f, 0.9f, 1f, 1f) }
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        controlsStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.9f, 0.95f, 1f, 1f) }
        };
    }
}

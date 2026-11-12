using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Стартовый экран: выбор режима и переход к вертикальному срезу.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private const string LevelSceneName = "Level01_VerticalSlice";

    public Button hamsterButton;
    public Button girlButton;
    public Button coopButton;
    public Button quitButton;

    private bool startingGame;

    private void Awake()
    {
        GameSession.Clear();
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ApplyMenuFont();
        UpdateBuildLabel();

        hamsterButton?.onClick.AddListener(SelectHamster);
        girlButton?.onClick.AddListener(SelectGirl);
        coopButton?.onClick.AddListener(SelectCoop);
        quitButton?.onClick.AddListener(QuitGame);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 pointer = mouse.position.ReadValue();
            if (Contains(hamsterButton, pointer))
            {
                SelectHamster();
                return;
            }
            if (Contains(girlButton, pointer))
            {
                SelectGirl();
                return;
            }
            if (Contains(coopButton, pointer))
            {
                SelectCoop();
                return;
            }
            if (Contains(quitButton, pointer))
            {
                QuitGame();
                return;
            }
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            SelectHamster();
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            SelectGirl();
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            SelectCoop();
        else if (keyboard.escapeKey.wasPressedThisFrame)
            QuitGame();
    }

    private static bool Contains(Button button, Vector2 screenPoint)
    {
        return button != null && RectTransformUtility.RectangleContainsScreenPoint(
            button.GetComponent<RectTransform>(), screenPoint, null);
    }

    public void SelectHamster() => StartGame(GameMode.SoloHamster);
    public void SelectGirl() => StartGame(GameMode.SoloHamsterGirl);
    public void SelectCoop() => StartGame(GameMode.Coop);

    private void StartGame(GameMode mode)
    {
        if (startingGame)
            return;

        startingGame = true;
        GameSession.SetMode(mode);
        Time.timeScale = 1f;
        SceneManager.LoadScene(LevelSceneName);
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

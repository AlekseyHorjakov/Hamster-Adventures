using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Временный экран выбора режима до появления полноценного главного меню.
/// 1 — соло за Хомяка, 2 — соло за Хомячиху, 3 — локальный кооп.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameModeSelector : MonoBehaviour
{
    private static readonly GUIStyle TitleStyle = new GUIStyle();
    private static readonly GUIStyle HintStyle = new GUIStyle();
    private static bool stylesReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (GameSession.HasSelection || Object.FindFirstObjectByType<GameModeSelector>() != null)
            return;

        new GameObject("GameModeSelector").AddComponent<GameModeSelector>();
    }

    private void Awake()
    {
        Time.timeScale = 0f;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            Select(GameMode.SoloHamster);
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            Select(GameMode.SoloHamsterGirl);
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            Select(GameMode.Coop);
    }

    private void OnGUI()
    {
        PrepareStyles();

        float width = Mathf.Min(520f, Screen.width - 40f);
        float height = 300f;
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

        GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.94f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(panel);
        GUILayout.Space(24f);
        GUILayout.Label("Hamster Adventures", TitleStyle);
        GUILayout.Space(8f);
        GUILayout.Label("Выбери режим игры", HintStyle);
        GUILayout.Space(24f);

        if (GUILayout.Button("1 — Один игрок: Хомяк", GUILayout.Height(48f)))
            Select(GameMode.SoloHamster);
        GUILayout.Space(8f);
        if (GUILayout.Button("2 — Один игрок: Хомячиха", GUILayout.Height(48f)))
            Select(GameMode.SoloHamsterGirl);
        GUILayout.Space(8f);
        if (GUILayout.Button("3 — Вдвоём на одном компьютере", GUILayout.Height(48f)))
            Select(GameMode.Coop);

        GUILayout.EndArea();
    }

    private void Select(GameMode mode)
    {
        GameSession.SetMode(mode);
        ApplyMode(mode);
        Time.timeScale = 1f;
        Destroy(gameObject);
    }

    private static void ApplyMode(GameMode mode)
    {
        HamsterMovement[] players = FindObjectsByType<HamsterMovement>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        HamsterMovement hamster = null;
        HamsterMovement girl = null;

        foreach (HamsterMovement player in players)
        {
            if (player == null)
                continue;

            if (player.name.Contains("HamsterGirl"))
                girl = player;
            else if (player.name.Contains("Hamster"))
                hamster = player;
        }

        if (hamster != null)
            hamster.gameObject.SetActive(mode != GameMode.SoloHamsterGirl);
        if (girl != null)
            girl.gameObject.SetActive(mode != GameMode.SoloHamster);
    }

    private static void PrepareStyles()
    {
        if (stylesReady)
            return;

        stylesReady = true;
        TitleStyle.fontSize = 32;
        TitleStyle.fontStyle = FontStyle.Bold;
        TitleStyle.alignment = TextAnchor.MiddleCenter;
        TitleStyle.normal.textColor = Color.white;

        HintStyle.fontSize = 20;
        HintStyle.alignment = TextAnchor.MiddleCenter;
        HintStyle.normal.textColor = new Color(0.8f, 0.88f, 1f, 1f);
    }
}

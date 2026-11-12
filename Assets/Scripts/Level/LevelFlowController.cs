using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Простой сценарий вертикального среза: стартовая зона, арена с гиенами,
/// мини-босс, открытие выхода и экран завершения. Без сложной системы диалогов.
/// </summary>
public class LevelFlowController : MonoBehaviour
{
    private enum Phase
    {
        Intro,
        Arena,
        Boss,
        ExitOpen,
        Completed,
        GameOver
    }

    [Header("Уровень")]
    [SerializeField] private string levelTitle = "Вертикальный срез";
    [SerializeField] private GameObject arenaRoot;
    [SerializeField] private HyenaAI[] arenaEnemies;
    [SerializeField] private GameObject boss;
    [SerializeField] private GameObject exitGate;

    private Phase phase = Phase.Intro;

    public bool IsEndScreen => phase == Phase.Completed || phase == Phase.GameOver;
    private Health bossHealth;
    private float levelStartedAt;
    private float completionTime;
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;

    private void Start()
    {
        GameSession.ApplyModeToPlayers();
        levelStartedAt = Time.unscaledTime;

        if (arenaRoot != null)
            arenaRoot.SetActive(false);

        if (boss != null)
        {
            boss.SetActive(false);
            bossHealth = boss.GetComponent<Health>();
        }

        if (exitGate != null)
            exitGate.SetActive(true);

        if (GameProgress.ConsumeResume(out CheckpointData checkpoint))
            ApplyCheckpoint(checkpoint);
        else if (GameProgress.ConsumeNewGame())
            SaveCheckpointAt(0);
    }

    private void Update()
    {
        if (!GameSession.HasSelection)
            return;

        if (phase == Phase.Completed || phase == Phase.GameOver)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.rKey.wasPressedThisFrame)
                    RestartScene();
                else if (Keyboard.current.enterKey.wasPressedThisFrame
                         || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                         || Keyboard.current.escapeKey.wasPressedThisFrame)
                    ReturnToMainMenu();
            }
            return;
        }

        if (AllPlayersDefeated())
        {
            phase = Phase.GameOver;
            Time.timeScale = 0f;
            return;
        }

        if (phase == Phase.Arena && AllArenaEnemiesDefeated())
        {
            phase = Phase.Boss;
            if (boss != null)
                boss.SetActive(true);
            SaveCheckpointAt(2);
            return;
        }

        if (phase == Phase.Boss && (boss == null || bossHealth == null || !bossHealth.IsAlive))
        {
            phase = Phase.ExitOpen;
            if (exitGate != null)
                exitGate.SetActive(false);
            SaveCheckpointAt(3);
        }
    }

    public void NotifyArenaEntered()
    {
        if (phase != Phase.Intro)
            return;

        phase = Phase.Arena;
        if (arenaRoot != null)
            arenaRoot.SetActive(true);
        SaveCheckpointAt(1);
    }

    public void NotifyFinishEntered()
    {
        if (phase != Phase.ExitOpen)
            return;

        phase = Phase.Completed;
        completionTime = Mathf.Max(0f, Time.unscaledTime - levelStartedAt);
        GameProgress.MarkLevelCompleted();
        Time.timeScale = 0f;
    }

    private void ApplyCheckpoint(CheckpointData checkpoint)
    {
        RestorePlayerPositions(checkpoint);

        if (checkpoint.checkpoint <= 0)
            return;

        if (arenaRoot != null)
            arenaRoot.SetActive(true);

        if (checkpoint.checkpoint >= 2)
        {
            SetArenaEnemiesActive(false);

            if (boss != null)
            {
                boss.SetActive(checkpoint.checkpoint < 3);
                bossHealth = boss.GetComponent<Health>();
            }

            if (checkpoint.checkpoint >= 3)
            {
                phase = Phase.ExitOpen;
                if (exitGate != null)
                    exitGate.SetActive(false);
            }
            else
            {
                phase = Phase.Boss;
            }
        }
        else
        {
            phase = Phase.Arena;
        }
    }

    private static void RestorePlayerPositions(CheckpointData checkpoint)
    {
        HamsterMovement[] players = FindObjectsByType<HamsterMovement>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (HamsterMovement player in players)
        {
            if (player == null)
                continue;

            bool isGirl = player.name.Contains("HamsterGirl");
            bool hasPosition = isGirl ? checkpoint.hasGirl : checkpoint.hasHamster;
            if (!hasPosition)
                continue;

            Vector2 position = isGirl
                ? new Vector2(checkpoint.girlX, checkpoint.girlY)
                : new Vector2(checkpoint.hamsterX, checkpoint.hamsterY);

            player.Body.position = position;
            player.Body.linearVelocity = Vector2.zero;
        }
    }

    private void SetArenaEnemiesActive(bool active)
    {
        if (arenaEnemies == null)
            return;

        foreach (HyenaAI enemy in arenaEnemies)
        {
            if (enemy != null)
                enemy.gameObject.SetActive(active);
        }
    }

    private static void SaveCheckpointAt(int checkpoint)
    {
        HamsterMovement[] players = FindObjectsByType<HamsterMovement>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        Vector2? hamster = null;
        Vector2? girl = null;

        foreach (HamsterMovement player in players)
        {
            if (player == null)
                continue;

            if (player.name.Contains("HamsterGirl"))
                girl = player.transform.position;
            else if (player.name.Contains("Hamster"))
                hamster = player.transform.position;
        }

        GameProgress.SaveCheckpoint(checkpoint, hamster, girl);
    }
    private bool AllArenaEnemiesDefeated()
    {
        if (arenaEnemies == null || arenaEnemies.Length == 0)
            return true;

        foreach (HyenaAI enemy in arenaEnemies)
        {
            if (enemy == null)
                continue;

            Health health = enemy.GetComponent<Health>();
            if (health != null && health.IsAlive)
                return false;
        }

        return true;
    }

    private static bool AllPlayersDefeated()
    {
        PlayerCombatState[] players = FindObjectsByType<PlayerCombatState>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (players.Length == 0)
            return false;

        foreach (PlayerCombatState player in players)
        {
            if (player == null)
                continue;

            if (player.IsDead)
                continue;

            Health health = player.GetComponent<Health>();
            if (health != null && health.IsAlive)
                return false;
        }

        return true;
    }

    private static void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        GameSession.Clear();
        SceneManager.LoadScene("MainMenu");
    }

    private string CompletionDetails()
    {
        int totalSeconds = Mathf.FloorToInt(completionTime);
        string time = string.Format("{0:00}:{1:00}", totalSeconds / 60, totalSeconds % 60);
        string mode = GameSession.Mode == GameMode.Coop
            ? "Два игрока"
            : GameSession.Mode == GameMode.SoloHamsterGirl
                ? "Соло: Хомячиха"
                : "Соло: Хомяк";
        return "Время: " + time + "\nРежим: " + mode;
    }
    private void RestartScene()
    {
        Time.timeScale = 1f;
        GameProgress.StartNew(GameSession.Mode);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private string ObjectiveText()
    {
        switch (phase)
        {
            case Phase.Intro:
                return "Иди вправо. Потренируйся на манекенах.";
            case Phase.Arena:
                return "Победи гиен на арене.";
            case Phase.Boss:
                return "Победи мини-босса.";
            case Phase.ExitOpen:
                return "Выход открыт. Иди к финишу.";
            default:
                return "";
        }
    }

    private void OnGUI()
    {
        if (!GameSession.HasSelection)
            return;

        PrepareStyles();

        if (phase != Phase.Completed && phase != Phase.GameOver)
        {
            GUI.Label(new Rect(24f, 18f, Screen.width - 48f, 32f), levelTitle, titleStyle);
            GUI.Label(new Rect(24f, 52f, Screen.width - 48f, 30f), ObjectiveText(), textStyle);
            GUI.Label(new Rect(Screen.width - 300f, Screen.height - 28f, 280f, 22f),
                "build " + Application.version, textStyle);
            return;
        }

        GUI.color = new Color(0.02f, 0.03f, 0.06f, 0.92f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float width = Mathf.Min(620f, Screen.width - 40f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.28f, width, 330f);
        GUILayout.BeginArea(panel);
        GUILayout.Label(phase == Phase.Completed ? "УРОВЕНЬ ПРОЙДЕН!" : "ИГРА ОКОНЧЕНА", titleStyle);
        GUILayout.Space(18f);
        GUILayout.Label(phase == Phase.Completed
            ? CompletionDetails()
            : "Все доступные герои выведены из строя.", textStyle);
        GUILayout.Space(24f);
        if (GUILayout.Button("В главное меню", buttonStyle, GUILayout.Height(52f)))
        {
            ReturnToMainMenu();
            return;
        }
        GUILayout.Space(10f);
        if (GUILayout.Button(phase == Phase.Completed ? "Пройти ещё раз" : "Начать сначала",
            buttonStyle, GUILayout.Height(52f)))
        {
            RestartScene();
            return;
        }
        GUILayout.Space(12f);
        GUILayout.Label("R — быстрый перезапуск", textStyle);
        GUILayout.EndArea();
    }

    private void PrepareStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.85f, 0.92f, 1f, 1f) }
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
    }
}

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
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;

    private void Start()
    {
        if (arenaRoot != null)
            arenaRoot.SetActive(false);

        if (boss != null)
        {
            boss.SetActive(false);
            bossHealth = boss.GetComponent<Health>();
        }

        if (exitGate != null)
            exitGate.SetActive(true);
    }

    private void Update()
    {
        if (!GameSession.HasSelection)
            return;

        if (phase == Phase.Completed || phase == Phase.GameOver)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                RestartScene();
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
            return;
        }

        if (phase == Phase.Boss && (boss == null || bossHealth == null || !bossHealth.IsAlive))
        {
            phase = Phase.ExitOpen;
            if (exitGate != null)
                exitGate.SetActive(false);
        }
    }

    public void NotifyArenaEntered()
    {
        if (phase != Phase.Intro)
            return;

        phase = Phase.Arena;
        if (arenaRoot != null)
            arenaRoot.SetActive(true);
    }

    public void NotifyFinishEntered()
    {
        if (phase != Phase.ExitOpen)
            return;

        phase = Phase.Completed;
        Time.timeScale = 0f;
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

            Health health = player.GetComponent<Health>();
            if (health != null && health.IsAlive)
                return false;
        }

        return true;
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
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
        Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.32f, width, 240f);
        GUILayout.BeginArea(panel);
        GUILayout.Label(phase == Phase.Completed ? "ЭТАП ПРОЙДЕН!" : "ИГРА ОКОНЧЕНА", titleStyle);
        GUILayout.Space(20f);
        GUILayout.Label(phase == Phase.Completed
            ? "Основной игровой цикл среза работает."
            : "Оба доступных героя выведены из строя.", textStyle);
        GUILayout.Space(26f);
        if (GUILayout.Button("R — пройти заново", buttonStyle, GUILayout.Height(52f)))
            RestartScene();
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

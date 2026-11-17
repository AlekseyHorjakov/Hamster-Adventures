using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Управляет потоком первого уровня. Вертикальный срез использует старые
/// arena/boss-поля, а полный уровень — комнаты, чекпоинты и боевые встречи.
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
        GameOver,
        ZoneCheckpoint
    }

    [Header("Уровень")]
    [SerializeField] private string levelTitle = "Hamster Adventures — Первый уровень";
    [SerializeField] private bool useRoomFlow;
    [SerializeField] private RoomDefinition startRoom;
    [SerializeField] private RoomDefinition[] roomDefinitions;

    [Header("Вертикальный срез")]
    [SerializeField] private GameObject arenaRoot;
    [SerializeField] private HyenaAI[] arenaEnemies;
    [SerializeField] private GameObject boss;
    [SerializeField] private GameObject exitGate;

    private Phase phase = Phase.Intro;
    private RoomDefinition currentRoom;

    private Health bossHealth;
    private float levelStartedAt;
    private float completionTime;
    private int roomsCompleted;

    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;
    private GUIStyle bannerStyle;
    private GUIStyle messageStyle;
    private Texture2D titleRibbon;

    private float roomBannerUntil;
    private string roomBannerText = string.Empty;
    private string messageText = string.Empty;
    private float messageUntil;
    private bool zoneCheckpointReached;

    private Vector2 initialHamsterPosition;
    private Vector2 initialGirlPosition;
    private Vector2 lastHamsterRespawn;
    private Vector2 lastGirlRespawn;
    private RoomDefinition respawnRoom;

    public bool IsEndScreen => phase == Phase.Completed || phase == Phase.GameOver;
    public RoomDefinition CurrentRoom => currentRoom;
    public int RoomsCompleted => roomsCompleted;

    private void Start()
    {
        GameSession.ApplyModeToPlayers();
        levelStartedAt = Time.unscaledTime;

        if (useRoomFlow)
        {
            StartRoomFlow();
            return;
        }

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

    private void StartRoomFlow()
    {
        RoomDefinition room = startRoom;
        if (room == null && roomDefinitions != null && roomDefinitions.Length > 0)
            room = roomDefinitions[0];

        CaptureInitialPlayerPositions();

        if (GameProgress.ConsumeResume(out CheckpointData checkpoint))
        {
            ApplyCheckpoint(checkpoint);
            room = FindRoom(checkpoint.roomId) ?? room;

            if (room != null)
                EnterRoom(room, false);
            ShowMessage("Продолжение с чекпоинта: " + checkpoint.checkpointName);
        }
        else
        {
            GameProgress.ConsumeNewGame();
            if (room != null)
            {
                EnterRoom(room, true);
                SaveCheckpointFromPlayers(0, room, "Старт лагеря");
            }
        }
    }

    private void Update()
    {
        if (!GameSession.HasSelection)
            return;

        if (zoneCheckpointReached)
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

        if (useRoomFlow)
        {
            UpdateCurrentRoomFromPlayers();
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

    private void UpdateCurrentRoomFromPlayers()
    {
        HamsterMovement[] players = FindObjectsByType<HamsterMovement>(FindObjectsInactive.Exclude);
        if (players.Length == 0 || roomDefinitions == null)
            return;

        float leadX = float.MinValue;
        foreach (HamsterMovement player in players)
        {
            if (player != null)
                leadX = Mathf.Max(leadX, player.transform.position.x);
        }

        RoomDefinition target = null;
        foreach (RoomDefinition room in roomDefinitions)
        {
            if (room != null && leadX >= room.WorldMinX && leadX < room.WorldMaxX)
            {
                target = room;
                break;
            }
        }

        if (target != null && target != currentRoom)
            EnterRoom(target, true);
    }

    public void EnterRoom(RoomDefinition room, bool showBanner = true)
    {
        if (room == null)
            return;

        bool changed = currentRoom != room;
        currentRoom = room;

        Camera camera = Camera.main;
        CameraFollow follow = camera != null ? camera.GetComponent<CameraFollow>() : null;
        if (follow != null)
        {
            follow.SetWorldBounds(
                new Vector2(room.WorldMinX, -24f),
                new Vector2(room.WorldMaxX, 36f));
            // Ниже линии земли камера не опускается даже при падении в яму.
            follow.SetMinY(room.SurfaceY + 2f);
        }

        if (showBanner && changed)
        {
            roomBannerText = string.Format("Комната {0:00} · {1}", room.RoomId, room.DisplayName);
            roomBannerUntil = Time.unscaledTime + 1.8f;
        }
    }

    /// <summary>Сохраняет чекпоинт по текущим позициям активных героев.</summary>
    public void SaveCheckpointFromPlayers(int checkpointIndex, RoomDefinition room, string checkpointLabel)
    {
        Vector2? hamster = null;
        Vector2? girl = null;

        HamsterMovement[] players = FindObjectsByType<HamsterMovement>(FindObjectsInactive.Exclude);
        foreach (HamsterMovement player in players)
        {
            if (player == null)
                continue;

            if (player.name.Contains("HamsterGirl"))
                girl = player.transform.position;
            else if (player.name.Contains("Hamster"))
                hamster = player.transform.position;
        }

        if (hamster.HasValue) lastHamsterRespawn = hamster.Value;
        if (girl.HasValue) lastGirlRespawn = girl.Value;

        RoomDefinition sourceRoom = room != null ? room : currentRoom;
        respawnRoom = sourceRoom;
        int roomId = sourceRoom != null ? sourceRoom.RoomId : 1;
        int zoneId = sourceRoom != null ? sourceRoom.ZoneId : 1;
        string roomName = sourceRoom != null ? sourceRoom.DisplayName : "Лагерь";

        GameProgress.SaveRoomCheckpoint(checkpointIndex, roomId, zoneId, roomName,
            checkpointLabel, hamster, girl);
    }

    /// <summary>
    /// Возвращает всю активную пару к последнему чекпоинту. В кооперативе
    /// нельзя оставлять второго героя в другой комнате после падения первого.
    /// </summary>
    public void RespawnPlayer(PlayerCombatState state)
    {
        if (state == null)
            return;

        PlayerCombatState[] states = FindObjectsByType<PlayerCombatState>(FindObjectsInactive.Exclude);
        foreach (PlayerCombatState player in states)
        {
            if (player == null)
                continue;

            bool isGirl = player.name.Contains("HamsterGirl");
            Vector2 position = isGirl ? lastGirlRespawn : lastHamsterRespawn;

            if (position == Vector2.zero)
                position = isGirl ? initialGirlPosition : initialHamsterPosition;

            player.RespawnAt(position, 0.6f);
        }

        if (respawnRoom != null)
            EnterRoom(respawnRoom, true);
    }

    public void ShowMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        messageText = message;
        messageUntil = Time.unscaledTime + 3.2f;
    }

    public void NotifyEncounterCleared(RoomDefinition room, string encounterName)
    {
        roomsCompleted++;
        ShowMessage(encounterName + " завершён. Проход открыт.");
        if (room != null)
            EnterRoom(room, false);
    }

    public void NotifyZoneCheckpoint(int completedRoomCount)
    {
        if (zoneCheckpointReached)
            return;

        roomsCompleted = Mathf.Max(roomsCompleted, completedRoomCount);
        zoneCheckpointReached = true;
        phase = Phase.ZoneCheckpoint;
        Time.timeScale = 0f;
        Cursor.visible = true;
    }

    public void NotifyArenaEntered()
    {
        if (useRoomFlow || phase != Phase.Intro)
            return;

        phase = Phase.Arena;
        if (arenaRoot != null)
            arenaRoot.SetActive(true);
        SaveCheckpointAt(1);
    }

    public void NotifyFinishEntered()
    {
        if (useRoomFlow || phase != Phase.ExitOpen)
            return;

        phase = Phase.Completed;
        completionTime = Mathf.Max(0f, Time.unscaledTime - levelStartedAt);
        GameProgress.MarkLevelCompleted();
        Time.timeScale = 0f;
    }

    private void ApplyCheckpoint(CheckpointData checkpoint)
    {
        RestorePlayerPositions(checkpoint);

        if (checkpoint.hasHamster)
            lastHamsterRespawn = new Vector2(checkpoint.hamsterX, checkpoint.hamsterY);
        if (checkpoint.hasGirl)
            lastGirlRespawn = new Vector2(checkpoint.girlX, checkpoint.girlY);

        if (useRoomFlow)
        {
            respawnRoom = FindRoom(checkpoint.roomId);
            return;
        }

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

    private RoomDefinition FindRoom(int roomId)
    {
        if (roomDefinitions == null)
            return null;

        foreach (RoomDefinition room in roomDefinitions)
        {
            if (room != null && room.RoomId == roomId)
                return room;
        }

        return null;
    }

    private void CaptureInitialPlayerPositions()
    {
        HamsterMovement[] players = FindObjectsByType<HamsterMovement>(FindObjectsInactive.Exclude);
        foreach (HamsterMovement player in players)
        {
            if (player == null)
                continue;

            if (player.name.Contains("HamsterGirl"))
                initialGirlPosition = player.transform.position;
            else if (player.name.Contains("Hamster"))
                initialHamsterPosition = player.transform.position;
        }

        lastHamsterRespawn = initialHamsterPosition;
        lastGirlRespawn = initialGirlPosition;
    }

    private static void RestorePlayerPositions(CheckpointData checkpoint)
    {
        HamsterMovement[] players = FindObjectsByType<HamsterMovement>(FindObjectsInactive.Exclude);

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
            player.transform.position = position;
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

    private void SaveCheckpointAt(int checkpoint)
    {
        HamsterMovement[] players = FindObjectsByType<HamsterMovement>(FindObjectsInactive.Exclude);

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
        PlayerCombatState[] players = FindObjectsByType<PlayerCombatState>(FindObjectsInactive.Exclude);

        if (players.Length == 0)
            return false;

        foreach (PlayerCombatState player in players)
        {
            if (player == null || player.IsRespawning)
                return false;

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
        if (useRoomFlow)
            return currentRoom != null ? currentRoom.Objective : string.Empty;

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
                return string.Empty;
        }
    }

    private void OnGUI()
    {
        if (!GameSession.HasSelection)
            return;

        PrepareStyles();

        if (phase == Phase.ZoneCheckpoint)
        {
            DrawZoneCheckpointPanel();
            return;
        }

        if (phase == Phase.Completed || phase == Phase.GameOver)
        {
            DrawEndPanel();
            return;
        }

        string roomLine = currentRoom != null
            ? string.Format("{0:00}/18 · {1} · {2}", currentRoom.RoomId, currentRoom.DisplayName, currentRoom.ZoneName)
            : levelTitle;

        float hudSafeLeft = Mathf.Min(440f, Screen.width * 0.31f);
        float hudSafeRight = Mathf.Min(440f, Screen.width * 0.31f);
        float hudWidth = Mathf.Max(220f, Screen.width - hudSafeLeft - hudSafeRight - 24f);
        GUI.Label(new Rect(hudSafeLeft, 16f, hudWidth, 32f), roomLine, titleStyle);
        GUI.Label(new Rect(hudSafeLeft, 50f, hudWidth, 30f), ObjectiveText(), textStyle);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        GUI.Label(new Rect(Screen.width - 300f, Screen.height - 28f, 280f, 22f),
            "build " + Application.version, textStyle);
#endif

        DrawRoomBanner();
        DrawMessage();
    }

    private void DrawRoomBanner()
    {
        if (Time.unscaledTime > roomBannerUntil)
            return;

        if (titleRibbon == null)
            titleRibbon = Resources.Load<Texture2D>("Level01/Zone1/UI/RoomTitle_Ribbon");

        float alpha = Mathf.Clamp01((roomBannerUntil - Time.unscaledTime) / 0.45f);
        float width = Mathf.Min(460f, Screen.width * 0.45f);
        float height = width * 0.333f;
        Rect rect = new Rect((Screen.width - width) * 0.5f, 92f, width, height);

        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        if (titleRibbon != null)
            GUI.DrawTexture(rect, titleRibbon, ScaleMode.ScaleToFit);
        else
            GUI.Box(rect, GUIContent.none);

        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(new Rect(rect.x + 40f, rect.y + height * 0.22f, rect.width - 80f, height * 0.45f),
            roomBannerText, bannerStyle);
        GUI.color = previous;
    }

    private void DrawMessage()
    {
        if (string.IsNullOrEmpty(messageText) || Time.unscaledTime > messageUntil)
            return;

        float alpha = Mathf.Clamp01((messageUntil - Time.unscaledTime) / 0.35f);
        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(new Rect(0f, Screen.height - 120f, Screen.width, 40f), messageText, messageStyle);
        GUI.color = previous;
    }

    private void DrawZoneCheckpointPanel()
    {
        GUI.color = new Color(0.02f, 0.04f, 0.05f, 0.88f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float width = Mathf.Min(720f, Screen.width - 40f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.22f, width, 410f);
        GUILayout.BeginArea(panel);
        GUILayout.Label("ЗОНА ПРОЙДЕНА", titleStyle);
        GUILayout.Space(14f);
        GUILayout.Label("Комнаты 01–05 соединены и готовы к игровому тестированию.\n" +
                        "Следующая контрольная точка — зона II «Озеро» (комнаты 06–10).", textStyle);
        GUILayout.Space(20f);
        GUILayout.Label("Пройдено комнат: " + roomsCompleted, textStyle);
        GUILayout.Space(24f);
        if (GUILayout.Button("Начать зону заново", buttonStyle, GUILayout.Height(52f)))
        {
            RestartScene();
            return;
        }
        GUILayout.Space(10f);
        if (GUILayout.Button("В главное меню", buttonStyle, GUILayout.Height(52f)))
        {
            ReturnToMainMenu();
            return;
        }
        GUILayout.EndArea();
    }

    private void DrawEndPanel()
    {
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
            fontSize = 26,
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
        bannerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.28f, 0.12f, 0.04f, 1f) }
        };
        messageStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 21,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.92f, 0.55f, 1f) }
        };
    }
}


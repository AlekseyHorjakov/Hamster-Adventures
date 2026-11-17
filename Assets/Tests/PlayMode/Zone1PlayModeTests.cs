using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Play Mode smoke-тест зоны I. Тест намеренно обращается к игровым типам
/// через reflection, чтобы не менять существующую архитектуру сборок проекта.
/// </summary>
public sealed class Zone1PlayModeTests
{
    private static Type TypeOf(string name)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name, false))
            .FirstOrDefault(type => type != null);
    }

    private static Component FindComponent(string typeName)
    {
        Type type = TypeOf(typeName);
        return type == null
            ? null
            : UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .OfType<Component>()
                .FirstOrDefault();
    }

    private static Component[] FindComponents(string typeName)
    {
        Type type = TypeOf(typeName);
        if (type == null)
            return Array.Empty<Component>();

        return UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .OfType<Component>()
            .ToArray();
    }

    [UnityTest]
    public IEnumerator ZoneOne_CoopRoute_Encounters_Respawn_And_ZoneEnd()
    {
        Type gameSession = TypeOf("GameSession");
        Type gameProgress = TypeOf("GameProgress");
        Type gameMode = TypeOf("GameMode");
        Assert.IsNotNull(gameSession, "GameSession type");
        Assert.IsNotNull(gameProgress, "GameProgress type");
        Assert.IsNotNull(gameMode, "GameMode type");

        object coop = Enum.Parse(gameMode, "Coop");
        gameSession.GetMethod("SetMode", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { coop });
        gameProgress.GetMethod("StartNew", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { coop });

        AsyncOperation load = SceneManager.LoadSceneAsync("Level01_Full");
        while (load != null && !load.isDone)
            yield return null;

        yield return null;
        yield return new WaitForSecondsRealtime(0.6f);

        Component flow = FindComponent("LevelFlowController");
        Assert.IsNotNull(flow, "LevelFlowController найден");

        Component[] players = FindComponents("HamsterMovement");
        Assert.AreEqual(2, players.Length, "В кооперативе два активных героя");

        object currentRoom = flow.GetType().GetProperty("CurrentRoom")?.GetValue(flow);
        Assert.IsNotNull(currentRoom, "Текущая комната назначена");
        Assert.AreEqual(1, currentRoom.GetType().GetProperty("RoomId")?.GetValue(currentRoom), "Старт в комнате 01");

        Component hamster = players.FirstOrDefault(p => p.name == "Hamster");
        Component girl = players.FirstOrDefault(p => p.name == "HamsterGirl");
        Assert.IsNotNull(hamster, "Хомяк найден");
        Assert.IsNotNull(girl, "Хомячиха найдена");
        Assert.IsNotNull(GameObject.Find("GladeTrainingDummy"), "Манекен комнаты 03 присутствует");
        Assert.IsNotNull(GameObject.Find("TrainingGroundDummy_1"), "Первый манекен комнаты 04 присутствует");
        Assert.IsNotNull(GameObject.Find("TrainingGroundDummy_2"), "Второй манекен комнаты 04 присутствует");
        Assert.IsNotNull(GameObject.Find("BakedLevelArt"), "Запечённый арт зоны I присутствует");
        Assert.IsNull(FindComponent("LevelArtController"), "Runtime-арт вертикального среза не дублируется");

        GameObject campCheckpointObject = GameObject.Find("Checkpoint_Camp");
        Assert.IsNotNull(campCheckpointObject, "Стартовый чекпоинт найден по имени");
        Component checkpoint = campCheckpointObject.GetComponent(TypeOf("RoomCheckpoint"));
        Assert.IsNotNull(checkpoint, "Стартовый чекпоинт найден");
        SpriteRenderer flame = GetPrivate<SpriteRenderer>(checkpoint, "flameRenderer");
        Assert.IsNotNull(flame, "Огонь чекпоинта найден");
        Assert.IsFalse(flame.enabled, "До прохода героев костёр не горит");
        yield return MovePlayer(hamster, new Vector2(-18f, 1.2f));
        yield return new WaitForSecondsRealtime(0.25f);
        Assert.IsTrue(flame.enabled, "Костёр загорается при активации чекпоинта");

        GameObject campGround = GameObject.Find("CampGround");
        Assert.IsNotNull(campGround, "Основная земля лагеря найдена");
        Assert.IsNull(campGround.GetComponent<PlatformEffector2D>(), "Земля лагеря имеет твёрдый коллайдер");

        yield return MovePlayer(hamster, new Vector2(48.8f, 1.2f));
        yield return new WaitForSecondsRealtime(0.2f);
        AssertRoom(flow, 2, "Переход 01 → 02");

        yield return MovePlayer(girl, new Vector2(45f, 1.2f));
        yield return MovePlayer(hamster, new Vector2(47.2f, 1.2f));
        yield return new WaitForSecondsRealtime(1.5f);
        AssertRoom(flow, 1, "Возврат 02 → 01 обновляет комнату");
        Camera backCamera = Camera.main;
        Vector3 backHamsterView = backCamera.WorldToViewportPoint(hamster.transform.position);
        Vector3 backGirlView = backCamera.WorldToViewportPoint(girl.transform.position);
        Debug.Log($"[Zone1Test] back camera={backCamera.transform.position} size={backCamera.orthographicSize} aspect={backCamera.aspect} hamsterView={backHamsterView} girlView={backGirlView}");
        Assert.IsTrue(backHamsterView.x > 0f && backHamsterView.x < 1f && backHamsterView.y > 0f && backHamsterView.y < 1f,
            "Камера видит Хомяка при возврате в лагерь");
        Assert.IsTrue(backGirlView.x > 0f && backGirlView.x < 1f && backGirlView.y > 0f && backGirlView.y < 1f,
            "Камера видит Хомячиху при возврате в лагерь");

        yield return MovePlayer(hamster, new Vector2(48.8f, 1.2f));
        yield return new WaitForSecondsRealtime(0.2f);
        yield return MovePlayer(hamster, new Vector2(84.8f, 1.2f));
        yield return new WaitForSecondsRealtime(0.2f);
        AssertRoom(flow, 3, "Переход 02 → 03");

        yield return MovePlayer(hamster, new Vector2(122.8f, 3.2f));
        yield return new WaitForSecondsRealtime(0.2f);
        AssertRoom(flow, 4, "Переход 03 → 04");

        yield return MovePlayer(hamster, new Vector2(123.6f, 5f));
        yield return new WaitForSecondsRealtime(1.0f);
        Assert.Greater(hamster.transform.position.y, 3.2f,
            "В начале комнаты 04 герой не проваливается сквозь землю");

        Component encounter = FindComponent("EncounterController");
        Assert.IsNotNull(encounter, "Encounter комнаты 04 найден");

        yield return MovePlayer(hamster, new Vector2(123.6f, 3.2f));
        yield return new WaitForSecondsRealtime(0.35f);

        GameObject[] enemies = GetPrivateArray<GameObject>(encounter, "enemies");
        GameObject[] gates = GetPrivateArray<GameObject>(encounter, "gates");
        Assert.AreEqual(2, enemies.Length, "В комнате 04 две гиены");
        Assert.IsTrue(enemies.All(enemy => enemy != null && enemy.activeInHierarchy),
            "Гиены активируются при входе");
        Assert.IsTrue(gates.Length == 1 && gates[0].activeInHierarchy,
            "Ворота закрываются на время боя");

        Type healthType = TypeOf("Health");
        foreach (GameObject enemy in enemies)
        {
            Component health = enemy.GetComponent(healthType);
            healthType.GetMethod("Kill", BindingFlags.Public | BindingFlags.Instance).Invoke(health, null);
        }

        yield return new WaitForSecondsRealtime(0.45f);
        Assert.IsTrue((bool)encounter.GetType().GetProperty("IsCleared").GetValue(encounter),
            "Encounter засчитывает зачистку");
        Assert.IsTrue(gates.Length == 1 && !gates[0].activeInHierarchy,
            "Ворота открываются после боя");

        GameObject crate = GameObject.Find("TrainingCrate");
        Assert.IsNotNull(crate, "Разрушаемый ящик найден");
        Component crateHealth = crate.GetComponent(healthType);
        Collider2D crateCollider = crate.GetComponent<Collider2D>();
        healthType.GetMethod("Kill", BindingFlags.Public | BindingFlags.Instance).Invoke(crateHealth, null);
        yield return null;
        Assert.IsFalse(crateCollider.enabled, "Ящик отключает коллайдер после разрушения");
        Component[] rewards = FindComponents("RewardPickup");
        Assert.IsTrue(rewards.Length > 0, "Из ящика появляется награда");
        SpriteRenderer acornRenderer = rewards[0].GetComponent<SpriteRenderer>();
        Assert.IsNotNull(acornRenderer, "У награды есть спрайт");
        Assert.Greater(acornRenderer.bounds.size.y, 1.5f, "Жёлудь сопоставим по размеру с головой героя");

        yield return MovePlayer(hamster, new Vector2(160.8f, 3.2f));
        yield return new WaitForSecondsRealtime(0.2f);
        AssertRoom(flow, 5, "Переход 04 → 05");

        yield return MovePlayer(hamster, new Vector2(178f, 7f));
        yield return new WaitForSecondsRealtime(1.0f);
        Assert.Greater(hamster.transform.position.y, 3.8f,
            "Герой стоит на секции моста, а не парит и не проваливается");

        yield return MovePlayer(hamster, new Vector2(166f, 3.2f));
        yield return MovePlayer(girl, new Vector2(198f, 3.2f));
        yield return new WaitForSecondsRealtime(0.7f);

        Camera camera = Camera.main;
        Assert.IsNotNull(camera, "Main Camera найдена");
        Vector3 hamsterView = camera.WorldToViewportPoint(hamster.transform.position);
        Vector3 girlView = camera.WorldToViewportPoint(girl.transform.position);
        Assert.IsTrue(hamsterView.x > 0f && hamsterView.x < 1f && hamsterView.y > 0f && hamsterView.y < 1f,
            "Камера видит Хомяка при кооперативном расхождении");
        Assert.IsTrue(girlView.x > 0f && girlView.x < 1f && girlView.y > 0f && girlView.y < 1f,
            "Камера видит Хомячиху при кооперативном расхождении");

        Component hamsterState = hamster.GetComponent(TypeOf("PlayerCombatState"));
        Assert.IsNotNull(hamsterState, "Состояние героя найдено");

        // Яма комнаты 03 имеет дно и траву: в неё можно спрыгнуть и остаться внутри.
        GameObject pitFloor = GameObject.Find("GladePitVisual_Floor");
        Assert.IsNotNull(pitFloor, "У ямы комнаты 03 есть дно");
        Collider2D pitFloorCollider = pitFloor.GetComponent<Collider2D>();
        Assert.IsNotNull(pitFloorCollider, "У дна ямы есть коллайдер");
        Assert.Greater(pitFloorCollider.bounds.size.x, 6f, "Дно ямы шире разрыва — у краёв нет щели");
        yield return MovePlayer(hamster, new Vector2(105f, 0.5f));
        yield return new WaitForSecondsRealtime(1.3f);
        Assert.Greater(hamster.transform.position.y, -3.4f,
            "Герой стоит на дне ямы комнаты 03, а не улетает вниз");
        Assert.IsFalse((bool)hamsterState.GetType().GetProperty("IsDead").GetValue(hamsterState),
            "Яма с дном не убивает героя");
        Assert.IsFalse((bool)hamsterState.GetType().GetProperty("IsRespawning").GetValue(hamsterState),
            "Респаун в яме с дном не запускается");
        Camera pitCamera = Camera.main;
        Assert.Greater(pitCamera.transform.position.y, 2f,
            "Камера не уезжает под землю при падении в яму");
        yield return MovePlayer(hamster, new Vector2(110f, 2.2f));

        yield return MovePlayer(hamster, new Vector2(180f, -3.0f));
        yield return new WaitForSecondsRealtime(1.4f);
        Assert.IsFalse((bool)hamsterState.GetType().GetProperty("IsRespawning").GetValue(hamsterState),
            "Респаун из обрыва завершается");
        Assert.Less(hamster.transform.position.x, 0f, "Обрыв возвращает Хомяка к чекпоинту в лагере");
        Assert.Less(girl.transform.position.x, 0f, "Напарник также возвращается к чекпоинту");
        AssertRoom(flow, 1, "После падения камера и комната возвращаются в лагерь");

        yield return MovePlayer(hamster, new Vector2(201.4f, 5.5f));
        yield return new WaitForSecondsRealtime(0.35f);
        Assert.AreEqual(0f, Time.timeScale, "Триггер конца зоны I срабатывает");

        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator ZoneOne_SoloHamster_StartsInCamp()
    {
        yield return LoadLevel("SoloHamster");
        Component flow = FindComponent("LevelFlowController");
        Assert.IsNotNull(flow, "LevelFlowController найден в соло");
        Component[] players = FindComponents("HamsterMovement");
        Assert.AreEqual(1, players.Length, "В соло активен один герой");
        Assert.AreEqual("Hamster", players[0].name, "Выбран Хомяк");
        AssertRoom(flow, 1, "Соло Хомяка начинается в лагере");
    }

    [UnityTest]
    public IEnumerator ZoneOne_SoloGirl_StartsInCamp()
    {
        yield return LoadLevel("SoloHamsterGirl");
        Component flow = FindComponent("LevelFlowController");
        Assert.IsNotNull(flow, "LevelFlowController найден в соло Хомячихи");
        Component[] players = FindComponents("HamsterMovement");
        Assert.AreEqual(1, players.Length, "В соло активен один герой");
        Assert.AreEqual("HamsterGirl", players[0].name, "Выбрана Хомячиха");
        AssertRoom(flow, 1, "Соло Хомячихи начинается в лагере");
    }

    private static IEnumerator LoadLevel(string modeName)
    {
        Type gameSession = TypeOf("GameSession");
        Type gameProgress = TypeOf("GameProgress");
        Type gameMode = TypeOf("GameMode");
        object mode = Enum.Parse(gameMode, modeName);
        gameSession.GetMethod("SetMode", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { mode });
        gameProgress.GetMethod("StartNew", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { mode });

        AsyncOperation load = SceneManager.LoadSceneAsync("Level01_Full");
        while (load != null && !load.isDone)
            yield return null;

        yield return null;
        yield return new WaitForSecondsRealtime(0.5f);
    }
    private static IEnumerator MovePlayer(Component player, Vector2 position)
    {
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Assert.IsNotNull(body, "У героя есть Rigidbody2D");
        body.simulated = true;
        body.position = position;
        body.linearVelocity = Vector2.zero;
        player.transform.position = position;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
    }

    private static void AssertRoom(Component flow, int roomId, string message)
    {
        object room = flow.GetType().GetProperty("CurrentRoom")?.GetValue(flow);
        Assert.IsNotNull(room, message + ": комната назначена");
        Assert.AreEqual(roomId, room.GetType().GetProperty("RoomId")?.GetValue(room), message);
    }

    private static T[] GetPrivateArray<T>(object target, string fieldName) where T : UnityEngine.Object
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return field != null ? field.GetValue(target) as T[] ?? Array.Empty<T>() : Array.Empty<T>();
    }

    private static T GetPrivate<T>(object target, string fieldName) where T : class
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return field != null ? field.GetValue(target) as T : null;
    }
}









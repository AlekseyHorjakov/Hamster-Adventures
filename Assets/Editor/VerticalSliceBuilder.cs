using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Собирает вертикальный срез первого уровня: стартовая зона, платформинг,
/// манекены, арена с гиенами, мини-босс, выход и экран завершения.
/// Меню: Tools/Hamster/Build Vertical Slice.
/// </summary>
public static class VerticalSliceBuilder
{
    private const string MainLevelPath = "Assets/Scenes/MainLevel.unity";
    private const string CombatTestPath = "Assets/Scenes/CombatTest.unity";
    private const string ScenePath = "Assets/Scenes/Level01_VerticalSlice.unity";
    private const string HyenaPrefabPath = "Assets/Prefabs/Hyena.prefab";
    private const string HamsterPrefabPath = "Assets/Prefabs/Hamster.prefab";
    private const string GirlPrefabPath = "Assets/Prefabs/HamsterGirl.prefab";
    private const string GroundSpritePath = "Assets/Resources/Level01/Ground.png";
    private const string PlatformSpritePath = "Assets/Resources/Level01/Platform.png";
    private const string BossControllerPath = "Assets/Animations/Enemies/HyenaBoss/HyenaBoss.controller";

    [MenuItem("Tools/Hamster/Build Vertical Slice")]
    public static void BuildAll()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            AssetDatabase.DeleteAsset(ScenePath);

        if (!AssetDatabase.CopyAsset(MainLevelPath, ScenePath))
        {
            Debug.LogError("[Slice] Не удалось скопировать MainLevel в " + ScenePath);
            return;
        }

        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        BuildLevelGeometry(scene);
        ConfigurePlayers(scene);
        ConfigureFallZone(scene);
        CloneTrainingDummies(scene);

        GameObject arenaRoot;
        HyenaAI[] hyenas;
        BuildArena(out arenaRoot, out hyenas);

        GameObject boss;
        GameObject exitGate;
        BuildBoss(out boss);
        BuildExit(out exitGate);

        LevelFlowController flow = BuildFlow(arenaRoot, hyenas, boss, exitGate);
        BuildTrigger("ArenaTrigger", new Vector3(132f, 2f, 0f), new Vector2(1.4f, 7f), flow, LevelTriggerAction.StartArena);
        BuildTrigger("FinishTrigger", new Vector3(204f, 2f, 0f), new Vector2(1.5f, 7f), flow, LevelTriggerAction.Finish);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Slice] Готово: Level01_VerticalSlice с ареной, мини-боссом и финишем.");
    }

    private static void BuildLevelGeometry(Scene scene)
    {
        // Удаляем старые тестовые прямоугольники из MainLevel: геометрия
        // уровня теперь создаётся вместе с визуалом.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Ground" || root.name.StartsWith("Ground ("))
                Object.DestroyImmediate(root);
        }

        CreateGroundSegment("CampGround", -52f, 48f, 0f, false);
        CreateGroundSegment("LakePlatform1", 53f, 61f, 2f, true);
        CreateGroundSegment("LakePlatform2", 67f, 74f, 3.5f, true);
        CreateGroundSegment("LakePlatform3", 80f, 87f, 2f, true);
        CreateGroundSegment("LakePlatform4", 93f, 101f, 4f, true);
        CreateGroundSegment("LakePlatform5", 107f, 114f, 2.5f, true);
        CreateGroundSegment("LakePlatform6", 118f, 126f, 1f, true);
        CreateGroundSegment("ArenaGround", 126f, 214f, 0f, false);
        CreateGroundSegment("ArenaRaisedLedge", 154f, 166f, 3.5f, true);
        CreateBoundary("LeftLevelBoundary", -51f);
        CreateBoundary("RightLevelBoundary", 213f);
    }

    private static void CreateBoundary(string name, float x)
    {
        GameObject boundary = new GameObject(name);
        boundary.transform.position = new Vector3(x, 10f, 0f);
        BoxCollider2D collider = boundary.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1f, 20f);
    }
    private static void CreateGroundSegment(string name, float left, float right,
        float topY, bool usePlatformArt)
    {
        float width = right - left;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(usePlatformArt
            ? PlatformSpritePath
            : GroundSpritePath);

        if (sprite == null)
        {
            Debug.LogError("[Slice] Не найден спрайт платформы для " + name);
            return;
        }

        GameObject root = new GameObject(name);
        root.transform.position = new Vector3((left + right) * 0.5f, topY - 0.5f, 0f);

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, 1f);
        collider.usedByEffector = true;

        PlatformEffector2D effector = root.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;
        effector.useSideFriction = false;
        effector.useSideBounce = false;
        effector.sideArc = 1f;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 10;

        if (usePlatformArt)
        {
            float scale = width / sprite.bounds.size.x;
            float visualHeight = sprite.bounds.size.y * scale;
            visual.transform.localScale = Vector3.one * scale;
            visual.transform.localPosition = new Vector3(0f, 0.5f - visualHeight * 0.5f + 1.1f, 0f);
        }
        else
        {
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(width, sprite.bounds.size.y);
            visual.transform.localPosition = new Vector3(0f, 0.5f - sprite.bounds.size.y * 0.5f + 1.1f, 0f);
        }
    }

    private static void ConfigurePlayers(Scene scene)
    {
        // В MainLevel лежат старые "запечённые" копии героев без новых
        // компонентов (SuperMeter, PartnerThrowController и т.д.).
        // Заменяем их актуальными префабами, иначе вертикальный срез
        // технически остаётся на старой версии персонажей.
        GameObject oldHamster = FindRoot(scene, "Hamster");
        GameObject oldGirl = FindRoot(scene, "HamsterGirl");
        if (oldHamster != null) Object.DestroyImmediate(oldHamster);
        if (oldGirl != null) Object.DestroyImmediate(oldGirl);

        GameObject hamsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HamsterPrefabPath);
        GameObject girlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GirlPrefabPath);
        GameObject hamster = (GameObject)PrefabUtility.InstantiatePrefab(hamsterPrefab);
        GameObject girl = (GameObject)PrefabUtility.InstantiatePrefab(girlPrefab);
        hamster.name = "Hamster";
        girl.name = "HamsterGirl";

        PlaceOnGround(hamster, -16.2f, 0f);
        PlaceOnGround(girl, -14.7f, 0f);

        HamsterMovement hamsterMovement = hamster.GetComponent<HamsterMovement>();
        HamsterMovement girlMovement = girl.GetComponent<HamsterMovement>();
        if (hamsterMovement != null) hamsterMovement.SetPartner(girl.transform);
        if (girlMovement != null) girlMovement.SetPartner(hamster.transform);

        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (camera != null)
        {
            camera.transform.position = new Vector3(-14f, 2.5f, -10f);
            CameraFollow follow = camera.GetComponent<CameraFollow>();
            if (follow != null)
            {
                SerializedObject followSo = new SerializedObject(follow);
                followSo.FindProperty("hamster").objectReferenceValue = hamster.transform;
                followSo.FindProperty("hamsterGirl").objectReferenceValue = girl.transform;
                followSo.FindProperty("useBounds").boolValue = true;
                followSo.FindProperty("boundsMin").vector2Value = new Vector2(-34f, 5.5f);
                followSo.FindProperty("boundsMax").vector2Value = new Vector2(196f, 12f);
                followSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void ConfigureFallZone(Scene scene)
    {
        GameObject fallZone = FindRoot(scene, "FallZone");
        if (fallZone == null)
            return;

        fallZone.transform.position = new Vector3(100f, -11f, 0f);
        BoxCollider2D collider = fallZone.GetComponent<BoxCollider2D>();
        if (collider != null)
            collider.size = new Vector2(300f, 2f);
    }

    private static void CloneTrainingDummies(Scene targetScene)
    {
        Scene combatScene = EditorSceneManager.OpenScene(CombatTestPath, OpenSceneMode.Additive);
        Dummy template = combatScene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Dummy>(true))
            .FirstOrDefault(dummy => dummy != null && dummy.gameObject.activeInHierarchy);

        if (template == null)
        {
            Debug.LogError("[Slice] Не найден обучающий манекен в CombatTest");
            EditorSceneManager.CloseScene(combatScene, true);
            return;
        }

        GameObject root = new GameObject("TrainingDummies");
        SceneManager.MoveGameObjectToScene(root, targetScene);

        float[] positions = { -10f, -5f };
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject dummy = Object.Instantiate(template.gameObject);
            SceneManager.MoveGameObjectToScene(dummy, targetScene);
            dummy.name = "TrainingDummy" + (i + 1);
            dummy.transform.SetParent(root.transform, true);
            PlaceOnGround(dummy, positions[i], 0f);
        }

        EditorSceneManager.CloseScene(combatScene, true);
    }

    private static void BuildArena(out GameObject arenaRoot, out HyenaAI[] hyenas)
    {
        arenaRoot = new GameObject("ArenaRoot");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HyenaPrefabPath);
        hyenas = new HyenaAI[2];

        float[] positions = { 148f, 162f };
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject hyena = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            hyena.name = "ArenaHyena" + (i + 1);
            hyena.transform.SetParent(arenaRoot.transform, true);
            PlaceOnGround(hyena, positions[i], 0f);
            ConfigureEnemyHitbox(hyena);
            hyenas[i] = hyena.GetComponent<HyenaAI>();
        }

        arenaRoot.SetActive(false);
    }

    private static void BuildBoss(out GameObject boss)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HyenaPrefabPath);
        boss = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        boss.name = "HyenaBoss";
        boss.transform.localScale = Vector3.one * 2.6f;
        PlaceOnGround(boss, 178f, 0f);
        ConfigureEnemyHitbox(boss);

        Health health = boss.GetComponent<Health>();
        if (health != null)
        {
            SerializedObject so = new SerializedObject(health);
            so.FindProperty("maxHealth").floatValue = 140f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Animator animator = boss.GetComponent<Animator>();
        RuntimeAnimatorController bossController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BossControllerPath);
        if (animator != null && bossController != null)
            animator.runtimeAnimatorController = bossController;

        SpriteRenderer renderer = boss.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
            renderer.color = Color.white;

        boss.SetActive(false);
    }

    private static void ConfigureEnemyHitbox(GameObject enemy)
    {
        Hitbox hitbox = enemy.GetComponentInChildren<Hitbox>(true);
        if (hitbox == null)
            return;

        SerializedObject so = new SerializedObject(hitbox);
        SerializedProperty center = so.FindProperty("centerOnRootCollider");
        if (center != null)
        {
            center.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    private static void BuildExit(out GameObject exitGate)
    {
        exitGate = new GameObject("ExitGate");
        exitGate.transform.position = new Vector3(194f, 3.5f, 0f);

        BoxCollider2D collider = exitGate.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1f, 7f);
    }

    private static LevelFlowController BuildFlow(GameObject arenaRoot, HyenaAI[] hyenas,
        GameObject boss, GameObject exitGate)
    {
        GameObject flowObject = new GameObject("VerticalSliceFlow");
        LevelFlowController flow = flowObject.AddComponent<LevelFlowController>();

        SerializedObject so = new SerializedObject(flow);
        so.FindProperty("levelTitle").stringValue = "Этап 1 — вертикальный срез";
        so.FindProperty("arenaRoot").objectReferenceValue = arenaRoot;
        so.FindProperty("boss").objectReferenceValue = boss;
        so.FindProperty("exitGate").objectReferenceValue = exitGate;

        SerializedProperty enemies = so.FindProperty("arenaEnemies");
        enemies.arraySize = hyenas.Length;
        for (int i = 0; i < hyenas.Length; i++)
            enemies.GetArrayElementAtIndex(i).objectReferenceValue = hyenas[i];

        so.ApplyModifiedPropertiesWithoutUndo();
        return flow;
    }

    private static void BuildTrigger(string name, Vector3 position, Vector2 size,
        LevelFlowController flow, LevelTriggerAction action)
    {
        GameObject triggerObject = new GameObject(name);
        triggerObject.transform.position = position;

        BoxCollider2D collider = triggerObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;

        LevelTrigger trigger = triggerObject.AddComponent<LevelTrigger>();
        SerializedObject so = new SerializedObject(trigger);
        so.FindProperty("level").objectReferenceValue = flow;
        so.FindProperty("action").enumValueIndex = (int)action;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PlaceOnGround(GameObject target, float x, float groundTopY)
    {
        if (target == null)
            return;

        Vector3 position = target.transform.position;
        float bottomOffset = 0f;

        BoxCollider2D box = target.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            // bounds у неактивных объектов в редакторе могут не обновиться,
            // поэтому считаем низ коробки из локального offset/size и масштаба.
            bottomOffset = (box.offset.y - box.size.y * 0.5f) * target.transform.lossyScale.y;
        }
        else
        {
            Collider2D collider = target.GetComponent<Collider2D>();
            if (collider != null)
                bottomOffset = collider.bounds.min.y - position.y;
        }

        target.transform.position = new Vector3(x, groundTopY - bottomOffset + 0.03f, position.z);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(scene => scene.path == scenePath))
            return;

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}

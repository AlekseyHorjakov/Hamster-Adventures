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

        ConfigureGrounds(scene);
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
        BuildTrigger("ArenaTrigger", new Vector3(6.5f, 2f, 0f), new Vector2(1.4f, 7f), flow, LevelTriggerAction.StartArena);
        BuildTrigger("FinishTrigger", new Vector3(47f, 2f, 0f), new Vector2(1.5f, 7f), flow, LevelTriggerAction.Finish);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Slice] Готово: Level01_VerticalSlice с ареной, мини-боссом и финишем.");
    }

    private static void ConfigureGrounds(Scene scene)
    {
        ConfigureGround(FindRoot(scene, "Ground"), new Vector3(-10f, -0.5f, 0f), new Vector2(16f, 1f));
        ConfigureGround(FindRoot(scene, "Ground (1)"), new Vector3(27.5f, -0.5f, 0f), new Vector2(45f, 1f));
        ConfigureGround(FindRoot(scene, "Ground (2)"), new Vector3(-1f, 1.5f, 0f), new Vector2(4f, 1f));
        ConfigureGround(FindRoot(scene, "Ground (3)"), new Vector3(3.25f, 3.5f, 0f), new Vector2(4.5f, 1f));
        ConfigureGround(FindRoot(scene, "Ground (4)"), new Vector3(14f, 3.5f, 0f), new Vector2(5f, 1f));
    }

    private static void ConfigureGround(GameObject ground, Vector3 position, Vector2 size)
    {
        if (ground == null)
            return;

        ground.SetActive(true);
        ground.transform.position = position;
        ground.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = new Color(0.28f, 0.52f, 0.24f, 1f);
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
                followSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void ConfigureFallZone(Scene scene)
    {
        GameObject fallZone = FindRoot(scene, "FallZone");
        if (fallZone == null)
            return;

        fallZone.transform.position = new Vector3(18f, -11f, 0f);
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

        float[] positions = { -11.5f, -8.2f };
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

        float[] positions = { 19f, 29f };
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject hyena = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            hyena.name = "ArenaHyena" + (i + 1);
            hyena.transform.SetParent(arenaRoot.transform, true);
            PlaceOnGround(hyena, positions[i], 0f);
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
        PlaceOnGround(boss, 39f, 0f);

        Health health = boss.GetComponent<Health>();
        if (health != null)
        {
            SerializedObject so = new SerializedObject(health);
            so.FindProperty("maxHealth").floatValue = 140f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        SpriteRenderer renderer = boss.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
            renderer.color = new Color(1f, 0.72f, 0.72f, 1f);

        boss.SetActive(false);
    }

    private static void BuildExit(out GameObject exitGate)
    {
        Sprite groundSprite = FindRoot(SceneManager.GetActiveScene(), "Ground")
            ?.GetComponent<SpriteRenderer>()?.sprite;

        exitGate = new GameObject("ExitGate");
        exitGate.transform.position = new Vector3(43.8f, 3.5f, 0f);
        exitGate.transform.localScale = new Vector3(1f, 7f, 1f);

        SpriteRenderer renderer = exitGate.AddComponent<SpriteRenderer>();
        renderer.sprite = groundSprite;
        renderer.color = new Color(0.85f, 0.20f, 0.16f, 0.88f);
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 5;

        BoxCollider2D collider = exitGate.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
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

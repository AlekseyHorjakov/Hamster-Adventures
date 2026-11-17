using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Собирает первую зону полного первого уровня: комнаты 01–05.
/// Сцена создаётся из вертикального среза, поэтому существующие герои,
/// камера, свет и рабочие боевые системы сохраняются без переписывания.
/// </summary>
public static class Level01Zone1Builder
{
    private const string SourceScenePath = "Assets/Scenes/Level01_VerticalSlice.unity";
    private const string TargetScenePath = "Assets/Scenes/Level01_Full.unity";
    private const string RoomFolder = "Assets/Level/Rooms/Zone1";
    private const string PrefabFolder = "Assets/Prefabs/Level/Zone1";
    private const string SliceFolder = "Assets/Art/Level01/ModularSlices";
    private const string ColliderBaselinePath = "LEVEL_1_COLLIDER_BASELINE.txt";

    private const string HyenaPrefabPath = "Assets/Prefabs/Hyena.prefab";
    private const string HamsterPrefabPath = "Assets/Prefabs/Hamster.prefab";
    private const string GirlPrefabPath = "Assets/Prefabs/HamsterGirl.prefab";
    private const string GroundSpritePath = "Assets/Resources/Level01/Ground.png";
    private const string DirtSpritePath = "Assets/Resources/Level01/DirtFill.png";
    private const string CampWorldSpritePath = "Assets/Resources/Level01/CampWorld.png";
    private const string CampSeamTreePath = "Assets/Resources/Level01/CampSeamTree.png";
    private const string CampSeamBushPath = "Assets/Resources/Level01/CampSeamBush.png";
    private const string SkyPath = "Assets/Resources/Level01/Sky.png";
    private const string MountainsPath = "Assets/Resources/Level01/Mountains.png";
    private const string ForestPath = "Assets/Resources/Level01/Forest.png";
    private const string ForegroundPath = "Assets/Resources/Level01/Foreground.png";

    private static LevelFlowController flow;
    private static RoomDefinition room01;
    private static RoomDefinition room02;
    private static RoomDefinition room03;
    private static RoomDefinition room04;
    private static RoomDefinition room05;
    private static GameObject rewardPrefab;
    private static Sprite checkpointLit;
    private static Sprite checkpointUnlit;
    private static Sprite[] flameFrames;
    private static Sprite arenaFence;

    [MenuItem("Tools/Hamster/Build Level 01 - Zone I (Rooms 01-05)")]
    public static void BuildZoneOne()
    {
        BuildZoneOneInternal(false);
    }

    [MenuItem("Tools/Hamster/Force Rebuild Level 01 - Zone I (overwrite manual edits)")]
    public static void ForceRebuildZoneOne()
    {
        BuildZoneOneInternal(true);
    }

    private static void BuildZoneOneInternal(bool force)
    {
        if (!force && System.IO.File.Exists(ColliderBaselinePath))
        {
            Debug.LogError("[Zone1] Сцена не пересобрана: найден эталон ручных правок " +
                ColliderBaselinePath + ". Используйте Force Rebuild только если действительно " +
                "хотите перезаписать ручную настройку коллайдеров.");
            return;
        }

        EnsureFolder("Assets/Level");
        EnsureFolder("Assets/Level/Rooms");
        EnsureFolder(RoomFolder);
        EnsureFolder("Assets/Prefabs/Level");
        EnsureFolder(PrefabFolder);
        EnsureFolder("Assets/Art/Level01");
        EnsureFolder(SliceFolder);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        CreateRoomDefinitions();
        CreateSharedPrefabs();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) != null)
            AssetDatabase.DeleteAsset(TargetScenePath);

        if (!AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath))
        {
            Debug.LogError("[Zone1] Не удалось скопировать вертикальный срез.");
            return;
        }

        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

        CleanupCopiedScene(scene);
        ConfigurePlayersAndCamera(scene);
        BuildFlowSystem(scene);
        BuildBackdrop(scene);
        BuildRooms(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        AddSceneToBuildSettings(TargetScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Zone1] Готово: Level01_Full.unity с комнатами 01–05.");
    }

    private static void CreateRoomDefinitions()
    {
        room01 = CreateRoomDefinition(1, "Лагерь", 1, "Лагерь и лес",
            "Осмотри лагерь, потренируйся на манекенах и иди вправо.",
            new Vector2(-29f, -2f), new Vector2(48f, 16f));
        room02 = CreateRoomDefinition(2, "Выход из лагеря", 1, "Лагерь и лес",
            "Следуй по лесной тропе к поляне.",
            new Vector2(48f, -2f), new Vector2(84f, 16f));
        room03 = CreateRoomDefinition(3, "Лесная поляна", 1, "Лагерь и лес",
            "Преодолей первую яму и поднимись на уступ.",
            new Vector2(84f, -2f), new Vector2(122f, 16f));
        room04 = CreateRoomDefinition(4, "Тренировочная площадка", 1, "Лагерь и лес",
            "Победи двух гиен на тренировочной площадке.",
            new Vector2(122f, -2f), new Vector2(160f, 16f));
        room05 = CreateRoomDefinition(5, "Старый мост", 1, "Лагерь и лес",
            "Переберись по разрушенному мосту к выходу из зоны.",
            new Vector2(160f, -3f), new Vector2(202f, 17f));
    }

    private static RoomDefinition CreateRoomDefinition(int id, string displayName, int zoneId,
        string zoneName, string objective, Vector2 cameraMin, Vector2 cameraMax)
    {
        string path = RoomFolder + "/Room_" + id.ToString("00") + ".asset";
        RoomDefinition room = AssetDatabase.LoadAssetAtPath<RoomDefinition>(path);
        if (room == null)
        {
            room = ScriptableObject.CreateInstance<RoomDefinition>();
            AssetDatabase.CreateAsset(room, path);
        }

        SerializedObject so = new SerializedObject(room);
        so.FindProperty("roomId").intValue = id;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("zoneId").intValue = zoneId;
        so.FindProperty("zoneName").stringValue = zoneName;
        so.FindProperty("objective").stringValue = objective;
        so.FindProperty("cameraBoundsMin").vector2Value = new Vector2(
            cameraMin.x + 17.8f, cameraMin.y);
        so.FindProperty("cameraBoundsMax").vector2Value = new Vector2(
            cameraMax.x - 17.8f, cameraMax.y);
        so.FindProperty("worldMinX").floatValue = cameraMin.x;
        so.FindProperty("worldMaxX").floatValue = cameraMax.x;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(room);
        return room;
    }

    private static void CreateSharedPrefabs()
    {
        checkpointUnlit = LoadSlice("Checkpoint_Campfire_Unlit");
        checkpointLit = LoadSlice("Checkpoint_Campfire_Lit");
        flameFrames = new[]
        {
            LoadSlice("Campfire_Flame_02"),
            LoadSlice("Campfire_Flame_04"),
            LoadSlice("Campfire_Flame_06"),
            LoadSlice("Campfire_Flame_05"),
            LoadSlice("Campfire_Flame_03"),
            LoadSlice("Campfire_Flame_01")
        };
        arenaFence = LoadSprite("Assets/Resources/Level01/ArenaFence.png");

        Sprite acorn = LoadSlice("Reward_Acorn_01");
        rewardPrefab = CreatePickupPrefab(acorn);

        Sprite crate = LoadSlice("Crate_Intact");
        Sprite crateBroken = LoadSlice("Crate_Broken");
        CreateBreakablePrefab("BreakableCrate", crate, crateBroken, new Vector2(0.95f, 1.02f));

        Sprite barrel = LoadSlice("Barrel_Intact");
        Sprite barrelBroken = LoadSlice("Barrel_Broken");
        CreateBreakablePrefab("BreakableBarrel", barrel, barrelBroken, new Vector2(0.88f, 1.02f));
    }

    private static GameObject CreatePickupPrefab(Sprite sprite)
    {
        string path = PrefabFolder + "/Reward_Acorn.prefab";
        GameObject temp = new GameObject("Reward_Acorn");
        SpriteRenderer renderer = temp.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Props";
        renderer.sortingOrder = 4;
        temp.transform.localScale = Vector3.one * 0.9f;

        CircleCollider2D trigger = temp.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.5f;
        temp.AddComponent<RewardPickup>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return prefab;
    }

    private static void CreateBreakablePrefab(string name, Sprite intact, Sprite broken, Vector2 colliderSize)
    {
        string path = PrefabFolder + "/" + name + ".prefab";
        GameObject temp = new GameObject(name);
        SpriteRenderer renderer = temp.AddComponent<SpriteRenderer>();
        renderer.sprite = intact;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 18;

        BoxCollider2D box = temp.AddComponent<BoxCollider2D>();
        box.size = colliderSize;

        Health health = temp.AddComponent<Health>();
        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("maxHealth").floatValue = 8f;
        healthSo.FindProperty("applyKnockback").boolValue = false;
        healthSo.ApplyModifiedPropertiesWithoutUndo();

        BreakableProp breakable = temp.AddComponent<BreakableProp>();
        SerializedObject breakableSo = new SerializedObject(breakable);
        breakableSo.FindProperty("visual").objectReferenceValue = renderer;
        breakableSo.FindProperty("brokenSprite").objectReferenceValue = broken;
        breakableSo.FindProperty("pickupPrefab").objectReferenceValue = rewardPrefab;
        breakableSo.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
    }

    private static void CleanupCopiedScene(Scene scene)
    {
        string[] remove = {
            "ArenaGround", "ArenaRaisedLedge", "ArenaRoot", "ArenaTrigger",
            "BakedLevelArt", "HyenaBoss", "ExitGate", "FallZone", "FinishTrigger",
            "LakePlatform1", "LakePlatform2", "LakePlatform3", "LakePlatform4",
            "LakePlatform5", "LakePlatform6", "LeftLevelBoundary", "RightLevelBoundary",
            "VerticalSliceFlow", "Rooms"
        };

        foreach (string rootName in remove)
        {
            GameObject root = FindRoot(scene, rootName);
            if (root != null)
                Object.DestroyImmediate(root);
        }
    }

    private static void ConfigurePlayersAndCamera(Scene scene)
    {
        GameObject hamster = FindRoot(scene, "Hamster");
        GameObject girl = FindRoot(scene, "HamsterGirl");
        if (hamster == null || girl == null)
        {
            if (hamster != null) Object.DestroyImmediate(hamster);
            if (girl != null) Object.DestroyImmediate(girl);
            hamster = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(HamsterPrefabPath));
            girl = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(GirlPrefabPath));
            hamster.name = "Hamster";
            girl.name = "HamsterGirl";
        }

        PlaceOnGround(hamster, -22.6f, 0f);
        PlaceOnGround(girl, -21.0f, 0f);

        HamsterMovement hamsterMovement = hamster.GetComponent<HamsterMovement>();
        HamsterMovement girlMovement = girl.GetComponent<HamsterMovement>();
        if (hamsterMovement != null) hamsterMovement.SetPartner(girl.transform);
        if (girlMovement != null) girlMovement.SetPartner(hamster.transform);

        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (camera == null)
            return;

        camera.transform.position = new Vector3(-10f, 5f, -10f);
        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow == null)
            follow = camera.gameObject.AddComponent<CameraFollow>();

        SerializedObject so = new SerializedObject(follow);
        so.FindProperty("hamster").objectReferenceValue = hamster.transform;
        so.FindProperty("hamsterGirl").objectReferenceValue = girl.transform;
        so.FindProperty("normalSize").floatValue = 10f;
        so.FindProperty("maxSize").floatValue = 15f;
        so.FindProperty("zoomPadding").floatValue = 1.5f;
        so.FindProperty("useBounds").boolValue = true;
        so.FindProperty("boundsMin").vector2Value = new Vector2(-11.2f, -2f);
        so.FindProperty("boundsMax").vector2Value = new Vector2(30.2f, 16f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildBackdrop(Scene scene)
    {
        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (camera == null)
            return;

        GameObject artRoot = new GameObject("BakedLevelArt");
        GameObject parallaxRoot = new GameObject("Parallax");
        parallaxRoot.transform.SetParent(artRoot.transform, false);

        CreateParallaxLayer(parallaxRoot.transform, camera, "Sky", LoadSprite(SkyPath),
            "Background", -100, new Vector3(camera.transform.position.x, camera.transform.position.y, 0f),
            24f, 1f, 1f, Color.white);
        CreateParallaxLayer(parallaxRoot.transform, camera, "Mountains", LoadSprite(MountainsPath),
            "Far", -30, new Vector3(camera.transform.position.x, camera.transform.position.y + 4f, 0f),
            20f, 0.98f, 0.82f, new Color(0.82f, 0.91f, 0.98f, 1f));
        CreateParallaxLayer(parallaxRoot.transform, camera, "Forest", LoadSprite(ForestPath),
            "Mid", -20, new Vector3(camera.transform.position.x, camera.transform.position.y + 1f, 0f),
            21f, 0.92f, 0.7f, new Color(0.78f, 0.93f, 0.78f, 1f));
        CreateParallaxLayer(parallaxRoot.transform, camera, "Foreground", LoadSprite(ForegroundPath),
            "Props", 100, new Vector3(camera.transform.position.x, camera.transform.position.y - 8f, 0f),
            22f, 1.03f, 0.88f, new Color(0.72f, 0.88f, 0.7f, 1f));

        CreateDecoration(artRoot.transform, "CampWorldVisual", LoadSprite(CampWorldSpritePath),
            -6f, 0f, 24f, "Mid", -5);

        Sprite tree = LoadSlice("ForestTree_Large");
        Sprite bush = LoadSlice("ForestBush");

        // Чёткие кусты стоят позади палатки и маскируют шов лагеря.
        CreateDecoration(artRoot.transform, "CampBushBack_Left", bush, -20f, 0f, 3.4f, "Mid", -6);
        CreateDecoration(artRoot.transform, "CampBushBack_Mid", bush, 3f, 0f, 3.2f, "Mid", -6);
        CreateDecoration(artRoot.transform, "CampBushBack_Right", bush, 18f, 0f, 3.6f, "Mid", -6);
        CreateDecoration(artRoot.transform, "CampSeamTree", tree, 23.5f, 0f, 12f, "Mid", -7);
        CreateDecoration(artRoot.transform, "CampSeamBush", bush, 21.2f, 0f, 4.2f, "Mid", -6);
        CreateDecoration(artRoot.transform, "TransitionTree_01", tree, 29f, 0f, 15f, "Mid", -4);
        CreateDecoration(artRoot.transform, "TransitionTree_02", tree, 63f, 0f, 14f, "Mid", -4);
        CreateDecoration(artRoot.transform, "TransitionTree_03", tree, 95f, 0f, 15f, "Mid", -4);
        CreateDecoration(artRoot.transform, "TransitionTree_04", tree, 126f, 2f, 14f, "Mid", -4);
        CreateDecoration(artRoot.transform, "TransitionTree_05", tree, 158f, 2f, 15f, "Mid", -4);
        CreateDecoration(artRoot.transform, "TransitionTree_06", tree, 194f, 2f, 16f, "Mid", -4);

        CreateDecoration(artRoot.transform, "Bush_01", bush, 35f, 0f, 3.5f, "Mid", -2);
        CreateDecoration(artRoot.transform, "Bush_02", bush, 55f, 0f, 3f, "Mid", -2);
        CreateDecoration(artRoot.transform, "Bush_03", bush, 87f, 0f, 3.2f, "Mid", -2);
        CreateDecoration(artRoot.transform, "Bush_04", bush, 119f, 1.5f, 3.2f, "Mid", -2);
        CreateDecoration(artRoot.transform, "Bush_05", bush, 165f, 2f, 3f, "Mid", -2);
        CreateDecoration(artRoot.transform, "Bush_06", bush, 199f, 2f, 3.4f, "Mid", -2);
    }

    private static void BuildRooms(Scene scene)
    {
        GameObject roomsRoot = new GameObject("Rooms");
        GameObject room01Root = new GameObject("Room_01_Camp");
        GameObject room02Root = new GameObject("Room_02_CampExit");
        GameObject room03Root = new GameObject("Room_03_ForestGlade");
        GameObject room04Root = new GameObject("Room_04_TrainingGround");
        GameObject room05Root = new GameObject("Room_05_OldBridge");
        room01Root.transform.SetParent(roomsRoot.transform, false);
        room02Root.transform.SetParent(roomsRoot.transform, false);
        room03Root.transform.SetParent(roomsRoot.transform, false);
        room04Root.transform.SetParent(roomsRoot.transform, false);
        room05Root.transform.SetParent(roomsRoot.transform, false);

        GameObject campGround = FindRoot(scene, "CampGround");
        if (campGround != null)
        {
            campGround.transform.SetParent(room01Root.transform, true);
            ConfigureExistingGroundSegment(campGround);
        }

        GameObject dummies = FindRoot(scene, "TrainingDummies");
        if (dummies != null)
            dummies.transform.SetParent(room01Root.transform, true);

        BuildCamp(room01Root.transform, scene, dummies);
        BuildCampExit(room02Root.transform);
        BuildForestGlade(room03Root.transform, scene);
        BuildTrainingGround(room04Root.transform, scene);
        BuildOldBridge(room05Root.transform);

        CreateBoundary(room01Root.transform, "LeftLevelBoundary", -29f, 0f);
        CreateBoundary(room05Root.transform, "ZoneOneEndBoundary", 202.2f, 2f);
    }

    private static void BuildCamp(Transform parent, Scene scene, GameObject dummies)
    {
        CreateRoomTrigger(parent, "Room_01_Trigger", room01, -27.5f, 0f, 12f);
        CreateCheckpoint(parent, -18f, 0f);

        CreateDecoration(parent, "CampSign", LoadSlice("Prop_Sign"), 14.5f, 0f, 3.2f, "Ground", 15);
        CreateDecoration(parent, "CampLantern", LoadSlice("Prop_Lantern"), 4f, 0f, 2.3f, "Ground", 15);
        CreateDecoration(parent, "CampGrass_A", LoadSlice("Prop_Grass"), 18f, 0f, 2.2f, "Ground", 14);
        CreateDecoration(parent, "CampGrass_B", LoadSlice("Prop_Grass"), -24.5f, 0f, 1.8f, "Ground", 14);
        CreateDecoration(parent, "CampGrass_C", LoadSlice("Prop_Grass"), -14f, 0f, 1.6f, "Ground", 14);
        CreateDecoration(parent, "CampFern", LoadSlice("Prop_Fern"), 33f, 0f, 2.8f, "Ground", 14);
        CreateDecoration(parent, "CampFern_B", LoadSlice("Prop_Fern"), -3.5f, 0f, 2.4f, "Ground", 14);
        CreateDecoration(parent, "CampRock", LoadSlice("Prop_Rock"), 41f, 0f, 1.8f, "Ground", 14);
        CreateDecoration(parent, "CampRock_B", LoadSlice("Prop_Rock"), -16.5f, 0f, 1.2f, "Ground", 14);
        CreateDecoration(parent, "CampLog", LoadSlice("Prop_Log"), -12f, 0f, 1.5f, "Ground", 14);
        CreateDecoration(parent, "CampFlowers", LoadSlice("Prop_Flowers"), -8f, 0f, 1.2f, "Ground", 14);
        CreateDecoration(parent, "CampMushroom", LoadSlice("Prop_Mushroom"), 27f, 0f, 1.3f, "Ground", 14);
        CreateDecoration(parent, "CampStump", LoadSlice("Prop_Stump"), 38f, 0f, 2.2f, "Ground", 14);

        if (dummies != null)
        {
            Transform dummy1 = dummies.transform.Find("TrainingDummy1");
            Transform dummy2 = dummies.transform.Find("TrainingDummy2");
            if (dummy1 != null) PlaceOnGround(dummy1.gameObject, -10f, 0f);
            if (dummy2 != null) PlaceOnGround(dummy2.gameObject, -5f, 0f);
        }
    }

    private static void BuildCampExit(Transform parent)
    {
        CreateGroundSegment(parent, "PathGround_02", 48f, 84f, 0f, false);
        CreateRoomTrigger(parent, "Room_02_Trigger", room02, 48.3f, 0f, 12f);
        CreateRoomTrigger(parent, "Room_01_BackTrigger", room01, 47.7f, 0f, 12f);

        CreateDecoration(parent, "TrailSign", LoadSlice("Prop_Sign"), 54f, 0f, 3.4f, "Ground", 15);
        CreateDecoration(parent, "TrailRock_A", LoadSlice("Prop_Rock"), 62f, 0f, 1.7f, "Ground", 14);
        CreateDecoration(parent, "TrailRock_B", LoadSlice("Prop_Rock"), 80f, 0f, 1.2f, "Ground", 14);
        CreateDecoration(parent, "TrailFern_A", LoadSlice("Prop_Fern"), 69f, 0f, 2.6f, "Ground", 14);
        CreateDecoration(parent, "TrailFern_B", LoadSlice("Prop_Fern"), 58f, 0f, 2.0f, "Ground", 14);
        CreateDecoration(parent, "TrailGrass_A", LoadSlice("Prop_Grass"), 78f, 0f, 2.2f, "Ground", 14);
        CreateDecoration(parent, "TrailGrass_B", LoadSlice("Prop_Grass"), 52f, 0f, 1.7f, "Ground", 14);
        CreateDecoration(parent, "TrailFlowers", LoadSlice("Prop_Flowers"), 64f, 0f, 1.4f, "Ground", 14);
        CreateDecoration(parent, "TrailMushroom", LoadSlice("Prop_Mushroom"), 73f, 0f, 1.2f, "Ground", 14);
        CreateDecoration(parent, "TrailLog", LoadSlice("Prop_Log"), 82f, 0f, 1.4f, "Ground", 14);
        CreateAward(parent, 74f, 0f);
    }

    private static void BuildForestGlade(Transform parent, Scene scene)
    {
        CreateGroundSegment(parent, "GladeGround_A", 84f, 102f, 0f, false);
        CreateGroundSegment(parent, "GladeGround_B", 108f, 122f, 1.5f, false);
        CreateOneWayPlatform(parent, "GladeStep_A", 98f, 103f, 0.75f, LoadSlice("Platform_End_A"));
        CreateOneWayPlatform(parent, "GladeStep_B", 105f, 109f, 1.25f, LoadSlice("Platform_Small_B"));
        CreateRoomTrigger(parent, "Room_03_Trigger", room03, 84.3f, 0f, 12f);
        CreateRoomTrigger(parent, "Room_02_BackTrigger", room02, 83.7f, 0f, 12f);
        CreateFallZone(parent, "GladePitFall", 105f, -1.4f, 7.5f, 2.4f);

        CreateDecoration(parent, "GladeMushroom_A", LoadSlice("Prop_Mushroom"), 89f, 0f, 1.7f, "Ground", 14);
        CreateDecoration(parent, "GladeFern_A", LoadSlice("Prop_Fern"), 95f, 0f, 2.4f, "Ground", 14);
        CreateDecoration(parent, "GladeStump", LoadSlice("Prop_Stump"), 112f, 1.5f, 2.4f, "Ground", 14);
        CreateDecoration(parent, "GladeFlowers", LoadSlice("Prop_Flowers"), 117f, 1.5f, 1.6f, "Ground", 14);
        CreateDecoration(parent, "GladeRock", LoadSlice("Prop_Rock"), 100f, 0f, 1.5f, "Ground", 14);

        GameObject dummyTemplate = FindSceneObject(scene, "TrainingDummy1");
        if (dummyTemplate != null)
        {
            GameObject dummy = Object.Instantiate(dummyTemplate, parent, true);
            dummy.name = "GladeTrainingDummy";
            PlaceOnGround(dummy, 115.5f, 1.5f);
        }

        CreateAward(parent, 106.5f, 1.25f);
    }

    private static void BuildTrainingGround(Transform parent, Scene scene)
    {
        CreateGroundSegment(parent, "TrainingGround_A", 121.6f, 142f, 2f, false);
        CreateGroundSegment(parent, "TrainingGround_B", 149f, 160f, 2f, false);
        CreateOneWayPlatform(parent, "TrainingStep_A", 138f, 143f, 3.2f, LoadSlice("Platform_Wide_B"));
        CreateOneWayPlatform(parent, "TrainingStep_B", 147f, 152f, 3.5f, LoadSlice("Platform_Wide_A"));
        CreateRoomTrigger(parent, "Room_04_Trigger", room04, 122.3f, 2f, 12f);
        CreateRoomTrigger(parent, "Room_03_BackTrigger", room03, 121.7f, 1.5f, 12f);
        CreateFallZone(parent, "TrainingPitFall", 145.5f, -2.2f, 9f, 2.6f);

        CreateDecoration(parent, "TrainingRock", LoadSlice("Prop_Rock"), 127.5f, 2f, 1.4f, "Ground", 14);
        CreateDecoration(parent, "TrainingMushroom", LoadSlice("Prop_Mushroom"), 157.5f, 2f, 1.5f, "Ground", 14);
        CreateDecoration(parent, "TrainingFern", LoadSlice("Prop_Fern"), 139f, 2f, 2.1f, "Ground", 14);

        GameObject dummyTemplate = FindSceneObject(scene, "TrainingDummy1");
        if (dummyTemplate != null)
        {
            for (int i = 0; i < 2; i++)
            {
                GameObject dummy = Object.Instantiate(dummyTemplate, parent, true);
                dummy.name = "TrainingGroundDummy_" + (i + 1);
                PlaceOnGround(dummy, 126f + i * 4f, 2f);
            }
        }

        CreateHyenaEncounter(parent);
        CreateBreakable(parent, "TrainingCrate", 124.2f, 2f, false);
        CreateBreakable(parent, "TrainingBarrel", 155f, 2f, true);
        CreateAward(parent, 150.5f, 3.5f);
    }

    private static void BuildOldBridge(Transform parent)
    {
        CreateGroundSegment(parent, "BridgeEntryGround", 160f, 168f, 2f, false);
        CreateGroundSegment(parent, "BridgeExitGround", 192f, 202f, 2f, false);
        CreateOneWayPlatform(parent, "BridgeSection_01", 168f, 173f, 2f, LoadSlice("Bridge_Section_01"), 0.58f);
        CreateOneWayPlatform(parent, "BridgeSection_02", 176f, 180f, 2.6f, LoadSlice("Bridge_Section_02"), 0.58f);
        CreateOneWayPlatform(parent, "BridgeSection_03", 183f, 188f, 3.2f, LoadSlice("Bridge_Section_03"), 0.58f);
        CreateOneWayPlatform(parent, "BridgeSection_04", 190f, 194f, 2.6f, LoadSlice("Bridge_Patch_01"), 0.03f);
        CreateRoomTrigger(parent, "Room_05_Trigger", room05, 160.3f, 2f, 13f);
        CreateRoomTrigger(parent, "Room_04_BackTrigger", room04, 159.7f, 2f, 13f);
        CreateFallZone(parent, "OldBridgeFall", 180f, -3.6f, 31f, 3.4f);
        CreateZoneEndTrigger(parent, "ZoneOneEndTrigger", 201.3f, 2f, 9f);

        CreateDecoration(parent, "BridgeSign", LoadSlice("Prop_Sign"), 198.8f, 2f, 3.2f, "Ground", 15);
        CreateDecoration(parent, "BridgeRock", LoadSlice("Prop_Rock"), 163.8f, 2f, 1.4f, "Ground", 14);
        CreateBreakable(parent, "BridgeBarrel", 165f, 2f, true);
        CreateAward(parent, 196.8f, 2f);
    }

    private static void BuildFlowSystem(Scene scene)
    {
        GameObject flowObject = new GameObject("LevelFlow");
        flow = flowObject.AddComponent<LevelFlowController>();
        SerializedObject so = new SerializedObject(flow);
        so.FindProperty("levelTitle").stringValue = "Hamster Adventures — Зона I";
        so.FindProperty("useRoomFlow").boolValue = true;
        so.FindProperty("startRoom").objectReferenceValue = room01;
        SerializedProperty rooms = so.FindProperty("roomDefinitions");
        rooms.arraySize = 5;
        rooms.GetArrayElementAtIndex(0).objectReferenceValue = room01;
        rooms.GetArrayElementAtIndex(1).objectReferenceValue = room02;
        rooms.GetArrayElementAtIndex(2).objectReferenceValue = room03;
        rooms.GetArrayElementAtIndex(3).objectReferenceValue = room04;
        rooms.GetArrayElementAtIndex(4).objectReferenceValue = room05;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateHyenaEncounter(Transform parent)
    {
        GameObject encounterRoot = new GameObject("Encounter_Room04");
        encounterRoot.transform.SetParent(parent, false);

        BoxCollider2D trigger = encounterRoot.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.2f, 4.5f);
        trigger.offset = new Vector2(0f, 2.25f);
        encounterRoot.transform.position = new Vector3(123.2f, 2f, 0f);

        GameObject hyenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HyenaPrefabPath);
        GameObject hyenaA = (GameObject)PrefabUtility.InstantiatePrefab(hyenaPrefab);
        GameObject hyenaB = (GameObject)PrefabUtility.InstantiatePrefab(hyenaPrefab);
        hyenaA.name = "TrainingHyena_A";
        hyenaB.name = "TrainingHyena_B";
        hyenaA.transform.SetParent(encounterRoot.transform, true);
        hyenaB.transform.SetParent(encounterRoot.transform, true);
        PlaceOnGround(hyenaA, 134f, 2f);
        PlaceOnGround(hyenaB, 153f, 2f);
        ConfigureEnemyHitbox(hyenaA);
        ConfigureEnemyHitbox(hyenaB);

        GameObject gate = CreateEncounterGate(parent, "TrainingGate", 159.2f, 2f);
        gate.SetActive(false);

        EncounterController controller = encounterRoot.AddComponent<EncounterController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("level").objectReferenceValue = flow;
        so.FindProperty("room").objectReferenceValue = room04;
        so.FindProperty("encounterName").stringValue = "Тренировочный бой";
        so.FindProperty("activateOnStart").boolValue = false;
        SerializedProperty enemies = so.FindProperty("enemies");
        enemies.arraySize = 2;
        enemies.GetArrayElementAtIndex(0).objectReferenceValue = hyenaA;
        enemies.GetArrayElementAtIndex(1).objectReferenceValue = hyenaB;
        SerializedProperty gates = so.FindProperty("gates");
        gates.arraySize = 1;
        gates.GetArrayElementAtIndex(0).objectReferenceValue = gate;
        so.ApplyModifiedPropertiesWithoutUndo();

        hyenaA.SetActive(false);
        hyenaB.SetActive(false);
    }

    private static GameObject CreateEncounterGate(Transform parent, string name, float x, float groundY)
    {
        GameObject gate = new GameObject(name);
        gate.transform.SetParent(parent, false);
        gate.transform.position = new Vector3(x, groundY + 2.3f, 0f);

        BoxCollider2D box = gate.AddComponent<BoxCollider2D>();
        box.size = new Vector2(2.1f, 4.6f);

        Sprite post = LoadSlice("Bridge_Post_01");
        Sprite rope = LoadSlice("Bridge_Rope_01");
        CreateLocalDecoration(gate.transform, "LeftPost", post, -1.15f, -0.5f, 3.6f, "Ground", 30);
        CreateLocalDecoration(gate.transform, "RightPost", post, 1.15f, -0.5f, 3.6f, "Ground", 30);
        CreateLocalDecoration(gate.transform, "Rope", rope, 0f, 1.2f, 1.25f, "Ground", 31);

        return gate;
    }

    private static void CreateCheckpoint(Transform parent, float x, float groundY)
    {
        GameObject root = new GameObject("Checkpoint_Camp");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(x, groundY, 0f);

        BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(3.2f, 3.2f);
        trigger.offset = new Vector2(0f, 1.6f);

        GameObject baseObject = new GameObject("Base");
        baseObject.transform.SetParent(root.transform, false);
        baseObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        baseObject.transform.localScale = Vector3.one * 2.2f;
        SpriteRenderer baseRenderer = baseObject.AddComponent<SpriteRenderer>();
        baseRenderer.sprite = checkpointUnlit;
        baseRenderer.sortingLayerName = "Ground";
        baseRenderer.sortingOrder = 20;

        GameObject flame = new GameObject("Flame");
        flame.transform.SetParent(root.transform, false);
        flame.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        flame.transform.localScale = Vector3.one * 1.9f;
        SpriteRenderer flameRenderer = flame.AddComponent<SpriteRenderer>();
        flameRenderer.sprite = flameFrames[0];
        flameRenderer.enabled = false;
        flameRenderer.sortingLayerName = "Ground";
        flameRenderer.sortingOrder = 21;

        SpriteFrameAnimator animator = flame.AddComponent<SpriteFrameAnimator>();
        SerializedObject animatorSo = new SerializedObject(animator);
        animatorSo.FindProperty("target").objectReferenceValue = flameRenderer;
        SerializedProperty frames = animatorSo.FindProperty("frames");
        frames.arraySize = flameFrames.Length;
        for (int i = 0; i < flameFrames.Length; i++)
            frames.GetArrayElementAtIndex(i).objectReferenceValue = flameFrames[i];
        animatorSo.FindProperty("framesPerSecond").floatValue = 10f;
        animatorSo.ApplyModifiedPropertiesWithoutUndo();

        RoomCheckpoint checkpoint = root.AddComponent<RoomCheckpoint>();
        SerializedObject so = new SerializedObject(checkpoint);
        so.FindProperty("level").objectReferenceValue = flow;
        so.FindProperty("room").objectReferenceValue = room01;
        so.FindProperty("checkpointIndex").intValue = 0;
        so.FindProperty("checkpointLabel").stringValue = "Лагерь";
        so.FindProperty("flameRenderer").objectReferenceValue = flameRenderer;
        so.FindProperty("frameRate").floatValue = 10f;
        SerializedProperty frameArray = so.FindProperty("flameFrames");
        frameArray.arraySize = flameFrames.Length;
        for (int i = 0; i < flameFrames.Length; i++)
            frameArray.GetArrayElementAtIndex(i).objectReferenceValue = flameFrames[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateBreakable(Transform parent, string name, float x, float groundY, bool barrel)
    {
        string prefabName = barrel ? "BreakableBarrel" : "BreakableCrate";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + prefabName + ".prefab");
        if (prefab == null)
            return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent, true);
        PlaceOnGround(instance, x, groundY);
    }

    private static void CreateAward(Transform parent, float x, float groundY)
    {
        if (rewardPrefab == null)
            return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(rewardPrefab);
        instance.name = "Reward_Acorn";
        instance.transform.SetParent(parent, true);
        instance.transform.position = new Vector3(x, groundY + 1.1f, 0f);
    }

    private static void CreateRoomTrigger(Transform parent, string name, RoomDefinition room,
        float x, float groundY, float height)
    {
        GameObject triggerObject = new GameObject(name);
        triggerObject.transform.SetParent(parent, false);
        triggerObject.transform.position = new Vector3(x, groundY + height * 0.5f, 0f);

        BoxCollider2D box = triggerObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1.1f, height);

        RoomTrigger trigger = triggerObject.AddComponent<RoomTrigger>();
        SerializedObject so = new SerializedObject(trigger);
        so.FindProperty("level").objectReferenceValue = flow;
        so.FindProperty("room").objectReferenceValue = room;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateFallZone(Transform parent, string name, float x, float y,
        float width, float height)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent, false);
        zone.transform.position = new Vector3(x, y, 0f);

        BoxCollider2D box = zone.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(width, height);

        FallRespawnZone fall = zone.AddComponent<FallRespawnZone>();
        SerializedObject so = new SerializedObject(fall);
        so.FindProperty("level").objectReferenceValue = flow;
        so.FindProperty("respawnDelay").floatValue = 0.65f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateZoneEndTrigger(Transform parent, string name, float x,
        float groundY, float height)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent, false);
        zone.transform.position = new Vector3(x, groundY + height * 0.5f, 0f);

        BoxCollider2D box = zone.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1.2f, height);

        ZoneEndTrigger end = zone.AddComponent<ZoneEndTrigger>();
        SerializedObject so = new SerializedObject(end);
        so.FindProperty("level").objectReferenceValue = flow;
        so.FindProperty("completedRooms").intValue = 5;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateBoundary(Transform parent, string name, float x, float groundY)
    {
        GameObject boundary = new GameObject(name);
        boundary.transform.SetParent(parent, false);
        boundary.transform.position = new Vector3(x, groundY + 10f, 0f);
        BoxCollider2D box = boundary.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 24f);
    }

    private static void CreateGroundSegment(Transform parent, string name, float left, float right,
        float topY, bool usePlatformArt)
    {
        float width = right - left;
        float centerX = (left + right) * 0.5f;
        Sprite groundSprite = LoadSprite(GroundSpritePath);

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(centerX, topY - 0.5f, 0f);

        // Основная земля должна быть твёрдой со всех сторон. Односторонние
        // платформы используются только для прыжковых элементов и моста.
        BoxCollider2D box = root.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, 1f);
        box.usedByEffector = false;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = groundSprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(width, groundSprite.bounds.size.y);
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 10;
        visual.transform.localPosition = new Vector3(
            0f, 0.5f - groundSprite.bounds.size.y * 0.5f + 1.1f, 0f);

        AddGroundFill(root, width);
    }

    private static void ConfigureExistingGroundSegment(GameObject root)
    {
        if (root == null)
            return;

        BoxCollider2D box = root.GetComponent<BoxCollider2D>();
        if (box != null)
            box.usedByEffector = false;

        foreach (PlatformEffector2D effector in root.GetComponents<PlatformEffector2D>())
            Object.DestroyImmediate(effector);

        Transform oldFill = root.transform.Find("UndergroundFill");
        if (oldFill != null)
            Object.DestroyImmediate(oldFill.gameObject);

        Transform newFill = root.transform.Find("GroundFill");
        if (newFill != null)
            Object.DestroyImmediate(newFill.gameObject);

        float width = box != null ? box.size.x * Mathf.Abs(root.transform.lossyScale.x) : 20f;
        AddGroundFill(root, width);
    }

    private static void AddGroundFill(GameObject root, float width)
    {
        Sprite fillSprite = LoadSlice("GroundFill_Tile");
        if (fillSprite == null)
            return;

        GameObject fill = new GameObject("GroundFill");
        fill.transform.SetParent(root.transform, false);
        fill.transform.localPosition = new Vector3(0f, -9.6f, 0f);

        SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = fillSprite;
        fillRenderer.drawMode = SpriteDrawMode.Tiled;
        fillRenderer.size = new Vector2(width + 4f, 22f);
        fillRenderer.sortingLayerName = "Ground";
        fillRenderer.sortingOrder = 8;
        fillRenderer.color = new Color(0.92f, 0.86f, 0.82f, 1f);
    }

    private static void CreateOneWayPlatform(Transform parent, string name, float left, float right,
        float topY, Sprite sprite, float visualTopOffset = 0.12f)
    {
        if (sprite == null)
            return;

        float width = right - left;
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3((left + right) * 0.5f, topY - 0.5f, 0f);

        BoxCollider2D box = root.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, 0.55f);
        box.offset = new Vector2(0f, 0.225f);
        box.usedByEffector = true;
        AddPlatformEffector(root);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 12;

        float scale = width / sprite.bounds.size.x;
        visual.transform.localScale = Vector3.one * scale;
        float visualHeight = sprite.bounds.size.y * scale;
        visual.transform.localPosition = new Vector3(0f, 0.55f + visualTopOffset - visualHeight * 0.5f, 0f);
    }

    private static void AddPlatformEffector(GameObject root)
    {
        PlatformEffector2D effector = root.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;
        effector.useSideFriction = false;
        effector.useSideBounce = false;
        effector.sideArc = 1f;
    }

    private static void CreateDecoration(Transform parent, string name, Sprite sprite,
        float x, float groundY, float height, string sortingLayer, int order, bool relativeToParent = false)
    {
        if (sprite == null)
            return;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        if (relativeToParent)
        {
            go.transform.localPosition = new Vector3(x, groundY + height * 0.5f, 0f);
        }
        else
        {
            go.transform.position = new Vector3(x, groundY + height * 0.5f, 0f);
        }

        float scale = height / sprite.bounds.size.y;
        go.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = order;
    }

    private static void CreateLocalDecoration(Transform parent, string name, Sprite sprite,
        float x, float yCenter, float height, string sortingLayer, int order)
    {
        if (sprite == null)
            return;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, yCenter, 0f);
        go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = order;
    }

    private static GameObject CreateParallaxLayer(Transform parent, Camera camera, string name,
        Sprite sprite, string sortingLayer, int order, Vector3 position, float targetHeight,
        float horizontalFactor, float verticalFactor, Color tint)
    {
        if (sprite == null)
            return null;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * (targetHeight / sprite.bounds.size.y);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = order;

        ParallaxLayer parallax = go.AddComponent<ParallaxLayer>();
        parallax.Initialize(camera.transform, horizontalFactor, verticalFactor);
        return go;
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

    private static void PlaceOnGround(GameObject target, float x, float groundTopY)
    {
        if (target == null)
            return;

        Vector3 position = target.transform.position;
        float bottomOffset = 0f;

        BoxCollider2D box = target.GetComponent<BoxCollider2D>();
        if (box != null)
        {
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

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite LoadSlice(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SliceFolder + "/" + name + ".png");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
    }

    private static GameObject FindSceneObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == name)
                    return candidate.gameObject;
            }
        }

        return null;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(scene => scene.path == scenePath))
            return;

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}


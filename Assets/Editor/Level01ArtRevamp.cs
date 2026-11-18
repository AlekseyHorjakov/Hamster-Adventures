using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Художественная переработка зоны I без изменения игровых коллайдеров.
/// Меняет только визуал земли и декорации, сохраняя ручную настройку сцены.
/// </summary>
public static class Level01ArtRevamp
{
    private const string ScenePath = "Assets/Scenes/Level01_Full.unity";
    private const string SliceFolder = "Assets/Art/Level01/ModularSlices";
    private const string GroundSlice = "Ground_SingleLayer";

    [MenuItem("Tools/Хомяк/Зона I: пересобрать арт (коллайдеры не трогать)")]
    public static void Apply()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureGroundImport();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject artRoot = GameObject.Find("BakedLevelArt");
        if (artRoot == null)
        {
            Debug.LogError("[ArtRevamp] Не найден BakedLevelArt. Сцена не изменена.");
            return;
        }

        SetRoomSurfaces();
        PurgeGenerated(artRoot.transform);
        BuildParallax(artRoot.transform);
        ReplaceGroundVisuals(artRoot.transform);
        RemoveOldCampArt();
        RemoveRoomDecorations();
        BuildCampClearing(artRoot.transform);
        RemoveCampClutter();
        FixRewardReadability();
        BuildTreesAndBushes(artRoot.transform);
        BuildRoomDecorations();
        BuildPitModules();
        FixPlatformColliders();
        AlignPlatformVisuals();
        BuildStepCliffs(artRoot.transform);
        BuildStepMask(artRoot.transform);
        AddContactShadows(artRoot.transform);
        FixEncounterGate();
        AddCheckpointMarker(artRoot.transform);
        AddFinishGate(artRoot.transform);
        TuneForeground();
        TuneParallaxLayers();
        TuneTreeVariety(artRoot.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ArtRevamp] Визуал зоны I обновлён. Существующие игровые коллайдеры не изменялись; ямы оформлены как глубокие провалы.");
    }

    private static void SetRoomSurfaces()
    {
        // Высота земли каждой комнаты: нужна камере, чтобы не уезжать под уровень.
        var surfaces = new (int id, float y)[] { (1, 0f), (2, 0f), (3, 0.75f), (4, 2f), (5, 2f) };
        foreach (var entry in surfaces)
        {
            string path = "Assets/Level/Rooms/Zone1/Room_" + entry.id.ToString("00") + ".asset";
            RoomDefinition room = AssetDatabase.LoadAssetAtPath<RoomDefinition>(path);
            if (room == null)
                continue;
            SerializedObject so = new SerializedObject(room);
            so.FindProperty("surfaceY").floatValue = entry.y;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(room);
        }
        AssetDatabase.SaveAssets();
    }

    private static bool TryGroundY(float x, out float y)
    {
        y = 0f;
        float best = float.MinValue;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(new Vector2(x, 40f), Vector2.down, 80f))
        {
            if (hit.collider == null)
                continue;
            string n = hit.collider.gameObject.name;
            if (n.IndexOf("Ground", System.StringComparison.Ordinal) < 0)
                continue;
            if (hit.point.y > best)
                best = hit.point.y;
        }
        if (best == float.MinValue)
            return false;
        y = best;
        return true;
    }
    private static void PurgeGenerated(Transform artRoot)
    {
        // Удаляем весь ранее сгенерированный арт, кроме параллакс-слоёв.
        for (int i = artRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = artRoot.GetChild(i);
            Object.DestroyImmediate(child.gameObject);
        }

        // Чистим накопленные декорации внутри комнат и старые ямы/тени.
        foreach (Transform t in Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray())
        {
            if (t == null)
                continue;
            string n = t.name;
            if (n.StartsWith("Prop_") || n == "ContactShadow" ||
                n.Contains("PitVisual") || n == "GladePitFall" || n == "TrainingPitFall" ||
                n == "GladeFall" || n == "TrainingFall" || n == "Step122_Rock" ||
                n == "Step122_GroundCover" || n.StartsWith("Step10") || n.StartsWith("Step12"))
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }


    }
    private static void BuildParallax(Transform artRoot)
    {
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
            return;

        GameObject root = new GameObject("Parallax");
        root.transform.SetParent(artRoot, false);

        CreateParallax(root.transform, cam, "Sky", LoadRes("Sky"), "Background", -100,
            76f, new Vector3(0f, 0f, 0f), 1f, 1f, Color.white);
        CreateParallax(root.transform, cam, "Mountains", LoadRes("Mountains"), "Far", -30,
            76f, new Vector3(0f, 4f, 0f), 0.98f, 0.85f, new Color(0.86f, 0.93f, 0.99f, 1f));
        CreateParallax(root.transform, cam, "Forest", LoadRes("Forest"), "Mid", -20,
            76f, new Vector3(0f, 1f, 0f), 0.92f, 0.72f, new Color(0.80f, 0.92f, 0.82f, 1f));
        CreateParallax(root.transform, cam, "Foreground", LoadRes("Foreground"), "Props", 100,
            76f, new Vector3(0f, -9f, 0f), 1.03f, 0.88f, new Color(0.74f, 0.86f, 0.72f, 0.82f));
    }

    private static void CreateParallax(Transform parent, Camera cam, string name, Sprite sprite,
        string layer, int order, float targetWidth, Vector3 offset, float hFactor, float vFactor, Color color)
    {
        if (sprite == null)
            return;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(cam.transform.position.x + offset.x, cam.transform.position.y + offset.y, 0f);
        float scale = targetWidth / sprite.bounds.size.x;
        go.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = layer;
        renderer.sortingOrder = order;

        ParallaxLayer parallax = go.AddComponent<ParallaxLayer>();
        parallax.Initialize(cam.transform, hFactor, vFactor);
    }

    private static Sprite LoadRes(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Level01/" + name + ".png");
    }
    private static void ConfigureGroundImport()
    {
        string path = SliceFolder + "/" + GroundSlice + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.spriteBorder = new Vector4(110f, 70f, 110f, 185f);
        importer.SaveAndReimport();
    }

    private static void ReplaceGroundVisuals(Transform artRoot)
    {
        Sprite tile = LoadSlice("Ground_Tile");
        Sprite capLeft = LoadSlice("Ground_Cap_Left");
        Sprite capRight = LoadSlice("Ground_Cap_Right");
        Sprite fill = LoadSlice("Ground_Soil_Tile");
        if (tile == null || capLeft == null || capRight == null || fill == null)
        {
            Debug.LogError("[ArtRevamp] Не найдены тайлы земли.");
            return;
        }

        // Старые визуалы на самих коллайдерах больше не нужны.
        string[] groundNames = {
            "CampGround", "PathGround_02", "GladeGround_A", "GladeGround_B",
            "TrainingGround_A", "TrainingGround_B", "BridgeEntryGround", "BridgeExitGround"
        };
        foreach (string name in groundNames)
        {
            GameObject groundObject = GameObject.Find(name);
            if (groundObject == null)
                continue;
            foreach (string childName in new[] { "Visual", "GroundFill", "UndergroundFill", "GroundVisual_SingleLayer" })
            {
                Transform child = groundObject.transform.Find(childName);
                if (child != null)
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        Transform oldRuns = artRoot.Find("GroundRuns");
        if (oldRuns != null)
            Object.DestroyImmediate(oldRuns.gameObject);
        Transform oldSeams = artRoot.Find("GroundSeamPatches");
        if (oldSeams != null)
            Object.DestroyImmediate(oldSeams.gameObject);

        GameObject root = new GameObject("GroundRuns");
        root.transform.SetParent(artRoot, false);

        // Один длинный визуал на каждый непрерывный участок. Границы совпадают с коллайдерами.
        AddGroundRun(root.transform, "LowRun", -52f, 102f, 0f, tile, capLeft, capRight, fill);
        AddGroundRun(root.transform, "GladeHighRun", 108f, 122f, 1.5f, tile, capLeft, capRight, fill);
        AddGroundRun(root.transform, "TrainingRun", 121.6f, 142f, 2f, tile, capLeft, capRight, fill);
        AddGroundRun(root.transform, "BridgeEntryRun", 149f, 168f, 2f, tile, capLeft, capRight, fill);
        AddGroundRun(root.transform, "BridgeExitRun", 192f, 202f, 2f, tile, capLeft, capRight, fill);
    }

    private static void AddGroundRun(Transform parent, string name, float left, float right,
        float groundTop, Sprite tile, Sprite capLeft, Sprite capRight, Sprite fill)
    {
        const float surfaceInset = 1.25f; // строка, где начинается плоская трава, а не кусты сверху
        float height = tile.bounds.size.y;
        float centerY = groundTop - height * 0.5f + surfaceInset;
        float width = right - left;

        GameObject run = new GameObject(name);
        run.transform.SetParent(parent, false);

        GameObject tileObject = new GameObject(name + "_Tile");
        tileObject.transform.SetParent(run.transform, false);
        tileObject.transform.position = new Vector3((left + right) * 0.5f, centerY, 0f);
        SpriteRenderer tileRenderer = tileObject.AddComponent<SpriteRenderer>();
        tileRenderer.sprite = tile;
        tileRenderer.drawMode = SpriteDrawMode.Tiled;
        tileRenderer.size = new Vector2(width + 0.2f, height);
        tileRenderer.sortingLayerName = "Ground";
        tileRenderer.sortingOrder = 10;

        AddGroundCap(run.transform, name + "_LeftCap", capLeft,
            new Vector3(left + capLeft.bounds.size.x * 0.5f, centerY, 0f));
        AddGroundCap(run.transform, name + "_RightCap", capRight,
            new Vector3(right - capRight.bounds.size.x * 0.5f, centerY, 0f));

        // Земляной массив вниз: камера никогда не должна показывать небо под уровнем.
        GameObject fillObject = new GameObject(name + "_UndergroundFill");
        fillObject.transform.SetParent(run.transform, false);
        fillObject.transform.position = new Vector3((left + right) * 0.5f, groundTop - 5.45f - 10f, 0f);
        SpriteRenderer fillRenderer = fillObject.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = fill;
        fillRenderer.drawMode = SpriteDrawMode.Tiled;
        fillRenderer.size = new Vector2(width + 0.6f, 20f);
        fillRenderer.sortingLayerName = "Ground";
        fillRenderer.sortingOrder = 12;
        fillRenderer.color = Color.white;
    }

    private static void AddGroundCap(Transform parent, string name, Sprite sprite, Vector3 position)
    {
        GameObject cap = new GameObject(name);
        cap.transform.SetParent(parent, false);
        cap.transform.position = position;
        SpriteRenderer renderer = cap.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 11;
    }
    private static void RemoveOldCampArt()
    {
        string[] names = {
            "CampWorldVisual", "CampSeamTree", "CampSeamBush",
            "CampBushBack_Left", "CampBushBack_Mid", "CampBushBack_Right",
            "TransitionTree_01", "TransitionTree_02", "TransitionTree_03",
            "TransitionTree_04", "TransitionTree_05", "TransitionTree_06",
            "Bush_01", "Bush_02", "Bush_03", "Bush_04", "Bush_05", "Bush_06"
        };
        foreach (string name in names)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
                Object.DestroyImmediate(go);
        }
    }

    private static void RemoveRoomDecorations()
    {
        string[] oldDecor = {
            "CampSign", "CampLantern", "CampGrass_A", "CampGrass_B", "CampGrass_C",
            "CampFern", "CampFern_B", "CampRock", "CampRock_B", "CampLog",
            "CampFlowers", "CampMushroom", "CampStump",
            "TrailSign", "TrailRock_A", "TrailRock_B", "TrailFern_A", "TrailFern_B",
            "TrailGrass_A", "TrailGrass_B", "TrailFlowers", "TrailMushroom", "TrailLog",
            "GladeMushroom_A", "GladeFern_A", "GladeStump", "GladeFlowers", "GladeRock",
            "TrainingRock", "TrainingMushroom", "TrainingFern",
            "BridgeSign", "BridgeRock"
        };
        foreach (string name in oldDecor)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
                Object.DestroyImmediate(go);
        }
    }

    private static void BuildCampClearing(Transform artRoot)
    {
        Transform old = artRoot.Find("CampClearing");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        GameObject root = new GameObject("CampClearing");
        root.transform.SetParent(artRoot, false);

        // Лагерь — небольшая поляна: несколько деревьев по краям, без плотной стены.
        VisualTree(root.transform, "CampTree_Oak", "Tree_Oak", -30f, 15f, -8);
        VisualTree(root.transform, "CampTree_Birch", "Tree_Birch", -15f, 13f, -8);
        VisualTree(root.transform, "CampTree_Flowering", "Tree_FloweringGreen", 0f, 14f, -8);
        VisualTree(root.transform, "CampTree_Autumn", "Tree_Autumn", 16f, 13f, -8);
        VisualTree(root.transform, "CampTree_Fir", "Tree_Fir", 33f, 15f, -8);

        Sprite bush = LoadSlice("ForestBush");
        AddBush(root.transform, bush, -25f, 0f, 2.8f, -6);
        AddBush(root.transform, bush, -18f, 0f, 2.4f, -6);
        AddBush(root.transform, bush, -5f, 0f, 2.6f, -6);
        AddBush(root.transform, bush, 9f, 0f, 2.5f, -6);
        AddBush(root.transform, bush, 25f, 0f, 2.9f, -6);
        AddBush(root.transform, bush, 40f, 0f, 2.5f, -6);

        // Две палатки и минимальный лагерный реквизит.
        Visual(root.transform, "CampTent_Green", "CampTent_Green", -26.2f, 0f, 5.2f, "Mid", -3);
        Visual(root.transform, "CampTent_Beige", "CampTent_Beige", -11.6f, 0f, 4.8f, "Mid", -3);

        Visual(root.transform, "CampTable_Long", "CampTable_Long", -15.7f, 0f, 2.1f, "Ground", 12);
        Visual(root.transform, "CampBench_A", "CampBench", -18.4f, 0f, 1.3f, "Ground", 13);
        Visual(root.transform, "CampLantern_Red", "CampLantern_Red", -23.5f, 0f, 2.4f, "Ground", 14);
        Visual(root.transform, "CampChest_A", "CampChest_Wood", -7.5f, 0f, 1.2f, "Ground", 14);
        // Мелкая лесная растительность.
        AddPlant(root.transform, "Prop_Grass", -28.6f, 0f, 2.0f, 14);
        AddPlant(root.transform, "Prop_Flowers", -20.8f, 0f, 1.35f, 14);
        AddPlant(root.transform, "Prop_Fern", -17.8f, 0f, 2.5f, 14);
        AddPlant(root.transform, "Prop_Rock", -8.9f, 0f, 1.25f, 14);
        AddPlant(root.transform, "Prop_Grass", 0.8f, 0f, 1.8f, 14);
        AddPlant(root.transform, "Prop_Flowers", 6.2f, 0f, 1.25f, 14);
        AddPlant(root.transform, "Prop_Mushroom", 13.4f, 0f, 1.35f, 14);
        AddPlant(root.transform, "Prop_Fern", 18.6f, 0f, 2.2f, 14);
        AddPlant(root.transform, "Prop_Grass", 25.5f, 0f, 1.8f, 14);
        AddPlant(root.transform, "Prop_Flowers", 34.0f, 0f, 1.2f, 14);
        AddPlant(root.transform, "Prop_Rock", 42.5f, 0f, 1.4f, 14);
    }

    private static void BuildTreesAndBushes(Transform artRoot)
    {
        Transform old = artRoot.Find("Zone1Trees");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        GameObject root = new GameObject("Zone1Trees");
        root.transform.SetParent(artRoot, false);
        Sprite bush = LoadSlice("ForestBush");

        VisualTree(root.transform, "TrailTree_Birch", "Tree_Birch", 57f, 13f, -7);
        VisualTree(root.transform, "TrailTree_Autumn", "Tree_Autumn", 73f, 12f, -7);
        VisualTree(root.transform, "TrailTree_Fir", "Tree_Fir", 82f, 14f, -8);
        VisualTree(root.transform, "GladeTree_Oak", "Tree_Oak", 90f, 14f, -7);
        VisualTree(root.transform, "GladeTree_Sakura", "Tree_Sakura", 106f, 12f, -7);
        VisualTree(root.transform, "GladeTree_Bonsai", "Tree_Bonsai", 118f, 9f, -6);
        VisualTree(root.transform, "TrainingTree_Old", "Tree_OldHollow", 126f, 14f, -7);
        VisualTree(root.transform, "TrainingTree_Fir", "Tree_Fir", 155f, 13f, -8);
        VisualTree(root.transform, "BridgeTree_Willow", "Tree_Willow", 163f, 13f, -7);
        VisualTree(root.transform, "BridgeTree_Dead", "Tree_Dead", 186f, 14f, -7);
        VisualTree(root.transform, "BridgeTree_Apple", "Tree_Apple", 200f, 13f, -7);

        AddBush(root.transform, bush, 51f, 0f, 3.2f, -5);
        AddBush(root.transform, bush, 66f, 0f, 2.8f, -5);
        AddBush(root.transform, bush, 80f, 0f, 3.3f, -5);
        AddBush(root.transform, bush, 100f, 0f, 3.0f, -5);
        AddBush(root.transform, bush, 119f, 1.5f, 3.2f, -5);
        AddBush(root.transform, bush, 132f, 2f, 3.0f, -5);
        AddBush(root.transform, bush, 151f, 2f, 3.2f, -5);
        AddBush(root.transform, bush, 171f, 2f, 3.4f, -5);
        AddBush(root.transform, bush, 194f, 2f, 3.1f, -5);
    }

    private static void BuildRoomDecorations()
    {
        GameObject campRoot = GameObject.Find("Room_01_Camp");
        if (campRoot != null)
        {
            AddPlant(campRoot.transform, "Prop_Log", -13.6f, 0f, 1.4f, 14);
            AddPlant(campRoot.transform, "Prop_Stump", 27.5f, 0f, 2.1f, 14);
        }

        GameObject pathRoot = GameObject.Find("Room_02_CampExit");
        if (pathRoot != null)
        {
            AddPlant(pathRoot.transform, "Prop_Sign", 54f, 0f, 3.0f, 15);
            AddPlant(pathRoot.transform, "Prop_Log", 63f, 0f, 1.4f, 14);
            AddPlant(pathRoot.transform, "Prop_Fern", 70f, 0f, 2.5f, 14);
            AddPlant(pathRoot.transform, "Prop_Rock", 79f, 0f, 1.3f, 14);
            AddPlant(pathRoot.transform, "Prop_Flowers", 59f, 0f, 1.2f, 14);
        }

        GameObject gladeRoot = GameObject.Find("Room_03_ForestGlade");
        if (gladeRoot != null)
        {
            AddPlant(gladeRoot.transform, "Prop_Mushroom", 89f, 0f, 1.6f, 14);
            AddPlant(gladeRoot.transform, "Prop_Fern", 96f, 0f, 2.3f, 14);
            AddPlant(gladeRoot.transform, "Prop_Stump", 113f, 1.5f, 2.3f, 14);
            AddPlant(gladeRoot.transform, "Prop_Flowers", 118f, 1.5f, 1.4f, 14);
            AddPlant(gladeRoot.transform, "Prop_Rock", 101f, 0f, 1.4f, 14);
        }

        GameObject trainingRoot = GameObject.Find("Room_04_TrainingGround");
        if (trainingRoot != null)
        {
            AddPlant(trainingRoot.transform, "Prop_Rock", 127f, 2f, 1.2f, 14);
            AddPlant(trainingRoot.transform, "Prop_Mushroom", 157f, 2f, 1.2f, 14);
            AddPlant(trainingRoot.transform, "Prop_Fern", 139f, 2f, 2.0f, 14);
        }

        GameObject bridgeRoot = GameObject.Find("Room_05_OldBridge");
        if (bridgeRoot != null)
        {
            AddPlant(bridgeRoot.transform, "Prop_Sign", 198f, 2f, 3.0f, 15);
            AddPlant(bridgeRoot.transform, "Prop_Rock", 164f, 2f, 1.2f, 14);
        }
    }

    private static void BuildPitModules()
    {
        GameObject gladeRoot = GameObject.Find("Room_03_ForestGlade");
        if (gladeRoot != null)
        {
            RemoveIfExists("GladePitFall");
            RemoveIfExists("GladePitVisual");
            CreatePitModule(gladeRoot.transform, "GladePitVisual", 105f, 0f, 6.2f, 1.6f);
        }

        GameObject trainingRoot = GameObject.Find("Room_04_TrainingGround");
        if (trainingRoot != null)
        {
            RemoveIfExists("TrainingPitFall");
            RemoveIfExists("TrainingPitVisual");
            CreatePitModule(trainingRoot.transform, "TrainingPitVisual", 145.5f, 2f, 7.2f, 1.75f);
        }
    }

    private static void CreatePitModule(Transform parent, string name, float centerX,
        float groundY, float width, float floorDepth)
    {
        RemoveIfExists(name + "_Backdrop");
        RemoveIfExists(name + "_RockL");
        RemoveIfExists(name + "_RockR");
        RemoveIfExists(name + "_PitDepth");
        RemoveIfExists(name + "_PitCore");
        RemoveIfExists(name + "_FallZone");
        RemoveIfExists(name + "_EdgeRockL");
        RemoveIfExists(name + "_EdgeRockR");
        RemoveIfExists(name + "_WallL");
        RemoveIfExists(name + "_WallR");
        RemoveIfExists(name + "_Underfill");
        RemoveIfExists(name + "_Floor");
        RemoveIfExists(name);
        RemoveIfExists("GladePitFall");
        RemoveIfExists("TrainingPitFall");

        // Яма = разрыв земли: тёмные стенки + травяное дно. Без «столбов» поверх прохода.
        float floorTop = groundY + 0.02f - floorDepth;
        float pitHeight = floorDepth + 0.5f;

        Sprite soil = LoadSlice("Ground_Soil_Tile");
        if (soil != null)
        {
            GameObject walls = new GameObject(name + "_PitWalls");
            walls.transform.SetParent(parent, false);
            walls.transform.position = new Vector3(centerX, groundY + 0.02f - pitHeight * 0.5f, 0f);
            SpriteRenderer renderer = walls.AddComponent<SpriteRenderer>();
            renderer.sprite = soil;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(width + 0.5f, pitHeight);
            renderer.sortingLayerName = "Ground";
            renderer.sortingOrder = 8;
            renderer.color = new Color(0.24f, 0.13f, 0.09f, 1f);
        }

        Sprite floorSprite = LoadSlice("PitFloor");
        if (floorSprite != null)
        {
            GameObject floorVisual = new GameObject(name);
            floorVisual.transform.SetParent(parent, false);
            floorVisual.transform.position = new Vector3(centerX, floorTop - 0.18f, 0f);
            SpriteRenderer renderer = floorVisual.AddComponent<SpriteRenderer>();
            renderer.sprite = floorSprite;
            renderer.sortingLayerName = "Ground";
            renderer.sortingOrder = 10;
            float sx = (width + 0.2f) / floorSprite.bounds.size.x;
            floorVisual.transform.localScale = new Vector3(sx, 1f, 1f);
        }
        // Дно ямы: по нему можно ходить и выпрыгнуть двойным прыжком.
        GameObject floor = new GameObject(name + "_Floor");
        floor.transform.SetParent(parent, false);
        floor.transform.position = new Vector3(centerX, floorTop - 0.3f, 0f);
        BoxCollider2D floorCollider = floor.AddComponent<BoxCollider2D>();
        // Дно шире разрыва: у краёв ямы не остаётся щели, куда можно провалиться.
        floorCollider.size = new Vector2(width + 0.8f, 0.6f);

        GameObject fall = new GameObject(name + "_FallZone");
        fall.transform.SetParent(parent, false);
        fall.transform.position = new Vector3(centerX, floorTop - 1.9f, 0f);
        BoxCollider2D trigger = fall.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(width + 0.8f, 3.2f);
        fall.AddComponent<FallRespawnZone>();

        // Земля под дном ямы, чтобы снизу не просвечивал лес.
        if (soil != null)
        {
            GameObject under = new GameObject(name + "_Underfill");
            under.transform.SetParent(parent, false);
            under.transform.position = new Vector3(centerX, floorTop - 0.45f - 6f, 0f);
            SpriteRenderer underRenderer = under.AddComponent<SpriteRenderer>();
            underRenderer.sprite = soil;
            underRenderer.drawMode = SpriteDrawMode.Tiled;
            underRenderer.size = new Vector2(width + 0.6f, 12f);
            underRenderer.sortingLayerName = "Ground";
            underRenderer.sortingOrder = 12;
            underRenderer.color = Color.white;
        }

        // Стенки ямы: сквозь них нельзя пройти, но можно выпрыгнуть сверху.
        AddPitWall(parent, name + "_WallL", centerX - width * 0.5f + 0.15f, floorTop, groundY);
        AddPitWall(parent, name + "_WallR", centerX + width * 0.5f - 0.15f, floorTop, groundY);
        // Ступенька внутри ямы, чтобы из неё можно было выпрыгнуть без идеального двойного прыжка.
        AddPitStep(parent, name + "_Step", centerX + width * 0.2f, 1.7f, floorTop + 1.45f);

        Visual(parent, name + "_EdgeRockL", "Prop_Rock",
            centerX - width * 0.5f + 0.5f, groundY, 0.8f, "Ground", 14);
        Visual(parent, name + "_EdgeRockR", "Prop_Rock",
            centerX + width * 0.5f - 0.5f, groundY, 0.8f, "Ground", 14);
    }
    private static void AddPitWall(Transform parent, string name, float x, float floorTop, float groundY)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        float height = Mathf.Max(0.4f, groundY - floorTop + 0.3f);
        wall.transform.position = new Vector3(x, floorTop + height * 0.5f - 0.1f, 0f);
        BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.3f, height);
    }
    private static void AddPitStep(Transform parent, string name, float centerX, float width, float topY)
    {
        Sprite sprite = LoadSlice("Platform_Small_B");
        if (sprite == null)
            return;

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(centerX, topY - 0.5f, 0f);

        BoxCollider2D box = root.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, 0.55f);
        box.offset = new Vector2(0f, 0.225f);
        box.usedByEffector = true;
        PlatformEffector2D effector = root.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 12;
        float scale = width / sprite.bounds.size.x;
        visual.transform.localScale = Vector3.one * scale;
        float visualHeight = sprite.bounds.size.y * scale;
        visual.transform.localPosition = new Vector3(0f, 0.55f + 0.195f * scale - visualHeight * 0.5f, 0f);
    }
    private static void AddCliffFace(Transform parent, string name, string spriteName,
        float x, float topY, float height, int flip)
    {
        Sprite sprite = LoadSlice(spriteName);
        if (sprite == null)
            return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, topY - height * 0.5f, 0f);
        float scale = height / sprite.bounds.size.y;
        go.transform.localScale = new Vector3(scale * flip, scale, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 9;
    }
    private static void RemoveIfExists(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
            Object.DestroyImmediate(go);
    }

    private static void RemoveCampClutter()
    {
        // Мелкий реквизит, который визуально дробил лагерь и мешал читать путь.
        RemoveIfExists("CampChair_Folding");
        RemoveIfExists("CampBackpack_A");
        RemoveIfExists("CampBedroll_A");
        RemoveIfExists("CampBarrel_Decor");
    }

    private static void FixRewardReadability()
    {
        foreach (RewardPickup reward in Object.FindObjectsByType<RewardPickup>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SpriteRenderer renderer = reward.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer != null)
            {
                renderer.sortingLayerName = "Ground";
                renderer.sortingOrder = 16;
            }
        }
    }
    private static readonly (string root, string sprite)[] PlatformVisuals =
    {
        ("GladeStep_A", "Platform_End_A"),
        ("GladeStep_B", "Platform_Small_B"),
        ("TrainingStep_A", "Platform_Wide_B"),
        ("TrainingStep_B", "Platform_Wide_A"),
        ("BridgeSection_01", "Bridge_Section_01"),
        ("BridgeSection_02", "Bridge_Section_02"),
        ("BridgeSection_03", "Bridge_Section_03"),
        ("BridgeSection_04", "Bridge_Patch_01")
    };

    private static void EnsurePlatformVisual(GameObject root, string rootName)
    {
        Transform existing = root.transform.Find("Visual");
        if (existing != null && existing.GetComponent<SpriteRenderer>() != null)
            return;

        string spriteName = null;
        foreach (var entry in PlatformVisuals)
            if (entry.root == rootName)
                spriteName = entry.sprite;

        Sprite sprite = LoadSlice(spriteName);
        if (sprite == null)
            return;

        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 12;
        float scale = (root.GetComponent<BoxCollider2D>().size.x) / sprite.bounds.size.x;
        visual.transform.localScale = Vector3.one * scale;
        float h = sprite.bounds.size.y * scale;
        visual.transform.localPosition = new Vector3(0f, 0.55f - h * 0.5f, 0f);
    }
    private static float PlatformSurfaceFraction(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return 0.10f;
        if (spriteName.Contains("Bridge_Section")) return 0.38f;
        if (spriteName.Contains("Bridge_Patch")) return 0.35f;
        if (spriteName.Contains("Platform_End")) return 0.055f;
        if (spriteName.Contains("Platform_Small")) return 0.195f;
        if (spriteName.Contains("Platform_Wide_A")) return 0.209f;
        if (spriteName.Contains("Platform_Wide_B")) return 0.172f;
        return 0.10f;
    }

    private static void FixPlatformColliders()
    {
        // Коллайдеры этих площадок были смещены относительно сцены (визуал «парил»).
        // Возвращаем утверждённую геометрию LEVEL_1_GEOMETRY.
        var tops = new (string name, float left, float right, float top)[]
        {
            ("GladeStep_A", 98f, 103f, 0.75f),
            ("GladeStep_B", 105f, 109f, 1.25f),
            ("TrainingStep_A", 138f, 143f, 3.2f),
            ("TrainingStep_B", 147f, 152f, 3.5f),
            ("BridgeSection_01", 168f, 173f, 2.0f),
            ("BridgeSection_02", 176f, 180f, 2.6f),
            ("BridgeSection_03", 183f, 188f, 3.2f),
            ("BridgeSection_04", 190f, 194f, 2.6f)
        };

        foreach (var entry in tops)
        {
            GameObject go = GameObject.Find(entry.name);
            if (go == null)
                continue;

            BoxCollider2D box = go.GetComponent<BoxCollider2D>();
            if (box == null)
                continue;

            go.transform.position = new Vector3((entry.left + entry.right) * 0.5f, entry.top - 0.5f, 0f);
            go.transform.localScale = Vector3.one;
            box.offset = new Vector2(0f, 0.225f);
            box.size = new Vector2(entry.right - entry.left, 0.55f);
            box.usedByEffector = true;
            EnsurePlatformVisual(go, entry.name);
        }
    }
    private static void AlignPlatformVisuals()
    {
        foreach (PlatformEffector2D effector in Object.FindObjectsByType<PlatformEffector2D>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform visual = effector.transform.Find("Visual");
            if (visual == null)
                continue;
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
                continue;

            BoxCollider2D box = effector.GetComponent<BoxCollider2D>();
            if (box == null)
                continue;

            // Верх визуала должен стоять так, чтобы линия настила/травы
            // совпадала с верхом коллайдера.
            float surfaceFraction = PlatformSurfaceFraction(renderer.sprite.name);
            float visualHeight = renderer.bounds.size.y;
            float desiredTop = box.bounds.max.y + surfaceFraction * visualHeight - 0.02f;
            float delta = desiredTop - renderer.bounds.max.y;
            visual.position += new Vector3(0f, delta, 0f);
        }
    }

    private static void BuildSoilStrata(Transform artRoot)
    {
        Transform old = artRoot.Find("SoilStrata");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        Sprite band = LoadSlice("Soil_Strata");
        if (band == null)
            return;

        GameObject root = new GameObject("SoilStrata");
        root.transform.SetParent(artRoot, false);

        AddStrata(root.transform, "Strata_Low", -51.5f, 101.5f, -6.1f, band);
        AddStrata(root.transform, "Strata_Glade", 108.2f, 121.8f, -4.6f, band);
        AddStrata(root.transform, "Strata_Training", 121.8f, 142f, -4.1f, band);
        AddStrata(root.transform, "Strata_BridgeEntry", 149.2f, 168f, -4.1f, band);
        AddStrata(root.transform, "Strata_BridgeExit", 192.2f, 202f, -4.1f, band);
    }

    private static void AddStrata(Transform parent, string name, float left, float right,
        float topY, Sprite band)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        float height = 1.35f;
        go.transform.position = new Vector3((left + right) * 0.5f, topY - height * 0.5f, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = band;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(right - left, height);
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 9;
        renderer.color = new Color(0.88f, 0.72f, 0.6f, 1f);
    }
    private static void BuildStepCliffs(Transform artRoot)
    {
        Transform old = artRoot.Find("StepCliffs");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);
        GameObject root = new GameObject("StepCliffs");
        root.transform.SetParent(artRoot, false);

        AddCliffFace(root.transform, "Step108_Cliff", "Cliff_Chunk_A", 108.15f, 1.5f, 2.6f, 1);
        AddAbyss(artRoot);
        AddCliffFace(root.transform, "Step122_Cliff", "Cliff_Chunk_B", 121.95f, 2.0f, 2.4f, -1);
    }
    private static void AddAbyss(Transform artRoot)
    {
        Transform old = artRoot.Find("BridgeAbyss");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        Sprite soil = LoadSlice("Ground_Soil_Tile");
        if (soil == null)
            return;

        GameObject back = new GameObject("BridgeAbyss");
        back.transform.SetParent(artRoot, false);
        back.transform.position = new Vector3(181f, -14f, 0f);
        SpriteRenderer renderer = back.AddComponent<SpriteRenderer>();
        renderer.sprite = soil;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(44f, 24f);
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -5;
        renderer.color = new Color(0.30f, 0.22f, 0.18f, 1f);
    }
    private static void BuildStepMask(Transform artRoot)
    {
        Transform old = artRoot.Find("StepMask");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        GameObject root = new GameObject("StepMask");
        root.transform.SetParent(artRoot, false);

        Sprite patch = LoadSlice("Ground_Seam_Patch");
        if (patch != null)
        {
            GameObject cover = new GameObject("Step122_GroundCover");
            cover.transform.SetParent(root.transform, false);
            float height = 7.05f;
            cover.transform.position = new Vector3(122f, 2f - height * 0.5f + 0.70f, 0f);
            cover.transform.localScale = new Vector3(
                3.4f / patch.bounds.size.x,
                height / patch.bounds.size.y,
                1f);
            SpriteRenderer renderer = cover.AddComponent<SpriteRenderer>();
            renderer.sprite = patch;
            renderer.sortingLayerName = "Ground";
            renderer.sortingOrder = 12;
        }

        Visual(root.transform, "Step122_Rock", "Prop_Rock", 122f, 1.65f, 1.1f, "Ground", 14);
    }
    private static void BuildGroundSeamPatches(Transform artRoot)
    {
        Transform old = artRoot.Find("GroundSeamPatches");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        Sprite patch = LoadSlice("Ground_Seam_Patch");
        if (patch == null)
            return;

        GameObject root = new GameObject("GroundSeamPatches");
        root.transform.SetParent(artRoot, false);

        AddGroundSeamPatch(root.transform, "Seam_48", 48f, 0f, patch);
        AddGroundSeamPatch(root.transform, "Seam_84", 84f, 0f, patch);
        AddGroundSeamPatch(root.transform, "Seam_160", 160f, 2f, patch);
        AddGroundSeamPatch(root.transform, "Seam_122_Top", 122f, 2f, patch);

        // Маскируем вертикальную грань ступени 1.5 -> 2.0.
        Visual(root.transform, "Step_122_Rock", "Prop_Rock", 122f, 1.55f, 1.15f, "Ground", 14);

        Sprite bush = LoadSlice("ForestBush");
        if (bush != null)
        {
            AddBush(root.transform, bush, 48f, 0f, 1.45f, -6);
            AddBush(root.transform, bush, 84f, 0f, 1.55f, -6);
            AddBush(root.transform, bush, 160f, 2f, 1.45f, -6);
        }
    }

    private static void AddGroundSeamPatch(Transform parent, string name, float x,
        float groundY, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        float width = 3.4f;
        float height = 7.05f;
        go.transform.position = new Vector3(x, groundY + 0.04f - height * 0.5f, 0f);
        go.transform.localScale = new Vector3(
            width / sprite.bounds.size.x,
            height / sprite.bounds.size.y,
            1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 11;
    }

    private static void AddContactShadows(Transform artRoot)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray())
        {
            if (t != null && t.name == "ContactShadow")
                Object.DestroyImmediate(t.gameObject);
        }

        Transform old = artRoot.Find("ContactShadows");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        Sprite shadow = LoadSlice("Shadow_Soft");
        if (shadow == null)
            return;

        GameObject root = new GameObject("ContactShadows");
        root.transform.SetParent(artRoot, false);

        foreach (string rootName in new[] { "CampClearing", "Zone1Trees" })
        {
            Transform art = artRoot.Find(rootName);
            if (art == null)
                continue;

            foreach (SpriteRenderer renderer in art.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite == null || renderer.name == "ContactShadow")
                    continue;
                AddWorldShadow(root.transform, renderer, shadow);
            }
        }

        foreach (HamsterMovement player in Object.FindObjectsByType<HamsterMovement>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddAttachedShadow(player.gameObject, shadow);

        foreach (HyenaAI hyena in Object.FindObjectsByType<HyenaAI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddAttachedShadow(hyena.gameObject, shadow);

        foreach (Dummy dummy in Object.FindObjectsByType<Dummy>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddAttachedShadow(dummy.gameObject, shadow);

        foreach (BreakableProp breakable in Object.FindObjectsByType<BreakableProp>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddAttachedShadow(breakable.gameObject, shadow);

        foreach (RewardPickup reward in Object.FindObjectsByType<RewardPickup>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddAttachedShadow(reward.gameObject, shadow);
    }

    private static void AddWorldShadow(Transform parent, SpriteRenderer target, Sprite shadow)
    {
        Bounds bounds = target.bounds;
        float width = Mathf.Clamp(bounds.size.x * 0.5f, 0.6f, 4.2f);
        float height = Mathf.Clamp(width * 0.28f, 0.22f, 1.0f);

        GameObject go = new GameObject("ContactShadow");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(bounds.center.x, bounds.min.y + 0.03f, 0f);
        go.transform.localScale = new Vector3(
            width / shadow.bounds.size.x,
            height / shadow.bounds.size.y,
            1f);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = shadow;
        renderer.color = new Color(1f, 1f, 1f, 0.6f);
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 11;
    }

    private static void AddAttachedShadow(GameObject target, Sprite shadow)
    {
        SpriteRenderer source = target.GetComponentInChildren<SpriteRenderer>();
        if (source == null || source.sprite == null)
            return;

        Bounds bounds = source.bounds;
        bool enemy = target.GetComponent<HyenaAI>() != null;
        float width = Mathf.Clamp(bounds.size.x * (enemy ? 0.7f : 0.58f), 0.55f, enemy ? 3.0f : 2.4f);
        float height = Mathf.Clamp(width * 0.28f, 0.2f, 0.75f);
        Vector3 scale = target.transform.lossyScale;

        GameObject go = new GameObject("ContactShadow");
        go.transform.SetParent(target.transform, false);
        go.transform.position = new Vector3(bounds.center.x, bounds.min.y + 0.03f, 0f);
        go.transform.localScale = new Vector3(
            width / shadow.bounds.size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            height / shadow.bounds.size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            1f);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = shadow;
        renderer.color = enemy
            ? new Color(0.98f, 0.34f, 0.14f, 0.72f)
            : new Color(1f, 1f, 1f, 0.6f);
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 11;
    }

    private static void FixEncounterGate()
    {
        GameObject gate = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.name == "TrainingGate")?.gameObject;
        if (gate == null)
            return;

        for (int i = gate.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(gate.transform.GetChild(i).gameObject);

        Sprite fence = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Resources/Level01/ArenaFence.png");
        if (fence == null)
            return;

        GameObject visual = new GameObject("GateVisual");
        visual.transform.SetParent(gate.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.8f, 0f);
        float height = 4.6f;
        visual.transform.localScale = Vector3.one * (height / fence.bounds.size.y);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = fence;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 30;
    }

    private static void AddCheckpointMarker(Transform artRoot)
    {
        Transform existing = artRoot.Find("CheckpointMarker_Camp");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        Sprite shadow = LoadSlice("Shadow_Soft");
        if (shadow == null)
            return;

        GameObject go = new GameObject("CheckpointMarker_Camp");
        go.transform.SetParent(artRoot, false);
        go.transform.position = new Vector3(-18f, 0.08f, 0f);
        go.transform.localScale = new Vector3(
            3.2f / shadow.bounds.size.x,
            0.55f / shadow.bounds.size.y,
            1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = shadow;
        renderer.color = new Color(1f, 0.72f, 0.22f, 0.3f);
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 11;
    }
    private static void AddFinishGate(Transform artRoot)
    {
        Transform old = artRoot.Find("Zone1FinishGate");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        Sprite finish = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Resources/Level01/FinishGate.png");
        if (finish != null)
        {
            Visual(artRoot, "Zone1FinishGate", finish, 202.4f, 2f, 8.2f, "Ground", 25);
            GameObject gateObject = GameObject.Find("Zone1FinishGate");
            if (gateObject != null)
                gateObject.transform.position += Vector3.down * 0.7f;
        }
    }

    private static void TuneParallaxLayers()
    {
        // Ширина слоёв задаётся при сборке параллакса.
    }
    private static void TuneForeground()
    {
        GameObject foreground = GameObject.Find("Foreground");
        if (foreground == null)
            return;

        SpriteRenderer renderer = foreground.GetComponent<SpriteRenderer>();
        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (renderer == null || renderer.sprite == null || camera == null)
            return;

        // Передний план не должен закрывать землю, ямы и персонажей —
        // держим его ниже игровой зоны.
        float height = 10f;
        foreground.transform.localScale = Vector3.one * (height / renderer.sprite.bounds.size.y);
        foreground.transform.position = new Vector3(
            camera.transform.position.x,
            camera.transform.position.y - 15f,
            0f);
        renderer.color = new Color(0.78f, 0.9f, 0.78f, 0.75f);
    }

    private static void TuneTreeVariety(Transform artRoot)
    {
        int index = 0;
        foreach (string rootName in new[] { "CampClearing", "Zone1Trees" })
        {
            Transform root = artRoot.Find(rootName);
            if (root == null)
                continue;

            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite == null || !renderer.sprite.name.StartsWith("Tree_"))
                    continue;

                renderer.flipX = (index % 3 == 1);
                renderer.color = index % 4 == 0
                    ? new Color(0.86f, 0.93f, 0.88f, 1f)
                    : new Color(0.93f, 0.98f, 0.94f, 1f);
                index++;
            }
        }
    }
    private static void VisualTree(Transform parent, string name, string spriteName, float x, float height, int order)
    {
        Sprite sprite = LoadSlice(spriteName);
        if (sprite == null || !TryGroundY(x, out float y))
            return;
        Visual(parent, name, sprite, x, y, height, "Mid", order);
    }
    private static void AddBush(Transform parent, Sprite bush, float x, float groundY, float height, int order)
    {
        if (bush == null || !TryGroundY(x, out float y))
            return;

        // Куст не должен свисать над ямой/обрывом: проверяем землю под его краями.
        float halfWidth = bush.bounds.size.x * (height / bush.bounds.size.y) * 0.45f;
        if (!TryGroundY(x - halfWidth, out _) || !TryGroundY(x + halfWidth, out _))
            return;

        Visual(parent, "Bush_" + x.ToString("0.#"), bush, x, y, height, "Mid", order);
    }

    private static void AddPlant(Transform parent, string spriteName, float x, float groundY, float height, int order)
    {
        Sprite sprite = LoadSlice(spriteName);
        if (sprite == null || !TryGroundY(x, out float y))
            return;
        Visual(parent, spriteName + "_" + x.ToString("0.#"), sprite, x, y, height, "Ground", order);
    }

    private static void Visual(Transform parent, string name, string spriteName, float x,
        float groundY, float height, string sortingLayer, int sortingOrder)
    {
        Sprite sprite = LoadSlice(spriteName);
        if (sprite == null)
        {
            Debug.LogWarning("[ArtRevamp] Нет спрайта " + spriteName);
            return;
        }

        Visual(parent, name, sprite, x, groundY, height, sortingLayer, sortingOrder);
    }

    private static void Visual(Transform parent, string name, Sprite sprite, float x,
        float groundY, float height, string sortingLayer, int sortingOrder)
    {
        if (sprite == null)
            return;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, groundY + height * 0.5f, 0f);
        go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;
    }

    private static Sprite LoadSlice(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SliceFolder + "/" + name + ".png");
    }
}









































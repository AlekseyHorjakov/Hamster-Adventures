using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Сборка арта Зоны II, этап 1: комнаты 06 «Берег озера» и 07 «Мелководье».
/// Идемпотентно: удаляет и пересобирает только объекты зоны, коллайдеры комнат 01-05 не трогает.
/// </summary>
public static class Level01Zone2Art
{
    private const string ScenePath = "Assets/Scenes/Level01_Full.unity";
    private const string SlicesPath = "Assets/Art/Level01/ModularSlices";
    private const string RoomsPath = "Assets/Level/Rooms/Zone2";

    [MenuItem("Tools/Hamster/Пересобрать арт Зоны II (06-07)")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var level = Object.FindFirstObjectByType<LevelFlowController>();
        Transform roomsRoot = GameObject.Find("Rooms").transform;

        RemoveObject("BakedZone2Art");
        RemoveObject("Room_06_LakeShore");
        RemoveObject("Room_07_Shallows");

        RoomDefinition room06 = EnsureRoom(6, "Берег озера", 0.1f, 202f, 238f,
            "Спустись по ступеням к воде и дойди до мелководья.");
        RoomDefinition room07 = EnsureRoom(7, "Мелководье", 0.6f, 238f, 274f,
            "Перепрыгни по камням и доберись до дока.");
        WireLevelRooms(level);
        RoomDefinition room05 = AssetDatabase.LoadAssetAtPath<RoomDefinition>(
            "Assets/Level/Rooms/Zone1/Room_05.asset");

        GameObject art = new GameObject("BakedZone2Art");
        BuildRoom06(roomsRoot, art.transform, level, room06, room05);
        BuildRoom07(roomsRoot, art.transform, level, room07, room06);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Zone2] Комнаты 06-07 собраны");
    }

    // ---------- комнаты ----------

    private static void BuildRoom06(Transform roomsRoot, Transform art, LevelFlowController level,
        RoomDefinition room, RoomDefinition previous)
    {
        GameObject root = new GameObject("Room_06_LakeShore");
        root.transform.SetParent(roomsRoot, false);

        AddGround(root.transform, "LakeLedge", 202f, 207f, 2f);
        AddGround(root.transform, "LakeStep1", 207f, 209.5f, 1.4f);
        AddGround(root.transform, "LakeStep2", 209.5f, 212f, 0.75f);
        AddGround(root.transform, "LakeBeach", 212f, 238f, 0.1f);

        AddTrigger(root.transform, "Room_06_Trigger", 202.3f, 5.1f, level, room);
        if (previous != null)
            AddTrigger(root.transform, "Room_05_BackTrigger", 201.7f, 7.1f, level, previous);

        AddCheckpoint(root.transform, level, room, 1, "Берег озера", 220f, 0.1f);
        AddFallZone(root.transform, level, "FallZone_Lake", 238f, -5.2f, 78f);

        // Земля: зона I рисует себе арт в BakedLevelArt, здесь свой слой.
        TiledGround(art, "Z2_LedgeGround", 202f, 207.4f, 2f, 10);
        TiledGround(art, "Z2_StepGround_1", 206.6f, 210f, 1.4f, 11);
        TiledGround(art, "Z2_StepGround_2", 209.2f, 212.6f, 0.75f, 12);
        TiledGround(art, "Z2_BeachGround", 211.6f, 238f, 0.1f, 13);

        Water(art, 196f, 278f, -1.2f, -9.2f);
        Decor(art, "Prop_Sign", 217.5f, 0.1f, 1.6f);
        Decor(art, "Prop_Lantern", 221.4f, 0.1f, 1.7f);
        Decor(art, "Prop_Rock", 226f, 0.1f, 0.8f);
        Decor(art, "Prop_Rock", 233.5f, 0.1f, 0.7f);
        Decor(art, "Prop_Flowers", 231f, 0.1f, 0.5f);
        Tree(art, "Z2_TreeShore", "Tree_Birch", 204.5f, 13f);
    }

    private static void BuildRoom07(Transform roomsRoot, Transform art, LevelFlowController level,
        RoomDefinition room, RoomDefinition previous)
    {
        GameObject root = new GameObject("Room_07_Shallows");
        root.transform.SetParent(roomsRoot, false);

        AddStone(root.transform, "Stone_A", 241f, 245f, 0.2f, "SteppingStone_01");
        AddStone(root.transform, "Stone_B", 249f, 253f, 0.8f, "SteppingStone_02");
        AddStone(root.transform, "Stone_C", 257f, 261f, 1.4f, "SteppingStone_03");
        AddDock(root.transform, art, "LakeDock", 266f, 274f, 2f);

        AddTrigger(root.transform, "Room_07_Trigger", 238.3f, 5.6f, level, room);
        if (previous != null)
            AddTrigger(root.transform, "Room_06_BackTrigger", 237.7f, 3.1f, level, previous);

        AddDockFence(root.transform, 266.1f, 2f);
        AddDockFence(root.transform, 273.9f, 2f);
        SpawnHyena(root.transform, 270f, 2.6f);
        AddFallZone(root.transform, level, "FallZone_Shallows", 258f, -5.2f, 80f);

        Water(art, 196f, 278f, -1.2f, -9.2f);
        WaterPatch(art, "Water_Patch_A", 222f, 2.6f);
        WaterPatch(art, "Water_Patch_B", 245f, 2.4f);
        WaterPatch(art, "Water_Patch_C", 259f, 2.3f);
        WaterPatch(art, "Water_Patch_D", 274f, 2.2f);
    }

    // ---------- примитивы сцены ----------

    private static void AddGround(Transform parent, string name, float left, float right, float top)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((left + right) * 0.5f, top - 0.5f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(right - left, 1f);
    }

    private static void AddStone(Transform parent, string name, float left, float right, float top, string sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((left + right) * 0.5f, top - 0.35f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(right - left, 0.7f);

        Sprite stone = Load(sprite);
        if (stone == null)
            return;
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = stone;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 11;
        float scale = (right - left) / stone.bounds.size.x;
        visual.transform.localScale = Vector3.one * scale;
        visual.transform.localPosition = new Vector3(0f, 0.35f - stone.bounds.size.y * scale * 0.5f, 0f);
    }

    private static void AddDock(Transform parent, Transform art, string name, float left, float right, float top)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((left + right) * 0.5f, top - 0.25f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(right - left, 0.5f);

        Sprite sprite = Load("Platform_Wide_A");
        if (sprite == null)
            return;
        GameObject visual = new GameObject(name + "_Visual");
        visual.transform.SetParent(art, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 11;
        float scale = (right - left) / sprite.bounds.size.x;
        visual.transform.localScale = Vector3.one * scale;
        visual.transform.position = new Vector3((left + right) * 0.5f, top - sprite.bounds.size.y * scale * 0.5f + 0.02f, 0f);
    }

    private static void AddTrigger(Transform parent, string name, float x, float y, LevelFlowController level, RoomDefinition room)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1f, 12f);
        RoomTrigger trigger = go.AddComponent<RoomTrigger>();
        SerializedObject so = new SerializedObject(trigger);
        so.FindProperty("level").objectReferenceValue = level;
        so.FindProperty("room").objectReferenceValue = room;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddCheckpoint(Transform parent, LevelFlowController level, RoomDefinition room,
        int index, string label, float x, float groundY)
    {
        GameObject go = new GameObject("Checkpoint_Lake");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, groundY, 0f);

        Sprite baseSprite = Load("Checkpoint_Campfire_Unlit");
        GameObject baseVisual = new GameObject("Base");
        baseVisual.transform.SetParent(go.transform, false);
        SpriteRenderer baseRenderer = baseVisual.AddComponent<SpriteRenderer>();
        baseRenderer.sprite = baseSprite;
        baseRenderer.sortingLayerName = "Props";
        baseRenderer.sortingOrder = 3;
        float baseHeight = 1.44f;
        if (baseSprite != null)
            baseVisual.transform.localScale = Vector3.one * (baseHeight / baseSprite.bounds.size.y);

        GameObject flame = new GameObject("Flame");
        flame.transform.SetParent(go.transform, false);
        flame.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        SpriteRenderer flameRenderer = flame.AddComponent<SpriteRenderer>();
        Sprite flameSprite = Load("Campfire_Flame_01");
        flameRenderer.sprite = flameSprite;
        flameRenderer.sortingLayerName = "Props";
        flameRenderer.sortingOrder = 4;
        if (flameSprite != null)
            flame.transform.localScale = Vector3.one * 0.9f;
        flameRenderer.enabled = false;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(3.2f, 2.6f);
        box.offset = new Vector2(0f, 1.1f);

        RoomCheckpoint checkpoint = go.AddComponent<RoomCheckpoint>();
        SerializedObject so = new SerializedObject(checkpoint);
        so.FindProperty("level").objectReferenceValue = level;
        so.FindProperty("room").objectReferenceValue = room;
        so.FindProperty("checkpointIndex").intValue = index;
        so.FindProperty("checkpointLabel").stringValue = label;
        so.FindProperty("flameRenderer").objectReferenceValue = flameRenderer;
        SerializedProperty frames = so.FindProperty("flameFrames");
        string[] names = { "Campfire_Flame_01", "Campfire_Flame_02", "Campfire_Flame_03", "Campfire_Flame_04", "Campfire_Flame_05", "Campfire_Flame_06" };
        frames.arraySize = names.Length;
        for (int i = 0; i < names.Length; i++)
            frames.GetArrayElementAtIndex(i).objectReferenceValue = Load(names[i]);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddFallZone(Transform parent, LevelFlowController level, string name, float x, float y, float width)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(width, 2.5f);
        FallRespawnZone zone = go.AddComponent<FallRespawnZone>();
        SerializedObject so = new SerializedObject(zone);
        so.FindProperty("level").objectReferenceValue = level;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddDockFence(Transform parent, float x, float top)
    {
        GameObject fence = new GameObject("DockFence_" + x.ToString("0"));
        fence.transform.SetParent(parent, false);
        fence.transform.position = new Vector3(x, top + 1f, 0f);
        BoxCollider2D box = fence.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.2f, 2f);
    }
    private static void SpawnHyena(Transform parent, float x, float y)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hyena.prefab");
        if (prefab == null)
            return;
        GameObject hyena = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        hyena.name = "ShallowsHyena";
        hyena.transform.SetParent(parent, false);
        hyena.transform.position = new Vector3(x, y, 0f);
    }

    // ---------- арт ----------

    private static void TiledGround(Transform parent, string name, float left, float right, float top, int order)
    {
        Sprite sprite = Load("Ground_Tile");
        if (sprite == null)
            return;
        const float inset = 1.25f;
        float height = 7.05f;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((left + right) * 0.5f, top - height * 0.5f + inset, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(right - left, height);
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = order;

        UndergroundFill(parent, name + "_Fill", left, right, top - 5.8f, order - 1);
    }

    private static void UndergroundFill(Transform parent, string name, float left, float right, float top, int order)
    {
        Sprite soil = Load("Ground_Soil_Tile");
        if (soil == null)
            return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        float height = 8f;
        go.transform.position = new Vector3((left + right) * 0.5f, top - height * 0.5f + 0.35f, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = soil;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(right - left, height);
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = order;
    }

    private static void Water(Transform parent, float left, float right, float surfaceY, float bottomY)
    {
        Sprite deep = Load("WaterDeep");
        Sprite texture = Load("LakeWater");
        float width = right - left;
        float height = surfaceY - bottomY;

        if (deep != null)
        {
            GameObject body = new GameObject("Z2_WaterBody");
            body.transform.SetParent(parent, false);
            body.transform.position = new Vector3((left + right) * 0.5f, surfaceY - height * 0.5f, 0f);
            SpriteRenderer renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = deep;
            renderer.sortingLayerName = "Ground";
            renderer.sortingOrder = 8;
            body.transform.localScale = new Vector3(width / deep.bounds.size.x, height / deep.bounds.size.y, 1f);
        }

        if (texture != null)
        {
            GameObject surface = new GameObject("Z2_WaterSurface");
            surface.transform.SetParent(parent, false);
            surface.transform.position = new Vector3((left + right) * 0.5f, surfaceY - 1.3f, 0f);
            SpriteRenderer renderer = surface.AddComponent<SpriteRenderer>();
            renderer.sprite = texture;
            renderer.sortingLayerName = "Ground";
            renderer.sortingOrder = 8;
            surface.transform.localScale = new Vector3(width / texture.bounds.size.x, 2.6f / texture.bounds.size.y, 1f);
        }

        Sprite wave = Load("Water_Wave_Thin");
        if (wave != null)
        {
            GameObject line = new GameObject("Z2_Waterline");
            line.transform.SetParent(parent, false);
            line.transform.position = new Vector3((left + right) * 0.5f, surfaceY - 0.15f, 0f);
            SpriteRenderer lineRenderer = line.AddComponent<SpriteRenderer>();
            lineRenderer.sprite = wave;
            lineRenderer.drawMode = SpriteDrawMode.Tiled;
            lineRenderer.size = new Vector2(width, 0.7f);
            lineRenderer.sortingLayerName = "Ground";
            lineRenderer.sortingOrder = 9;
        }
    }
    private static void WaterPatch(Transform parent, string spriteName, float x, float height)
    {
        Sprite sprite = Load(spriteName);
        if (sprite == null)
            return;
        GameObject go = new GameObject(spriteName + "_" + x.ToString("0"));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, -1.2f - 0.2f, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 9;
        go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
    }

    private static void Decor(Transform parent, string spriteName, float x, float bottomY, float height)
    {
        Sprite sprite = Load(spriteName);
        if (sprite == null)
            return;
        GameObject go = new GameObject(spriteName + "_" + x.ToString("0.#"));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, bottomY, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Props";
        renderer.sortingOrder = 2;
        go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
    }

    private static void Tree(Transform parent, string name, string spriteName, float x, float height)
    {
        Sprite sprite = Load(spriteName);
        if (sprite == null)
            return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 2f, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Mid";
        renderer.sortingOrder = -7;
        go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
    }

    // ---------- данные ----------

    private static RoomDefinition EnsureRoom(int id, string displayName, float surfaceY,
        float minX, float maxX, string objective)
    {
        if (!AssetDatabase.IsValidFolder(RoomsPath))
            AssetDatabase.CreateFolder("Assets/Level/Rooms", "Zone2");

        string path = RoomsPath + "/Room_" + id.ToString("00") + ".asset";
        RoomDefinition room = AssetDatabase.LoadAssetAtPath<RoomDefinition>(path);
        if (room == null)
        {
            room = ScriptableObject.CreateInstance<RoomDefinition>();
            AssetDatabase.CreateAsset(room, path);
        }

        SerializedObject so = new SerializedObject(room);
        so.FindProperty("roomId").intValue = id;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("zoneId").intValue = 2;
        so.FindProperty("zoneName").stringValue = "Озеро";
        so.FindProperty("objective").stringValue = objective;
        so.FindProperty("cameraBoundsMin").vector2Value = new Vector2(minX, -6f);
        so.FindProperty("cameraBoundsMax").vector2Value = new Vector2(maxX, 18f);
        so.FindProperty("surfaceY").floatValue = surfaceY;
        so.FindProperty("worldMinX").floatValue = minX;
        so.FindProperty("worldMaxX").floatValue = maxX;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(room);
        AssetDatabase.SaveAssets();
        return room;
    }

    private static void WireLevelRooms(LevelFlowController level)
    {
        if (level == null)
            return;

        RoomDefinition[] rooms = Enumerable.Range(1, 7)
            .Select(id =>
            {
                string folder = id <= 5 ? "Assets/Level/Rooms/Zone1" : RoomsPath;
                return AssetDatabase.LoadAssetAtPath<RoomDefinition>(folder + "/Room_" + id.ToString("00") + ".asset");
            })
            .Where(room => room != null)
            .ToArray();

        SerializedObject so = new SerializedObject(level);
        SerializedProperty list = so.FindProperty("roomDefinitions");
        list.arraySize = rooms.Length;
        for (int i = 0; i < rooms.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = rooms[i];
        SerializedProperty start = so.FindProperty("startRoom");
        if (start.objectReferenceValue == null)
            start.objectReferenceValue = rooms.Length > 0 ? rooms[0] : null;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(level);
    }

    private static Sprite Load(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        return AssetDatabase.LoadAssetAtPath<Sprite>(SlicesPath + "/" + name + ".png");
    }

    private static void RemoveObject(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
            Object.DestroyImmediate(go);
    }
}



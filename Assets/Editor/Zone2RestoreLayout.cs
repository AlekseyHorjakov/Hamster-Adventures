using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Восстанавливает раскладку Зоны II по абсолютным высотам: берег 06, камни 07, чекпоинт.
/// Коллайдер ставится на проектную высоту, визуал выравнивается по нему (без сдвига корней "на глаз").
/// </summary>
public static class Zone2RestoreLayout
{
    private const string ScenePath = "Assets/Scenes/Level01_Full.unity";
    private const string Slices = "Assets/Art/Level01/WaterSlices";

    private static readonly (string name, float top)[] Stones =
    {
        ("ShoreStone", 0.2f), ("Stone_A", 0.2f), ("Stone_B", 0.8f),
        ("Stone_C", 1.4f), ("StepStone_1", 0.5f), ("StepStone_2", 1.75f)
    };

    [MenuItem("Tools/Хомяк/Зона II: восстановить раскладку (берег, камни, чекпоинт)")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RestoreBeach();
        foreach (var stone in Stones)
            RestoreStone(stone.name, stone.top);
        RestoreCheckpoint();
        EnsureVisual("ShoreStone", "Stone_13", 1.1f);
        EnsureVisual("LakeDock", "Platform_Wide_A", 0.9f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Restore] раскладка восстановлена");
    }

    private static void RestoreBeach()
    {
        GameObject room = GameObject.Find("Room_06_LakeShore");
        if (room == null)
            return;

        GameObject old = GameObject.Find("Z2_BeachGround");
        if (old != null)
            Object.DestroyImmediate(old);
        GameObject oldFill = GameObject.Find("Z2_BeachGround_Fill");
        if (oldFill != null)
            Object.DestroyImmediate(oldFill);

        GameObject art = GameObject.Find("BakedZone2Art");
        Transform parent = art != null ? art.transform : room.transform;

        Sprite tile = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Level01/ModularSlices/Ground_Tile.png");
        Sprite soil = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Level01/ModularSlices/Ground_Soil_Tile.png");
        const float left = 211.6f;
        const float right = 238f;
        const float top = 0.1f;

        if (tile != null)
        {
            GameObject ground = new GameObject("Z2_BeachGround");
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3((left + right) * 0.5f, top - 7.05f * 0.5f + 1.25f, 0f);
            SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = tile;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(right - left, 7.05f);
            renderer.sortingLayerName = "Ground";
            renderer.sortingOrder = 12;
        }

        if (soil != null)
        {
            GameObject fill = new GameObject("Z2_BeachGround_Fill");
            fill.transform.SetParent(parent, false);
            fill.transform.position = new Vector3((left + right) * 0.5f, top - 5.45f - 4f, 0f);
            SpriteRenderer renderer = fill.AddComponent<SpriteRenderer>();
            renderer.sprite = soil;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(right - left, 8f);
            renderer.sortingLayerName = "Ground";
            renderer.sortingOrder = 11;
        }
    }

    private static void RestoreStone(string name, float designTop)
    {
        GameObject stone = GameObject.Find(name);
        if (stone == null)
            return;

        Collider2D col = stone.GetComponent<Collider2D>();
        if (col == null)
            return;

        // 1) Корень поднимаем так, чтобы верх коллайдера встал на проектную высоту.
        float delta = designTop - col.bounds.max.y;
        stone.transform.position += new Vector3(0f, delta, 0f);

        // 2) Визуал подтягиваем к верху коллайдера (ребёнок не двигает коллайдер).
        SpriteRenderer visual = stone.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault();
        if (visual == null)
            return;

        float visualTop = visual.bounds.max.y;
        float visualDelta = designTop - visualTop;
        visual.transform.position += new Vector3(0f, visualDelta, 0f);

        // 3) Если камень «утонул» сильно ниже воды, подрезаем высоту спрайта, чтобы не торчал гигантом.
        visual.sortingLayerName = "Ground";
        visual.sortingOrder = 12;
    }

    private static void EnsureVisual(string name, string spriteName, float height)
    {
        GameObject root = GameObject.Find(name);
        if (root == null)
            return;
        SpriteRenderer existing = root.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault();
        if (existing != null && existing.sprite != null)
            return;

        Sprite sprite = spriteName.StartsWith("Stone_")
            ? AssetDatabase.LoadAssetAtPath<Sprite>(Slices + "/" + spriteName + ".png")
            : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Level01/ModularSlices/" + spriteName + ".png");
        if (sprite == null)
            return;

        Collider2D col = root.GetComponent<Collider2D>();
        if (col == null)
            return;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 12;

        float width = col.bounds.size.x;
        float scaleX = width / sprite.bounds.size.x;
        float scaleY = height / sprite.bounds.size.y;
        visual.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        float topLocal = col.bounds.max.y - root.transform.position.y;
        visual.transform.localPosition = new Vector3(0f, topLocal - height * 0.5f, 0f);
    }

    private static void RestoreCheckpoint()
    {
        GameObject checkpoint = GameObject.Find("Checkpoint_Lake");
        if (checkpoint == null)
            return;

        checkpoint.transform.position = new Vector3(checkpoint.transform.position.x, 0.1f, 0f);

        Transform baseVisual = checkpoint.transform.Find("Base");
        if (baseVisual != null)
            baseVisual.localPosition = new Vector3(0f, 0.72f, 0f);
        Transform flame = checkpoint.transform.Find("Flame");
        if (flame != null)
            flame.localPosition = new Vector3(0f, 1.55f, 0f);
    }
}

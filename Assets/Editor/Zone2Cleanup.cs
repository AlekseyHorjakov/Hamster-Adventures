using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Чистка Зоны II: убирает коллайдеры, оставшиеся от удалённых платформ,
/// и возвращает берегу озера его прежние размеры (широкий фон под окружение).
/// </summary>
public static class Zone2Cleanup
{
    private const string ScenePath = "Assets/Scenes/Level01_Full.unity";

    private static readonly string[] PlatformNames =
    {
        "Island_1", "Island_2", "Island_3", "Island_Final",
        "CrossPlatform_1", "CrossPlatform_2", "CrossPlatform_3",
        "FallLedge_1", "FallLedge_2", "FallLedge_3",
        "Stone_A", "Stone_B", "Stone_C", "ShoreStone",
        "StepStone_1", "StepStone_2", "LakeDock",
        "Bridge_Patch_04", "Bridge_Section_01", "Bridge_Section_02"
    };

    [MenuItem("Tools/Хомяк/Зона II: убрать лишние коллайдеры и вернуть берег")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        int removedColliders = RemoveOrphanColliders();
        RestoreBank();
        RebuildLakeBackdrop();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Cleanup] убрано коллайдеров: " + removedColliders);
    }

    private static int RemoveOrphanColliders()
    {
        int removed = 0;
        foreach (string name in PlatformNames)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
                continue;

            // Платформа считается удалённой, если у неё нет ни одного видимого спрайта.
            bool hasVisibleSprite = go.GetComponentsInChildren<SpriteRenderer>(true)
                .Any(sr => sr != null && sr.sprite != null && sr.enabled);
            if (hasVisibleSprite)
                continue;

            foreach (Collider2D col in go.GetComponents<Collider2D>())
            {
                Object.DestroyImmediate(col);
                removed++;
            }

            if (go.GetComponents<Component>().Length <= 1)
                Object.DestroyImmediate(go);
        }
        return removed;
    }

    private static void RebuildLakeBackdrop()
    {
        GameObject old = GameObject.Find("Z2W_LakeBackdrop");
        if (old != null)
            Object.DestroyImmediate(old);

        Sprite lake = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Level01/WaterSlices/LakeBackdrop_Fade.png");
        if (lake == null)
            return;

        GameObject backdrop = new GameObject("Z2W_LakeBackdrop");
        backdrop.transform.position = new Vector3(294f, 0.1f, 0f);
        SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
        renderer.sprite = lake;
        renderer.sortingLayerName = "Mid";
        renderer.sortingOrder = -19;
        backdrop.transform.localScale = new Vector3(200f / lake.bounds.size.x, 22f / lake.bounds.size.y, 1f);
    }

    private static void RestoreBank()
    {
        SpriteRenderer bank = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(sr => sr != null && sr.gameObject.name.Contains("Shore"))
            .OrderByDescending(sr => sr.bounds.size.x)
            .FirstOrDefault();
        if (bank == null)
            return;

        Sprite sheet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Level01/ModularSlices/WaterShore_Edge.png");
        if (sheet != null)
            bank.sprite = sheet;

        bank.drawMode = SpriteDrawMode.Simple;
        bank.sortingLayerName = "Ground";
        bank.sortingOrder = 7;

        // Прежние размеры под окружение: 200 в ширину, ~27.8 в высоту, низ на линии воды.
        float width = 200f;
        float height = 27.8f;
        bank.transform.localScale = new Vector3(width / bank.sprite.bounds.size.x, height / bank.sprite.bounds.size.y, 1f);
        // Как было у тебя: верх полосы на уровне берега (2.4), вниз до дна.
        bank.transform.position = new Vector3(294f, 2.4f - height * 0.5f, 0f);
        bank.enabled = true;
        bank.gameObject.SetActive(true);
    }
}

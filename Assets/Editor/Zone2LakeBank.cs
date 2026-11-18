using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Берег озера из готового спрайта: ставит его над озерным фоном (перед LakeBackdrop),
/// растягивает на всю длину озера, чтобы он не пропадал в билде.
/// </summary>
public static class Zone2LakeBank
{
    private const string ScenePath = "Assets/Scenes/Level01_Full.unity";

    [MenuItem("Tools/Хомяк/Зона II: берег озера на фон (растянуть и поднять слой)")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Ищем берег: спрайт Shore*, самый широкий из найденных.
        SpriteRenderer bank = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(sr => sr != null && sr.sprite != null && sr.sprite.name.Contains("Shore"))
            .OrderByDescending(sr => sr.bounds.size.x)
            .FirstOrDefault();

        if (bank == null)
        {
            Debug.LogWarning("[LakeBank] берег не найден");
            return;
        }

        // 1) Перед озерным фоном: слой Ground, порядок 7 (вода — 8).
        bank.sortingLayerName = "Ground";
        bank.sortingOrder = 7;

        // 2) Тянем полосой на всю длину озера: тайлим по X, высоту берём родную.
        const float targetWidth = 200f;
        const float centerX = 294f;
        const float waterline = -1.2f;
        float topY = bank.bounds.max.y;

        // Лист WaterShore_Edge — это три полосы сразу; берём одну, иначе тайлится тремя рядами.
        Sprite row = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Level01/ModularSlices/Shore_Row_A.png");
        if (row != null)
            bank.sprite = row;

        float nativeHeight = bank.sprite.bounds.size.y;

        bank.transform.localScale = Vector3.one;
        bank.drawMode = SpriteDrawMode.Tiled;
        bank.size = new Vector2(targetWidth, nativeHeight);
        // Низ полосы сажаем ровно на линию воды, чтобы не было разрыва.
        bank.transform.position = new Vector3(centerX, waterline + nativeHeight * 0.5f, 0f);

        // Мой прежний фон больше не нужен — берег сам закрывает озеро.
        GameObject backdrop = GameObject.Find("Z2W_LakeBackdrop");
        if (backdrop != null)
            Object.DestroyImmediate(backdrop);

        // 3) Включаем принудительно (мог быть выключен).
        bank.enabled = true;
        bank.gameObject.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LakeBank] берег растянут: " + bank.name + " -> " + targetWidth + " units, Ground/7");
    }
}

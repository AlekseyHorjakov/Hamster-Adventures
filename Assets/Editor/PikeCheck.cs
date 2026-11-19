using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Числовая проверка посадки щуки: границы спрайта, вода и кромки секций моста.</summary>
public static class PikeCheck
{
    [MenuItem("Tools/Хомяк/Зона II: проверить посадку щуки")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);

        var pike = GameObject.Find("Pike_CrossingGap");
        if (pike == null)
        {
            Debug.LogError("[PikeCheck] нет Pike_CrossingGap");
            return;
        }

        var sr = pike.GetComponent<SpriteRenderer>();
        Debug.Log($"[PikeCheck] pike pos={pike.transform.position} scale={pike.transform.localScale} " +
                  $"sprite={(sr != null && sr.sprite != null ? sr.sprite.name : "null")} " +
                  $"bounds={(sr != null ? sr.bounds.ToString() : "-")}");

        foreach (string name in new[] { "Z2_WaterSurface", "Z2_WaterBody", "Bridge_Section_02", "Bridge_Section_01", "Bridge_Section_01 (1)", "Bridge_Section_02 (1)" })
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                Debug.Log($"[PikeCheck] {name}: нет");
                continue;
            }

            var renderer = go.GetComponentInChildren<SpriteRenderer>();
            var collider = go.GetComponent<Collider2D>();
            string rb = renderer != null ? renderer.bounds.ToString() : "-";
            string cb = collider != null ? collider.bounds.ToString() : "-";
            string sink = go.GetComponent<SinkingPlatform>() != null ? " тонет" : "";
            Debug.Log($"[PikeCheck] {name}: pos={go.transform.position} sprite={rb} collider={cb}{sink}");
        }
    }
}

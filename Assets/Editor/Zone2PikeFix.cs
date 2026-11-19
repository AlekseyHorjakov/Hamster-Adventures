using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Две правки после сборки: щука была на слое Default и рисовалась за всей сценой,
/// у диагонального склона GroundEdge_SlopeA стоял динамический Rigidbody2D и он падал в игре.
/// </summary>
public static class Zone2PikeFix
{
    [MenuItem("Tools/Хомяк/Зона II: починить щуку и склон")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);

        var pike = GameObject.Find("Pike_CrossingGap");
        if (pike != null)
        {
            var sr = pike.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = "Ground";
                sr.sortingOrder = 15;
            }
            Debug.Log("[PikeFix] щука: слой Ground, order 15");
        }
        else
        {
            Debug.LogError("[PikeFix] нет Pike_CrossingGap");
        }

        var slope = GameObject.Find("GroundEdge_SlopeA");
        if (slope != null)
        {
            var body = slope.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                // Склон — статичная часть уровня: динамическое тело уводило его вниз за кадр.
                body.bodyType = RigidbodyType2D.Static;
                body.simulated = true;
                Debug.Log("[PikeFix] GroundEdge_SlopeA: Rigidbody2D -> Static");
            }
            else
            {
                Debug.Log("[PikeFix] GroundEdge_SlopeA: Rigidbody2D уже нет");
            }
        }
        else
        {
            Debug.LogError("[PikeFix] нет GroundEdge_SlopeA");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
}

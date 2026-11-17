using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class Cover3
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        var points=new[]{ new Vector3(208f,0f,0f), new Vector3(210f,0f,0f), new Vector3(212f,0f,0f), new Vector3(214f,0f,0f), new Vector3(216f,0f,0f) };
        foreach(var p in points)
        {
            sb.AppendLine("POINT "+p.ToString("F1"));
            foreach(var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if(sr.sprite==null) continue;
                if(!sr.bounds.Contains(p)) continue;
                sb.AppendLine("  "+sr.name+" order="+sr.sortingOrder+" boundsMin="+sr.bounds.min.ToString("F1")+" col="+sr.color.ToString("F2"));
            }
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/cover3.txt",sb.ToString()); Debug.Log("done");
    }
}

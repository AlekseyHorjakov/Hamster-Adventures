using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class Cover2
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        foreach(var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if(sr.sprite==null) continue;
            foreach(var p in new[]{ new Vector3(226f,-3f,0f), new Vector3(209f,1f,0f), new Vector3(225f,0f,0f) })
            {
                if(!sr.bounds.Contains(p)) continue;
                sb.AppendLine("at "+p.ToString("F0")+" -> "+sr.name+" sprite="+sr.sprite.name+" order="+sr.sortingOrder+" bounds="+sr.bounds.min.ToString("F1")+".."+sr.bounds.max.ToString("F1")+" draw="+sr.drawMode);
                break;
            }
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/cover2.txt",sb.ToString()); Debug.Log("done");
    }
}

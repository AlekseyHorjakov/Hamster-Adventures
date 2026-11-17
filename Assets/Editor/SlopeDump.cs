using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class SlopeDump
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if(!t.name.StartsWith("Z2_") && !t.name.StartsWith("Checkpoint_Lake") && !t.name.StartsWith("Checkpoint_Camp")) continue;
            var sr=t.GetComponent<SpriteRenderer>();
            if(sr==null) continue;
            sb.AppendLine(t.name+" sprite="+(sr.sprite==null?"NULL":sr.sprite.name)+" enabled="+sr.enabled+
              " order="+sr.sortingOrder+" bounds="+sr.bounds.min.ToString("F1")+".."+sr.bounds.max.ToString("F1"));
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/slope.txt",sb.ToString()); Debug.Log("done");
    }
}

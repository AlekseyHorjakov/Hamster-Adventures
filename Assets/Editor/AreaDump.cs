using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class AreaDump
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach(var c in t.GetComponents<Collider2D>())
            {
                if(c.isTrigger) continue;
                var b=c.bounds;
                if(b.max.x<330f || b.min.x>400f) continue;
                if(b.max.y<1f) continue;
                var sr=t.GetComponentInChildren<SpriteRenderer>(true);
                sb.AppendLine(t.name+" "+c.GetType().Name+" bounds="+b.min.ToString("F1")+".."+b.max.ToString("F1")+
                   " sprite="+(sr==null?"none":(sr.sprite==null?"NULL":sr.sprite.name+" enabled="+sr.enabled)));
            }
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/area.txt",sb.ToString()); Debug.Log("done");
    }
}

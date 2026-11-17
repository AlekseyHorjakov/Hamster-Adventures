using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class WallDump
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach(var box in t.GetComponents<BoxCollider2D>())
            {
                var b=box.bounds;
                if(b.max.x < 195f || b.min.x > 250f) continue;
                if(box.isTrigger) continue;
                sb.AppendLine(t.name+" SOLID x="+b.min.x.ToString("F1")+".."+b.max.x.ToString("F1")+
                  " y="+b.min.y.ToString("F1")+".."+b.max.y.ToString("F1")+" parent="+(t.parent==null?"-":t.parent.name));
            }
            foreach(var col in t.GetComponents<Collider2D>())
            {
                var b=col.bounds;
                if(b.max.x < 195f || b.min.x > 250f) continue;
                if(!col.isTrigger) continue;
                sb.AppendLine(t.name+" TRIGGER x="+b.min.x.ToString("F1")+".."+b.max.x.ToString("F1")+
                  " y="+b.min.y.ToString("F1")+".."+b.max.y.ToString("F1"));
            }
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/walls.txt",sb.ToString()); Debug.Log("done");
    }
}

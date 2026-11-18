using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class BridgeDump
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            bool interesting = t.name.StartsWith("Bridge") || t.name.StartsWith("Island") || t.name.StartsWith("Stone") || t.name.StartsWith("StepStone") || t.name.StartsWith("LakeDock") || t.name.StartsWith("CrossPlatform") || t.name.StartsWith("FallLedge");
            if(!interesting) continue;
            var sr=t.GetComponent<SpriteRenderer>();
            var col=t.GetComponent<Collider2D>();
            sb.AppendLine(t.name+" pos="+t.position.ToString("F2")+" scale="+t.lossyScale.ToString("F2")+" layer="+(sr==null?"-":sr.sortingLayerName+"/"+sr.sortingOrder)+
              (sr==null?"":" sprite="+(sr.sprite==null?"NULL":sr.sprite.name)+" bounds="+sr.bounds.min.ToString("F2")+".."+sr.bounds.max.ToString("F2"))+
              (col==null?" COL=none":" COL="+col.GetType().Name+" top="+col.bounds.max.y.ToString("F2")+" x="+col.bounds.min.x.ToString("F1")+".."+col.bounds.max.x.ToString("F1")));
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/bridges.txt",sb.ToString()); Debug.Log("done");
    }
}

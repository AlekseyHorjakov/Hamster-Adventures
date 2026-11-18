using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class Leftovers
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        sb.AppendLine("== colliders without renderer in zone II ==");
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if(t.GetComponentInChildren<SpriteRenderer>(true)!=null) continue;
            var cols=t.GetComponents<Collider2D>();
            if(cols.Length==0) continue;
            var b=cols[0].bounds;
            if(b.max.x<196f) continue;
            foreach(var c in cols)
                sb.AppendLine(t.name+" parent="+(t.parent==null?"-":t.parent.name)+" "+c.GetType().Name+" trigger="+c.isTrigger+" bounds="+c.bounds.min.ToString("F1")+".."+c.bounds.max.ToString("F1"));
        }
        sb.AppendLine("== bank ==");
        foreach(var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if(sr.sprite==null) continue;
            if(!sr.sprite.name.Contains("Shore")&&!sr.gameObject.name.Contains("Shore")) continue;
            sb.AppendLine(sr.name+" sprite="+sr.sprite.name+" layer="+sr.sortingLayerName+"/"+sr.sortingOrder+" scale="+sr.transform.lossyScale.ToString("F2")+" bounds="+sr.bounds.min.ToString("F1")+".."+sr.bounds.max.ToString("F1")+" draw="+sr.drawMode+" size="+sr.size);
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/left.txt",sb.ToString()); Debug.Log("done");
    }
}

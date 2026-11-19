using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class RemoveVisualColliders
{
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        int removed=0;
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if(t==null) continue;
            if(t.name!="Visual") continue;
            var any=t.GetComponentInParent<Transform>();
            // только Зона II: по X родителя
            float x=t.position.x;
            if(x<196f || x>400f) continue;
            foreach(var c in t.GetComponents<Collider2D>())
            {
                sb.AppendLine("removed "+t.name+" parent="+(t.parent==null?"-":t.parent.name)+" "+(c==null?"":c.GetType().Name)+
                  " bounds="+c.bounds.min.ToString("F1")+".."+c.bounds.max.ToString("F1"));
                Object.DestroyImmediate(c);
                removed++;
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        System.IO.File.WriteAllText("C:/Windows/Temp/removed.txt",sb.ToString()+"\ntotal="+removed);
        Debug.Log("[RemoveVisualColliders] удалено: "+removed);
    }
}

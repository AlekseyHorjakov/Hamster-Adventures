using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class Z2Dump
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if(!t.name.StartsWith("Z2_")) continue;
            var sr=t.GetComponent<SpriteRenderer>(); var box=t.GetComponent<BoxCollider2D>();
            sb.AppendLine(t.name+" pos="+t.position.ToString("F2")+" scale="+t.lossyScale.ToString("F2")+
              (sr==null?"":" VIS top="+sr.bounds.max.y.ToString("F2")+" bot="+sr.bounds.min.y.ToString("F2")+" w="+sr.bounds.size.x.ToString("F2")+" draw="+sr.drawMode+" sprite="+sr.sprite?.name+" order="+sr.sortingOrder)+
              (box==null?"":" COL top="+box.bounds.max.y.ToString("F2")));
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/z2.txt",sb.ToString()); Debug.Log("done");
    }
}

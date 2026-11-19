using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class RemoveCrossStart
{
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        GameObject go = GameObject.Find("CrossStart");
        if (go == null) { sb.AppendLine("CrossStart not found"); }
        else
        {
            foreach (var c in go.GetComponents<Collider2D>())
            {
                sb.AppendLine("removed CrossStart " + c.GetType().Name + " bounds=" + c.bounds.min.ToString("F2") + ".." + c.bounds.max.ToString("F2"));
                Object.DestroyImmediate(c);
            }
            if (go.GetComponents<Component>().Length <= 1)
            {
                sb.AppendLine("removed empty object CrossStart");
                Object.DestroyImmediate(go);
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        System.IO.File.WriteAllText("C:/Windows/Temp/crossstart.txt", sb.ToString());
        Debug.Log("[RemoveCrossStart] done");
    }
}

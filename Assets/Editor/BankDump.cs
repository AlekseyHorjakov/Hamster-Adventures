using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class BankDump
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var sr=t.GetComponent<SpriteRenderer>();
            if(sr==null||sr.sprite==null) continue;
            if(!sr.sprite.name.Contains("Shore")&&!t.name.Contains("Shore")&&!t.name.Contains("LakeBackdrop")) continue;
            sb.AppendLine(t.name+" parent="+(t.parent==null?"-":t.parent.name)+" sprite="+sr.sprite.name+" layer="+sr.sortingLayerName+"/"+sr.sortingOrder+
              " scale="+t.lossyScale.ToString("F2")+" bounds="+sr.bounds.min.ToString("F1")+".."+sr.bounds.max.ToString("F1")+
              " enabled="+sr.enabled+" active="+t.gameObject.activeInHierarchy);
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/bank.txt",sb.ToString()); Debug.Log("done");
    }
}

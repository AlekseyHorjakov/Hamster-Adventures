using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class PlatDump
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        string[] names={"Island_1","Island_2","Island_3","Island_Final","CrossPlatform_1","CrossPlatform_2","CrossPlatform_3","FallLedge_1","FallLedge_2","FallLedge_3","Stone_A","Stone_B","Stone_C","ShoreStone","LakeDock","LakeLedge","LakeStep1","LakeStep2","LakeBeach","FallBase","FallTop","CrossStart","CrossExit"};
        foreach(var name in names)
        {
            var go=GameObject.Find(name);
            if(go==null){sb.AppendLine(name+" NULL");continue;}
            sb.AppendLine(name+" pos="+go.transform.position.ToString("F2")+" scale="+go.transform.lossyScale.ToString("F2"));
            var visual=go.transform.Find("Visual");
            if(visual!=null){var sr=visual.GetComponent<SpriteRenderer>(); if(sr!=null) sb.AppendLine("   VIS "+sr.sprite?.name+" bounds="+sr.bounds.min.ToString("F2")+".."+sr.bounds.max.ToString("F2"));}
            foreach(var col in go.GetComponents<Collider2D>())
                sb.AppendLine("   COL "+col.GetType().Name+" enabled="+col.enabled+" bounds="+col.bounds.min.ToString("F2")+".."+col.bounds.max.ToString("F2"));
        }
        sb.AppendLine("HYENAS");
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if(!t.name.ToLower().Contains("hyena")) continue;
            sb.AppendLine(" "+t.name+" parent="+(t.parent==null?"-":t.parent.name)+" pos="+t.position.ToString("F1")+" active="+t.gameObject.activeSelf);
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/plat.txt",sb.ToString()); Debug.Log("done");
    }
}

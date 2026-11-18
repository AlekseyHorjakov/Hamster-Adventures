using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class QuickCheck
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        foreach(var n in new[]{"Z2_BeachGround","Z2_LedgeGround","Z2_DescentSlope","ShoreStone","Stone_A","Stone_B","Stone_C","StepStone_1","StepStone_2","LakeDock","Checkpoint_Lake","Bridge_Patch_04"})
        {
            var go=GameObject.Find(n);
            if(go==null){sb.AppendLine(n+" NULL");continue;}
            var sr=go.GetComponentInChildren<SpriteRenderer>(true);
            var col=go.GetComponent<Collider2D>();
            sb.AppendLine(n+" active="+go.activeInHierarchy+
              (sr==null?" SR=NULL":" sprite="+(sr.sprite==null?"NULL":sr.sprite.name)+" enabled="+sr.enabled+" bounds="+sr.bounds.min.ToString("F1")+".."+sr.bounds.max.ToString("F1"))+
              (col==null?" COL=none":" COL top="+col.bounds.max.y.ToString("F2")));
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/quick.txt",sb.ToString()); Debug.Log("done");
    }
}

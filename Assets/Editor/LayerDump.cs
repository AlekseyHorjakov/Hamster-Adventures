using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class LayerDump
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        foreach(var n in new[]{"Z2_BeachGround","Z2_BeachGround_Fill","Z2_DescentSlope","Z2_WaterBody","Z2_WaterSurface","Z2_Waterline","Z2_LedgeGround"})
        {
            var go=GameObject.Find(n);
            if(go==null){sb.AppendLine(n+" NULL");continue;}
            var sr=go.GetComponent<SpriteRenderer>();
            sb.AppendLine(n+" layer='"+sr.sortingLayerName+"' order="+sr.sortingOrder+" enabled="+sr.enabled+
              " size="+sr.size+" draw="+sr.drawMode+" scale="+sr.transform.lossyScale.ToString("F2")+
              " bounds="+sr.bounds.min.ToString("F1")+".."+sr.bounds.max.ToString("F1"));
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/layers.txt",sb.ToString()); Debug.Log("done");
    }
}

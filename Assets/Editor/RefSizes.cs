using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class RefSizes
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        foreach(var name in new[]{"Checkpoint_Camp","Checkpoint_Lake","Prop_Sign_54"})
        {
            var go=GameObject.Find(name);
            sb.AppendLine(name+" found="+(go!=null));
            if(go==null) continue;
            foreach(var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                sb.AppendLine("  "+sr.name+" sprite="+(sr.sprite==null?"NULL":sr.sprite.name)+" enabled="+sr.enabled+" bounds="+sr.bounds.min.ToString("F2")+".."+sr.bounds.max.ToString("F2"));
        }
        foreach(var name in new[]{"Island_1","Island_2","Island_3","CrossPlatform_1","LakeDock","Stone_A"})
        {
            var go=GameObject.Find(name);
            if(go==null){sb.AppendLine(name+" NULL");continue;}
            var col=go.GetComponent<Collider2D>();
            sb.AppendLine(name+" col="+(col==null?"none":col.GetType().Name+" "+col.bounds.min.ToString("F2")+".."+col.bounds.max.ToString("F2")));
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/refsizes.txt",sb.ToString()); Debug.Log("done");
    }
}

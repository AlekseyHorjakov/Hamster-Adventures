using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class InvisDump2
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        var sprites=Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(sr=>sr!=null && sr.sprite!=null).ToArray();
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach(var c in t.GetComponents<Collider2D>())
            {
                if(c.isTrigger) continue;
                var b=c.bounds;
                if(b.max.x<196f || b.min.x>400f) continue;
                float best=999f; string bestName="-"; float bestTop=0f;
                foreach(var sr in sprites)
                {
                    float d=Vector2.Distance(new Vector2(sr.bounds.center.x, sr.bounds.max.y), new Vector2(b.center.x, b.max.y));
                    if(d<best){best=d;bestName=sr.gameObject.name;bestTop=sr.bounds.max.y;}
                }
                bool suspicious = best>1.0f || Mathf.Abs(bestTop-b.max.y)>0.6f;
                if(!suspicious) continue;
                sb.AppendLine(t.name+" "+c.GetType().Name+" top="+b.max.y.ToString("F1")+" x="+b.min.x.ToString("F1")+".."+b.max.x.ToString("F1")+
                  " nearest="+bestName+" d="+best.ToString("F2")+" spriteTop="+bestTop.ToString("F1"));
            }
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/invis2.txt",sb.ToString()); Debug.Log("done");
    }
}

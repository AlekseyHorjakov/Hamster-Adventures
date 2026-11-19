using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class InvisDump
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb=new StringBuilder();
        var allSprites=Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(sr=>sr!=null && sr.sprite!=null);
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach(var c in t.GetComponents<Collider2D>())
            {
                if(c.isTrigger) continue;
                var b=c.bounds;
                if(b.max.x<330f || b.min.x>400f) continue;
                // ближайший видимый спрайт к коллайдеру
                float best=999f; string bestName="-";
                foreach(var sr in allSprites)
                {
                    float d=Vector2.Distance(new Vector2(sr.bounds.center.x, sr.bounds.max.y), new Vector2(b.center.x, b.max.y));
                    if(d<best){best=d;bestName=sr.gameObject.name+" ("+sr.sprite.name+")";}
                }
                sb.AppendLine(t.name+" "+c.GetType().Name+" bounds="+b.min.ToString("F1")+".."+b.max.ToString("F1")+
                  " nearestSprite="+bestName+" dist="+best.ToString("F2")+" ownSprite="+(t.GetComponentInChildren<SpriteRenderer>(true)!=null));
            }
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/invis.txt",sb.ToString()); Debug.Log("done");
    }
}


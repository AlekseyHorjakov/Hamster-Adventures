using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CaptureColliderBaseline
{
    [MenuItem("Tools/Hamster/Capture Zone 1 Collider Baseline")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();
        sb.AppendLine("ZONE1_COLLIDER_BASELINE");
        sb.AppendLine("scene=" + scene.path);
        foreach (GameObject root in scene.GetRootGameObjects().OrderBy(r => r.name))
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true).OrderBy(x => x.name))
            {
                GameObject go = t.gameObject;
                Collider2D[] cols = go.GetComponents<Collider2D>();
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (cols.Length == 0 && sr == null)
                    continue;

                string path = GetPath(t);
                sb.AppendLine("OBJECT|" + path);
                sb.AppendLine("  active=" + go.activeInHierarchy + " tag=" + go.tag);
                sb.AppendLine("  pos=" + F(t.position) + " localScale=" + F(t.localScale));

                if (sr != null)
                {
                    sb.AppendLine("  sprite=" + (sr.sprite != null ? sr.sprite.name : "NULL") +
                        " layer=" + sr.sortingLayerName + "/" + sr.sortingOrder +
                        " bounds=" + B(sr.bounds) + " color=" + sr.color);
                }

                foreach (Collider2D c in cols)
                {
                    BoxCollider2D box = c as BoxCollider2D;
                    string extra = box != null
                        ? " offset=" + F(box.offset) + " size=" + F(box.size) + " usedByEffector=" + box.usedByEffector
                        : string.Empty;
                    sb.AppendLine("  collider=" + c.GetType().Name + " enabled=" + c.enabled +
                        " trigger=" + c.isTrigger + extra + " bounds=" + B(c.bounds));
                }

                Component breakable = go.GetComponent("BreakableProp");
                if (breakable != null)
                    sb.AppendLine("  interactive=BreakableProp");
                Component pickup = go.GetComponent("RewardPickup");
                if (pickup != null)
                    sb.AppendLine("  interactive=RewardPickup");
                Component checkpoint = go.GetComponent("RoomCheckpoint");
                if (checkpoint != null)
                    sb.AppendLine("  interactive=RoomCheckpoint");
            }
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/zone1_collider_baseline.txt", sb.ToString());
        System.IO.File.WriteAllText("D:/dev/Hamster-Adventures/Hamster Adventures/LEVEL_1_COLLIDER_BASELINE.txt", sb.ToString());
        Debug.Log("[ColliderBaseline] captured");
    }

    private static string GetPath(Transform t)
    {
        var parts = new List<string>();
        while (t != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static string F(Vector2 v) => "(" + v.x.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture) + "," + v.y.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture) + ")";
    private static string F(Vector3 v) => "(" + v.x.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture) + "," + v.y.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture) + "," + v.z.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture) + ")";
    private static string B(Bounds b) => F(b.min) + ".." + F(b.max);
}


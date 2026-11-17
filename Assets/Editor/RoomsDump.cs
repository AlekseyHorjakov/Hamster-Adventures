using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class RoomsDump
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();
        var rooms = GameObject.Find("Rooms");
        if (rooms == null) { System.IO.File.WriteAllText("C:/Windows/Temp/rooms.txt","NO ROOMS"); return; }
        foreach (Transform t in rooms.transform)
        {
            sb.AppendLine("ROOM " + t.name + " pos=" + t.position.ToString("F1"));
            foreach (var mb in t.GetComponents<MonoBehaviour>())
            {
                var so = new SerializedObject(mb);
                string extra = "";
                foreach (var name in new[]{"room","level","enemies","gates","encounterName","activateOnStart","spawnPoint","roomTrigger"})
                {
                    var prop = so.FindProperty(name);
                    if (prop == null) continue;
                    if (prop.isArray)
                    {
                        string items = "";
                        for (int i = 0; i < prop.arraySize; i++) items += prop.GetArrayElementAtIndex(i).objectReferenceValue?.name + ",";
                        extra += name + "[" + items + "] ";
                    }
                    else if (prop.propertyType == SerializedPropertyType.ObjectReference)
                        extra += name + "=" + (prop.objectReferenceValue?.name ?? "null") + " ";
                }
                sb.AppendLine("  " + mb.GetType().Name + " " + extra);
            }
            foreach (Transform child in t)
                sb.AppendLine("  child " + child.name + " pos=" + child.position.ToString("F1") + " comps=" + string.Join(',', child.GetComponents<Component>().Select(c => c.GetType().Name)));
        }
        System.IO.File.WriteAllText("C:/Windows/Temp/rooms.txt", sb.ToString()); Debug.Log("done");
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Эталон Зоны II, этап 1: комнаты 06-07. Меню: Tools/Hamster/Проверить эталон Зоны II.</summary>
public static class Zone2ReferenceGuard
{
    private static readonly (string name, float top, float left, float right)[] Expected =
    {
        ("LakeLedge", 2f, 202f, 207f),
        ("LakeStep1", 1.4f, 207f, 209.5f),
        ("LakeStep2", 0.75f, 209.5f, 212f),
        ("LakeBeach", 0.1f, 212f, 238f),
        ("Stone_A", 0.2f, 241f, 245f),
        ("Stone_B", 0.8f, 249f, 253f),
        ("Stone_C", 1.4f, 257f, 261f),
        ("LakeDock", 2f, 266f, 274f),
        ("Island_1", 2.5f, 274f, 282f),
        ("Island_2", 3.2f, 286f, 294f),
        ("Island_3", 4f, 298f, 306f),
        ("Island_Final", 2f, 309f, 314f),
        ("CrossStart", 2f, 314f, 321f),
        ("CrossPlatform_1", 2.8f, 325f, 330f),
        ("CrossPlatform_2", 3.6f, 334f, 339f),
        ("CrossPlatform_3", 4.4f, 343f, 348f),
        ("CrossExit", 4f, 348f, 352f),
        ("FallBase", 4f, 352f, 358f),
        ("FallLedge_1", 5f, 360f, 364f),
        ("FallLedge_2", 6f, 367f, 371f),
        ("FallLedge_3", 7f, 374f, 378f),
        ("FallTop", 8f, 380f, 390f)
    };

    private static readonly (string asset, float surfaceY)[] Rooms = { ("Room_06", 0.1f), ("Room_07", 0.6f), ("Room_08", 2.5f), ("Room_09", 3f), ("Room_10", 5f) };

    [MenuItem("Tools/Hamster/Проверить эталон Зоны II")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        int errors = 0;
        foreach (var entry in Expected)
        {
            GameObject go = GameObject.Find(entry.name);
            BoxCollider2D box = go == null ? null : go.GetComponent<BoxCollider2D>();
            if (box == null) { Debug.LogError("[Zone2Guard] НЕТ: " + entry.name); errors++; continue; }
            if (Mathf.Abs(box.bounds.max.y - entry.top) > 0.02f ||
                Mathf.Abs(box.bounds.min.x - entry.left) > 0.02f ||
                Mathf.Abs(box.bounds.max.x - entry.right) > 0.02f)
            {
                Debug.LogError("[Zone2Guard] " + entry.name + " top=" + box.bounds.max.y.ToString("F2") +
                               " x=" + box.bounds.min.x.ToString("F1") + ".." + box.bounds.max.x.ToString("F1"));
                errors++;
            }
        }

        foreach (var room in Rooms)
        {
            RoomDefinition def = AssetDatabase.LoadAssetAtPath<RoomDefinition>(
                "Assets/Level/Rooms/Zone2/" + room.asset + ".asset");
            if (def == null || Mathf.Abs(def.SurfaceY - room.surfaceY) > 0.01f)
            {
                Debug.LogError("[Zone2Guard] " + room.asset + " surfaceY=" + (def == null ? "MISSING" : def.SurfaceY.ToString("F2")));
                errors++;
            }
        }

        Debug.Log(errors == 0 ? "[Zone2Guard] PASS" : "[Zone2Guard] FAIL: " + errors);
    }
}


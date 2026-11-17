using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Эталон Зоны I (комнаты 01-05). Фиксирует ключевые коллайдеры и высоты комнат,
/// чтобы будущие правки арта не сдвинули проходимость и не сломали прыжки.
/// Меню: Tools/Hamster/Проверить эталон Зоны I.
/// </summary>
public static class Zone1ReferenceGuard
{
    private static readonly (string name, float top, float left, float right, bool effector)[] Expected =
    {
        ("CampGround", 0f, -52f, 48f, false),
        ("PathGround_02", 0f, 48f, 84f, false),
        ("GladeGround_A", 0f, 84f, 102f, false),
        ("GladeGround_B", 1.5f, 108f, 122f, false),
        ("TrainingGround_A", 2f, 121.6f, 142f, false),
        ("TrainingGround_B", 2f, 149f, 160f, false),
        ("BridgeEntryGround", 2f, 160f, 168f, false),
        ("BridgeExitGround", 2f, 192f, 202f, false),
        ("GladeStep_A", 0.75f, 98f, 103f, true),
        ("GladeStep_B", 1.25f, 105f, 109f, true),
        ("TrainingStep_A", 3.2f, 138f, 143f, true),
        ("TrainingStep_B", 3.5f, 147f, 152f, true),
        ("BridgeSection_01", 2f, 168f, 173f, true),
        ("BridgeSection_02", 2.6f, 176f, 180f, true),
        ("BridgeSection_03", 3.2f, 183f, 188f, true),
        ("BridgeSection_04", 2.6f, 190f, 194f, true),
        ("GladePitVisual_Floor", -1.58f, 101.5f, 108.5f, false),
        ("TrainingPitVisual_Floor", 0.27f, 141.5f, 149.5f, false),
        ("GladePitVisual_WallL", 0.2f, 101.9f, 102.2f, false),
        ("GladePitVisual_WallR", 0.2f, 107.8f, 108.1f, false),
        ("TrainingPitVisual_WallL", 2.2f, 141.9f, 142.2f, false),
        ("TrainingPitVisual_WallR", 2.2f, 148.8f, 149.1f, false)
    };

    private static readonly (string asset, float surfaceY)[] Rooms =
    {
        ("Room_01", 0f), ("Room_02", 0f), ("Room_03", 0.75f), ("Room_04", 2f), ("Room_05", 2f)
    };

    [MenuItem("Tools/Hamster/Проверить эталон Зоны I")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        int errors = 0;
        foreach (var entry in Expected)
        {
            GameObject go = GameObject.Find(entry.name);
            BoxCollider2D box = go == null ? null : go.GetComponent<BoxCollider2D>();
            if (box == null)
            {
                Debug.LogError("[RefGuard] НЕТ КОЛЛАЙДЕРА: " + entry.name);
                errors++;
                continue;
            }

            bool ok = Mathf.Abs(box.bounds.max.y - entry.top) < 0.02f &&
                      Mathf.Abs(box.bounds.min.x - entry.left) < 0.02f &&
                      Mathf.Abs(box.bounds.max.x - entry.right) < 0.02f &&
                      box.usedByEffector == entry.effector;
            if (!ok)
            {
                Debug.LogError("[RefGuard] " + entry.name + " top=" + box.bounds.max.y.ToString("F2") +
                               " left=" + box.bounds.min.x.ToString("F2") + " right=" + box.bounds.max.x.ToString("F2") +
                               " effector=" + box.usedByEffector +
                               " ожидалось top=" + entry.top + " left=" + entry.left + " right=" + entry.right);
                errors++;
            }
        }

        foreach (var room in Rooms)
        {
            RoomDefinition def = AssetDatabase.LoadAssetAtPath<RoomDefinition>("Assets/Level/Rooms/Zone1/" + room.asset + ".asset");
            if (def == null || Mathf.Abs(def.SurfaceY - room.surfaceY) > 0.01f)
            {
                Debug.LogError("[RefGuard] " + room.asset + " surfaceY=" + (def == null ? "MISSING" : def.SurfaceY.ToString("F2")) +
                               " ожидалось " + room.surfaceY);
                errors++;
            }
        }

        Debug.Log(errors == 0 ? "[RefGuard] PASS — эталон Зоны I совпадает" : "[RefGuard] FAIL, расхождений: " + errors);
    }
}

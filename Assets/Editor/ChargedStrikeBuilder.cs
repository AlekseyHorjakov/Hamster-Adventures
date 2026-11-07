using UnityEditor;
using UnityEngine;

/// <summary>
/// Включает готовый клип рывка и удара заряженного приёма.
/// Кадры 3–6 старого листа уже собраны в Hamster_ChargedPunch/HG_ChargedPunch:
/// 0 с — срыв, 0,1 — полёт, 0,22 — удар (ровно конец рывка), 0,42 — восстановление.
/// </summary>
public static class ChargedStrikeBuilder
{
    [MenuItem("Tools/Hamster/Enable Charged Punch")]
    public static void Apply()
    {
        WireCharacter("Assets/Prefabs/Hamster.prefab");
        WireCharacter("Assets/Prefabs/HamsterGirl.prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ChargedPunch] Рывок и удар включены из старых кадров");
    }

    private static void WireCharacter(string prefabPath)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        AttackController controller = contents.GetComponent<AttackController>();

        if (controller == null)
        {
            Debug.LogWarning("[ChargedPunch] В " + prefabPath + " нет AttackController");
            PrefabUtility.UnloadPrefabContents(contents);
            return;
        }

        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty state = serialized.FindProperty("chargedStateName");
        if (state != null)
            state.stringValue = "ChargedPunch";

        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Создаёт Assets/Input/PlayerControls.inputactions с картами Hamster
/// (A/D/W/F/E/R/Q) и HamsterGirl (H/K/U/L/O/P/I), затем добавляет PlayerInputReader
/// на оба префаба и связывает его с HamsterMovement.
///
/// Запуск: Tools/Hamster/Migrate Input или batchmode
/// -executeMethod InputMigrationBuilder.BuildAll
/// </summary>
public static class InputMigrationBuilder
{
    private const string Folder = "Assets/Input";
    private const string AssetPath = Folder + "/PlayerControls.inputactions";

    [MenuItem("Tools/Hamster/Migrate Input")]
    public static void BuildAll()
    {
        InputActionAsset asset = BuildAsset();
        if (asset == null)
        {
            Debug.LogError("[Input] Не удалось создать InputActionAsset");
            return;
        }

        Wire("Assets/Prefabs/Hamster.prefab", asset, "Hamster");
        Wire("Assets/Prefabs/HamsterGirl.prefab", asset, "HamsterGirl");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Input] Готово: PlayerControls.inputactions, карты Hamster и HamsterGirl");
    }

    private static InputActionAsset BuildAsset()
    {
        InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.name = "PlayerControls";

        AddMap(asset, "Hamster", "<Keyboard>/a", "<Keyboard>/d", "<Keyboard>/w", "<Keyboard>/f", "<Keyboard>/e", "<Keyboard>/r", "<Keyboard>/q");
        AddMap(asset, "HamsterGirl", "<Keyboard>/h", "<Keyboard>/k", "<Keyboard>/u", "<Keyboard>/l", "<Keyboard>/o", "<Keyboard>/p", "<Keyboard>/i");

        Directory.CreateDirectory(Folder);
        File.WriteAllText(AssetPath, asset.ToJson());
        AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);

        InputActionAsset loaded = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
        if (loaded == null)
            Debug.LogError("[Input] Не импортировался " + AssetPath);

        return loaded;
    }

    private static void AddMap(InputActionAsset asset, string mapName,
        string left, string right, string jump, string attack, string interact, string throwKey, string superKey)
    {
        InputActionMap map = asset.AddActionMap(mapName);

        InputAction move = map.AddAction("Move", InputActionType.Value);
        move.AddCompositeBinding("1DAxis")
            .With("Negative", left)
            .With("Positive", right);

        map.AddAction("Jump", InputActionType.Button).AddBinding(jump);
        map.AddAction("Attack", InputActionType.Button).AddBinding(attack);
        map.AddAction("Interact", InputActionType.Button).AddBinding(interact);
        map.AddAction("Throw", InputActionType.Button).AddBinding(throwKey);
        map.AddAction("Super", InputActionType.Button).AddBinding(superKey);
        map.AddAction("Any", InputActionType.Button).AddBinding("<Keyboard>/anyKey");
    }

    private static void Wire(string prefabPath, InputActionAsset asset, string mapName)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        HamsterMovement movement = contents.GetComponent<HamsterMovement>();

        if (movement == null)
        {
            Debug.LogError("[Input] Нет HamsterMovement в " + prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            return;
        }

        PlayerInputReader reader = contents.GetComponent<PlayerInputReader>();
        if (reader == null)
            reader = contents.AddComponent<PlayerInputReader>();

        SerializedObject readerObject = new SerializedObject(reader);
        readerObject.FindProperty("actions").objectReferenceValue = asset;
        readerObject.FindProperty("mapName").stringValue = mapName;
        readerObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject movementObject = new SerializedObject(movement);
        movementObject.FindProperty("input").objectReferenceValue = reader;
        movementObject.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}

using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Собирает вращающиеся звёздочки над потерявшим сознание героем.</summary>
public static class DizzyStarsBuilder
{
    private const string Art = "Assets/Art/VFX/DizzyStars";
    private const string Anim = "Assets/Animations/VFX";
    private const string ClipPath = Anim + "/DizzyStars.anim";
    private const string ControllerPath = Anim + "/DizzyStars.controller";
    private const string PrefabPath = "Assets/VFX/Prefabs/DizzyStars.prefab";

    [MenuItem("Tools/Hamster/Build Dizzy Stars")]
    public static void BuildAll()
    {
        EnsureFolder("Assets/Animations/VFX");
        EnsureFolder("Assets/VFX/Prefabs");
        Sprite[] frames = new Sprite[8];
        for (int i = 0; i < 8; i++)
        {
            string path = Art + "/DizzyStars_" + (i + 1).ToString("00") + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 200f;
            TextureImporterSettings settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            imp.SetTextureSettings(settings);
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            frames[i] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, ClipPath); }
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[8];
        for (int i = 0; i < 8; i++) keys[i] = new ObjectReferenceKeyframe { time = 0.08f * i, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings cs = AnimationUtility.GetAnimationClipSettings(clip);
        cs.stopTime = 0.64f; cs.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, cs);
        EditorUtility.SetDirty(clip);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorState state = controller.AddMotion(clip, 0); state.name = "DizzyStars";
        controller.layers[0].stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);

        GameObject go = new GameObject("DizzyStars");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0]; sr.sortingLayerName = "FX"; sr.sortingOrder = 20;
        Animator animator = go.AddComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath); Object.DestroyImmediate(go);

        AssignTo("Assets/Prefabs/Hamster.prefab", prefab);
        AssignTo("Assets/Prefabs/HamsterGirl.prefab", prefab);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("[DizzyStars] Готово: 8 кадров, 0,64 с");
    }

    private static void AssignTo(string prefabPath, GameObject stars)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        PlayerCombatState state = contents.GetComponent<PlayerCombatState>();
        if (state == null) { PrefabUtility.UnloadPrefabContents(contents); return; }
        SerializedObject so = new SerializedObject(state);
        so.FindProperty("dizzyStarsPrefab").objectReferenceValue = stars;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, name);
    }
}

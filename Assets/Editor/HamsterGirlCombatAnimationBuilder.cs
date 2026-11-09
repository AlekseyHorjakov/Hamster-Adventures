using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Подключает Хомячихе анимации урона, потери сознания и смерти.</summary>
public static class HamsterGirlCombatAnimationBuilder
{
    private const string ArtRoot = "Assets/Art/HamsterGirl/Combat";
    private const string AnimRoot = "Assets/Animations/HamsterGirl";
    private const string ControllerPath = AnimRoot + "/HamsterGirl.controller";
    private const string PrefabPath = "Assets/Prefabs/HamsterGirl.prefab";
    private const float Ppu = 98f;
    private const int Count = 8;

    [MenuItem("Tools/Hamster/Build Hamster Girl Combat Animations")]
    public static void BuildAll()
    {
        Sprite[] hurt = Import("Hurt", "HamsterGirl_Hurt_");
        Sprite[] downed = Import("Downed", "HamsterGirl_Downed_");
        Sprite[] death = Import("Death", "HamsterGirl_Death_");

        AnimationClip hurtClip = BuildClip(AnimRoot + "/HG_Hurt.anim", hurt, 0.035f);
        AnimationClip downedClip = BuildClip(AnimRoot + "/HG_Downed.anim", downed, 0.08f);
        AnimationClip deathClip = BuildClip(AnimRoot + "/HG_Death.anim", death, 0.09f);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) { Debug.LogError("[HGCombat] Нет контроллера"); return; }

        AddState(controller, "Hurt", hurtClip);
        AddState(controller, "Downed", downedClip);
        AddState(controller, "Death", deathClip);
        EditorUtility.SetDirty(controller);

        AddComponent();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HGCombat] Готово: Hurt 0,28 с, Downed 0,64 с, Death 0,72 с");
    }

    private static Sprite[] Import(string folder, string prefix)
    {
        Sprite[] result = new Sprite[Count];
        for (int i = 0; i < Count; i++)
        {
            string path = ArtRoot + "/" + folder + "/" + prefix + (i + 1).ToString("00") + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError("[HGCombat] Нет " + path); continue; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Ppu;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            result[i] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }
        return result;
    }

    private static AnimationClip BuildClip(string path, Sprite[] frames, float frameSeconds)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("Visual", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = frameSeconds * i, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = frameSeconds * frames.Length;
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void AddState(AnimatorController controller, string name, AnimationClip clip)
    {
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
            if (child.state.name == name) { child.state.motion = clip; return; }
        controller.AddMotion(clip, 0).name = name;
    }

    private static void AddComponent()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        PlayerCombatState state = contents.GetComponent<PlayerCombatState>();
        if (state == null) state = contents.AddComponent<PlayerCombatState>();
        SerializedObject so = new SerializedObject(state);
        so.FindProperty("animator").objectReferenceValue = contents.GetComponent<Animator>();
        so.FindProperty("movement").objectReferenceValue = contents.GetComponent<HamsterMovement>();
        so.FindProperty("attack").objectReferenceValue = contents.GetComponent<AttackController>();
        so.FindProperty("body").objectReferenceValue = contents.GetComponent<Rigidbody2D>();
        so.FindProperty("idleStateName").stringValue = "HGIdle";
        so.FindProperty("hurtStateName").stringValue = "Hurt";
        so.FindProperty("downedStateName").stringValue = "Downed";
        so.FindProperty("deathStateName").stringValue = "Death";
        so.FindProperty("hurtDuration").floatValue = 0.28f;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}

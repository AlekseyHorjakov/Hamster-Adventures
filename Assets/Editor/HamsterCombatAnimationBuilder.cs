using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Подключает к Хомяку кадры урона, потери сознания и смерти.
/// Меню: Tools -> Hamster -> Build Hamster Combat Animations.
/// </summary>
public static class HamsterCombatAnimationBuilder
{
    private const string ArtRoot = "Assets/Art/Hamster/Combat";
    private const string AnimRoot = "Assets/Animations/Hamster";
    private const string ControllerPath = AnimRoot + "/Hamster_Controller.controller";
    private const string PrefabPath = "Assets/Prefabs/Hamster.prefab";
    private const float Ppu = 93.9f;
    private const int Count = 8;

    [MenuItem("Tools/Hamster/Build Hamster Combat Animations")]
    public static void BuildAll()
    {
        Sprite[] hurt = Import("Hurt", "Hamster_Hurt_");
        Sprite[] downed = Import("Downed", "Hamster_Downed_");
        Sprite[] death = Import("Death", "Hamster_Death_");

        AnimationClip hurtClip = BuildClip(AnimRoot + "/Hamster_Hurt.anim", hurt, 0.06f, false);
        AnimationClip downedClip = BuildClip(AnimRoot + "/Hamster_Downed.anim", downed, 0.08f, false);
        AnimationClip deathClip = BuildClip(AnimRoot + "/Hamster_Death.anim", death, 0.09f, false);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[HamsterCombat] Нет контроллера " + ControllerPath);
            return;
        }

        AddState(controller, "Hurt", hurtClip);
        AddState(controller, "Downed", downedClip);
        AddState(controller, "Death", deathClip);
        EditorUtility.SetDirty(controller);

        AddComponent();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HamsterCombat] Готово: Hurt 0,48 с, Downed 0,64 с, Death 0,72 с");
    }

    private static Sprite[] Import(string folder, string prefix)
    {
        Sprite[] result = new Sprite[Count];
        for (int i = 0; i < Count; i++)
        {
            string path = ArtRoot + "/" + folder + "/" + prefix + (i + 1).ToString("00") + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) { Debug.LogError("[HamsterCombat] Нет " + path); continue; }

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

    private static AnimationClip BuildClip(string path, Sprite[] frames, float frameSeconds, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("Visual", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = frameSeconds * i, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = frameSeconds * frames.Length;
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void AddState(AnimatorController controller, string name, AnimationClip clip)
    {
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
        {
            if (child.state.name == name)
            {
                child.state.motion = clip;
                return;
            }
        }
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
        so.FindProperty("idleStateName").stringValue = "Idle 0";
        so.FindProperty("hurtStateName").stringValue = "Hurt";
        so.FindProperty("downedStateName").stringValue = "Downed";
        so.FindProperty("deathStateName").stringValue = "Death";
        so.FindProperty("hurtDuration").floatValue = 0.48f;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}

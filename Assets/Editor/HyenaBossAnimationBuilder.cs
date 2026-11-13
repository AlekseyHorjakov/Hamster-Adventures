using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Нарезает лист босса и собирает анимации Idle/Run/Attack/Hurt/Death.</summary>
public static class HyenaBossAnimationBuilder
{
    private const string FramesFolder = "Assets/Art/Enemies/HyenaBoss/Frames";
    private const string AnimationsFolder = "Assets/Animations/Enemies/HyenaBoss";
    private const string ControllerPath = AnimationsFolder + "/HyenaBoss.controller";

    [MenuItem("Tools/Hamster/Build Boss Animations")]
    public static void Build()
    {
        ConfigureFrameImporters();
        EnsureFolder(AnimationsFolder);

        Sprite[] idle = LoadFrames("Idle");
        Sprite[] run = LoadFrames("Run");
        Sprite[] attack = LoadFrames("Attack");
        Sprite[] hurt = LoadFrames("Hurt");
        Sprite[] death = LoadFrames("Death");

        AnimationClip idleClip = CreateClip("HyenaBoss_Idle", idle, true, 8f);
        AnimationClip runClip = CreateClip("HyenaBoss_Run", run, true, 10f);
        AnimationClip attackClip = CreateClip("HyenaBoss_Attack", attack, false, 12f);
        AnimationClip hurtClip = CreateClip("HyenaBoss_Hurt", hurt, false, 12f);
        AnimationClip deathClip = CreateClip("HyenaBoss_Death", death, false, 10f);

        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        foreach (ChildAnimatorState state in machine.states.ToArray())
            machine.RemoveState(state.state);

        AnimatorState idleState = machine.AddState("Idle");
        idleState.motion = idleClip;
        machine.defaultState = idleState;

        AddState(machine, "Run", runClip);
        AddState(machine, "Attack", attackClip);
        AddState(machine, "Hurt", hurtClip);
        AddState(machine, "Death", deathClip);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BossAnim] Готово: 30 кадров, 5 состояний.");
    }

    private static void AddState(AnimatorStateMachine machine, string name, Motion motion)
    {
        AnimatorState state = machine.AddState(name);
        state.motion = motion;
    }

    private static AnimationClip CreateClip(string name, Sprite[] frames, bool loop, float fps)
    {
        string path = AnimationsFolder + "/" + name + ".anim";
        AssetDatabase.DeleteAsset(path);

        AnimationClip clip = new AnimationClip { frameRate = fps };
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "Visual",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / fps,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static Sprite[] LoadFrames(string state)
    {
        string[] files = AssetDatabase.FindAssets("HyenaBoss_" + state + "_ t:Texture2D", new[] { FramesFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .ToArray();
        return files.Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path)).Where(s => s != null).ToArray();
    }

    private static void ConfigureFrameImporters()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { FramesFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}

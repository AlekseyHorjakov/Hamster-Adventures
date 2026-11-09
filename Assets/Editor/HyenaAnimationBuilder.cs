using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Собирает анимации первой гиены из листа 6×3:
/// ряд 1 — ходьба, ряд 2 — получение урона, ряд 3 — смерть.
/// Кадры уже нарезаны и посажены на общий якорь по ступням.
/// </summary>
public static class HyenaAnimationBuilder
{
    private const string FrameRoot = "Assets/Art/Enemies/Hyena";
    private const string AnimationRoot = "Assets/Animations/Enemies/Hyena";
    private const string ControllerPath = AnimationRoot + "/Hyena.controller";
    private const string Ppu = "130";

    private const string IdlePath = AnimationRoot + "/Hyena_Idle.anim";
    private const string WalkPath = AnimationRoot + "/Hyena_Walk.anim";
    private const string HurtPath = AnimationRoot + "/Hyena_Hurt.anim";
    private const string DeathPath = AnimationRoot + "/Hyena_Death.anim";

    private const string IdleState = "Idle";
    private const string WalkState = "Walk";
    private const string HurtState = "Hurt";
    private const string DeathState = "Death";

    [MenuItem("Tools/Hamster/Build Hyena Animations")]
    public static void BuildAll()
    {
        EnsureFolder("Assets/Animations/Enemies/Hyena");

        Sprite[] walk = ImportFrames("Walk", "Hyena_Walk_");
        Sprite[] hurt = ImportFrames("Hurt", "Hyena_Hurt_");
        Sprite[] death = ImportFrames("Death", "Hyena_Death_");

        if (walk.Length != 6 || hurt.Length != 6 || death.Length != 6)
        {
            Debug.LogError($"[HyenaAnim] Не хватает кадров: walk={walk.Length}, hurt={hurt.Length}, death={death.Length}");
            return;
        }

        AnimationClip idle = BuildClip(IdlePath, new[] { walk[0] }, 1f, true);
        AnimationClip walkClip = BuildClip(WalkPath, walk, 0.1f, true);
        AnimationClip hurtClip = BuildClip(HurtPath, hurt, 0.08f, false);
        AnimationClip deathClip = BuildClip(DeathPath, death, 0.12f, false);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        AnimatorState idleState = AddOrUpdateState(controller, IdleState, idle);
        AddOrUpdateState(controller, WalkState, walkClip);
        AddOrUpdateState(controller, HurtState, hurtClip);
        AddOrUpdateState(controller, DeathState, deathClip);
        controller.layers[0].stateMachine.defaultState = idleState;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[HyenaAnim] Готово: Idle, Walk 0,6 с, Hurt 0,48 с, Death 0,72 с");
    }

    private static Sprite[] ImportFrames(string folder, string prefix)
    {
        Sprite[] frames = new Sprite[6];
        string pathRoot = FrameRoot + "/" + folder;

        for (int i = 0; i < frames.Length; i++)
        {
            string path = pathRoot + "/" + prefix + (i + 1).ToString("00") + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[HyenaAnim] Не найден " + path);
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = float.Parse(Ppu, System.Globalization.CultureInfo.InvariantCulture);

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            frames[i] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (frames[i] == null)
                Debug.LogError("[HyenaAnim] Не загрузился спрайт " + path);
        }

        return frames;
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
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = frameSeconds * i,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = frameSeconds * frames.Length;
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorState AddOrUpdateState(AnimatorController controller, string stateName, AnimationClip clip)
    {
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
        {
            if (child.state.name == stateName)
            {
                child.state.motion = clip;
                EditorUtility.SetDirty(controller);
                return child.state;
            }
        }

        AnimatorState state = controller.AddMotion(clip, 0);
        state.name = stateName;
        EditorUtility.SetDirty(controller);
        return state;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}

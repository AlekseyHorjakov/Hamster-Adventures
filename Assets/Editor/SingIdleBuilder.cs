using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Собирает особое бездействие Хомячихи: вступление с доставанием микрофона
/// и зацикленную часть с песней и баблом.
///
/// Схема:
///   SingIntro — кадры 1–6, играется один раз (достаёт микрофон);
///   SingIdle  — кадры 7–16, начинается со стука по микрофону, доходит до
///               полного «СОСИСОЧКА» и держит последний кадр 1 секунду,
///               затем зацикливается.
///
/// Запуск: Tools/Hamster/Build Girl Sing Idle или
/// Unity.exe -batchmode -quit -executeMethod SingIdleBuilder.BuildAll
/// </summary>
public static class SingIdleBuilder
{
    private const string FrameFolder = "Assets/Art/HamsterGirl/SingIdle";
    private const string FramePrefix = "Girl_SingIdle_";
    private const int FrameCount = 16;
    private const int IntroCount = 6;
    private const int LoopStartIndex = 6;
    private const float HoldSeconds = 1f;

    // 0,12 с на кадр — это 8,3 fps. В исходной гифке было 0,45 с (2,2 fps),
    // из-за чего анимация читалась как слайдшоу.
    private const float FrameSeconds = 0.12f;
    private const float Ppu = 98f;

    private const string IntroPath = "Assets/Animations/HamsterGirl/SingIntro.anim";
    private const string LoopPath = "Assets/Animations/HamsterGirl/SingIdle.anim";
    private const string ControllerPath = "Assets/Animations/HamsterGirl/HamsterGirl.controller";
    private const string PrefabPath = "Assets/Prefabs/HamsterGirl.prefab";
    private const string IntroState = "SingIntro";
    private const string LoopState = "SingIdle";

    [MenuItem("Tools/Hamster/Build Girl Sing Idle")]
    public static void BuildAll()
    {
        Sprite[] frames = ImportFrames();
        if (frames.Length != FrameCount)
        {
            Debug.LogError("[SingIdle] Не хватает кадров: " + frames.Length + " из " + FrameCount);
            return;
        }

        AnimationClip intro = BuildClip(IntroPath, frames.Take(IntroCount).ToArray(), false, 0f);
        AnimationClip loop = BuildClip(LoopPath, frames.Skip(LoopStartIndex).ToArray(), true, HoldSeconds);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[SingIdle] Нет контроллера " + ControllerPath);
            return;
        }

        AddState(controller, IntroState, intro);
        AddState(controller, LoopState, loop);
        AddComponent();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        float introLength = IntroCount * FrameSeconds;
        float loopLength = (FrameCount - LoopStartIndex - 1) * FrameSeconds + HoldSeconds;
        Debug.Log("[SingIdle] Готово: вступление " + introLength + " с, цикл " + loopLength + " с");
    }

    private static Sprite[] ImportFrames()
    {
        Sprite[] frames = new Sprite[FrameCount];

        for (int i = 0; i < FrameCount; i++)
        {
            string path = FrameFolder + "/" + FramePrefix + (i + 1).ToString("00") + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Ppu;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            frames[i] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (frames[i] == null)
                Debug.LogError("[SingIdle] Не загрузился спрайт " + path);
        }

        return frames;
    }

    private static AnimationClip BuildClip(string path, Sprite[] frames, bool loop, float holdSeconds)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.frameRate = 60f;

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("Visual", typeof(SpriteRenderer), "m_Sprite");

        bool hasHold = holdSeconds > 0f;
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length + (hasHold ? 1 : 0)];

        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = FrameSeconds * i,
                value = frames[i]
            };
        }

        float stopTime = FrameSeconds * frames.Length;

        if (hasHold)
        {
            float lastKeyTime = FrameSeconds * (frames.Length - 1) + holdSeconds;
            keys[keys.Length - 1] = new ObjectReferenceKeyframe
            {
                time = lastKeyTime,
                value = frames[frames.Length - 1]
            };
            stopTime = lastKeyTime;
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = stopTime;
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void AddState(AnimatorController controller, string stateName, AnimationClip clip)
    {
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
        {
            if (child.state.name == stateName)
            {
                child.state.motion = clip;
                EditorUtility.SetDirty(controller);
                return;
            }
        }

        AnimatorState state = controller.AddMotion(clip, 0);
        state.name = stateName;
        EditorUtility.SetDirty(controller);
    }

    private static void AddComponent()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);

        IdleSpecialController idle = contents.GetComponent<IdleSpecialController>();
        if (idle == null)
            idle = contents.AddComponent<IdleSpecialController>();

        SerializedObject serialized = new SerializedObject(idle);
        serialized.FindProperty("movement").objectReferenceValue = contents.GetComponent<HamsterMovement>();
        serialized.FindProperty("animator").objectReferenceValue = contents.GetComponent<Animator>();
        serialized.FindProperty("idleState").stringValue = "HGIdle";
        serialized.FindProperty("introState").stringValue = IntroState;
        serialized.FindProperty("specialState").stringValue = LoopState;
        serialized.FindProperty("idleDelay").floatValue = 5f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}

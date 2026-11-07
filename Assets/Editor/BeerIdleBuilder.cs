using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Особое бездействие Хомяка: достаёт бутылку, открывает, затем цикл —
/// пьёт, рыгает, повторяет.
///
/// Вступление: кадры 1–4, по 0,12 с, один раз.
/// Цикл: кадры 5–9; кадр с зелёной отрыжкой держится 1 секунду, затем
/// последний кадр и повтор.
///
/// Запуск: Tools/Hamster/Build Hamster Beer Idle или
/// Unity.exe -batchmode -quit -executeMethod BeerIdleBuilder.BuildAll
/// </summary>
public static class BeerIdleBuilder
{
    private const string FrameFolder = "Assets/Art/Hamster/BeerIdle";
    private const string FramePrefix = "Hamster_BeerIdle_";
    private const int FrameCount = 9;
    private const int IntroCount = 4;
    private const int LoopStartIndex = 4;
    private const float FrameSeconds = 0.12f;
    private const float BurpHoldSeconds = 1f;
    private const float Ppu = 93.9f;

    private const string IntroPath = "Assets/Animations/Hamster/BeerIntro.anim";
    private const string LoopPath = "Assets/Animations/Hamster/BeerIdle.anim";
    private const string ControllerPath = "Assets/Animations/Hamster/Hamster_Controller.controller";
    private const string PrefabPath = "Assets/Prefabs/Hamster.prefab";
    private const string IntroState = "BeerIntro";
    private const string LoopState = "BeerIdle";

    [MenuItem("Tools/Hamster/Build Hamster Beer Idle")]
    public static void BuildAll()
    {
        Sprite[] frames = ImportFrames();
        if (frames.Length != FrameCount)
        {
            Debug.LogError("[BeerIdle] Не хватает кадров: " + frames.Length + " из " + FrameCount);
            return;
        }

        AnimationClip intro = BuildIntroClip(frames.Take(IntroCount).ToArray());
        AnimationClip loop = BuildLoopClip(frames.Skip(LoopStartIndex).ToArray());

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[BeerIdle] Нет контроллера " + ControllerPath);
            return;
        }

        AddState(controller, IntroState, intro);
        AddState(controller, LoopState, loop);
        AddComponent();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BeerIdle] Готово: вступление " + (IntroCount * FrameSeconds) +
                  " с, цикл с паузой на отрыжке " + BurpHoldSeconds + " с");
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
                Debug.LogError("[BeerIdle] Не загрузился спрайт " + path);
        }

        return frames;
    }

    private static AnimationClip BuildIntroClip(Sprite[] frames)
    {
        AnimationClip clip = LoadOrCreateClip(IntroPath);
        EditorCurveBinding binding = Binding();

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = FrameSeconds * i, value = frames[i] };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        ApplySettings(clip, FrameSeconds * frames.Length, false);
        return clip;
    }

    private static AnimationClip BuildLoopClip(Sprite[] frames)
    {
        AnimationClip clip = LoadOrCreateClip(LoopPath);
        EditorCurveBinding binding = Binding();

        // Кадры 5–8 идут по 0,12 с, затем кадр с отрыжкой (индекс 3) висит
        // секунду, после него последний кадр идёт 0,12 с и клип зацикливается.
        int holdIndex = 3;
        int keyCount = frames.Length;
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[keyCount];

        for (int i = 0; i <= holdIndex; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = FrameSeconds * i, value = frames[i] };
        }

        float holdEnd = FrameSeconds * holdIndex + BurpHoldSeconds;

        for (int i = holdIndex + 1; i < frames.Length; i++)
        {
            float time = holdEnd + FrameSeconds * (i - holdIndex - 1);
            keys[i] = new ObjectReferenceKeyframe { time = time, value = frames[i] };
        }

        float stopTime = keys[keys.Length - 1].time + FrameSeconds;

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        ApplySettings(clip, stopTime, true);
        return clip;
    }

    private static AnimationClip LoadOrCreateClip(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null)
            return clip;

        clip = new AnimationClip();
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static EditorCurveBinding Binding()
    {
        return EditorCurveBinding.PPtrCurve("Visual", typeof(SpriteRenderer), "m_Sprite");
    }

    private static void ApplySettings(AnimationClip clip, float stopTime, bool loop)
    {
        clip.frameRate = 60f;

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = stopTime;
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
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
        serialized.FindProperty("idleState").stringValue = "Idle 0";
        serialized.FindProperty("introState").stringValue = IntroState;
        serialized.FindProperty("specialState").stringValue = LoopState;
        serialized.FindProperty("idleDelay").floatValue = 5f;
        serialized.FindProperty("faceRightDuringSpecial").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}

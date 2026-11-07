using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Собирает особое бездействие Хомячихи: 16 кадров с микрофоном и баблом,
/// клип SingIdle, состояние в контроллере и компонент IdleSpecialController
/// на префабе. Включается после 5 секунд без ввода.
///
/// Запуск: Tools/Hamster/Build Girl Sing Idle или
/// Unity.exe -batchmode -quit -executeMethod SingIdleBuilder.BuildAll
/// </summary>
public static class SingIdleBuilder
{
    private const string FrameFolder = "Assets/Art/HamsterGirl/SingIdle";
    private const string FramePrefix = "Girl_SingIdle_";
    private const int FrameCount = 16;
    private const float FrameSeconds = 0.45f;
    private const float Ppu = 98f;
    private const string ClipPath = "Assets/Animations/HamsterGirl/SingIdle.anim";
    private const string ControllerPath = "Assets/Animations/HamsterGirl/HamsterGirl.controller";
    private const string PrefabPath = "Assets/Prefabs/HamsterGirl.prefab";
    private const string StateName = "SingIdle";

    [MenuItem("Tools/Hamster/Build Girl Sing Idle")]
    public static void BuildAll()
    {
        Sprite[] frames = ImportFrames();
        if (frames.Length != FrameCount)
        {
            Debug.LogError("[SingIdle] Не хватает кадров: " + frames.Length + " из " + FrameCount);
            return;
        }

        AnimationClip clip = BuildClip(frames);
        AddState(clip);
        AddComponent();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SingIdle] Готово: " + FrameCount + " кадров, " + (FrameCount * FrameSeconds) + " с, состояние " + StateName);
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

    private static AnimationClip BuildClip(Sprite[] frames)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);

        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, ClipPath);
        }

        clip.frameRate = 60f;

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("Visual", typeof(SpriteRenderer), "m_Sprite");

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = FrameSeconds * i,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = FrameSeconds * frames.Length;
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void AddState(AnimationClip clip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[SingIdle] Нет контроллера " + ControllerPath);
            return;
        }

        ChildAnimatorState[] states = controller.layers[0].stateMachine.states;

        foreach (ChildAnimatorState child in states)
        {
            if (child.state.name == StateName)
            {
                child.state.motion = clip;
                EditorUtility.SetDirty(controller);
                return;
            }
        }

        AnimatorState state = controller.AddMotion(clip, 0);
        state.name = StateName;
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
        serialized.FindProperty("specialState").stringValue = StateName;
        serialized.FindProperty("idleDelay").floatValue = 5f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}

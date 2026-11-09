using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Собирает оживление: зелье, облачко с текстом и ReviveController.</summary>
public static class ReviveSystemBuilder
{
    private const string BubbleArt = "Assets/Art/VFX/ReviveBubble";
    private const string BubbleAnim = "Assets/Animations/VFX/ReviveBubble.anim";
    private const string BubbleController = "Assets/Animations/VFX/ReviveBubble.controller";
    private const string BubblePrefab = "Assets/VFX/Prefabs/ReviveBubble.prefab";

    [MenuItem("Tools/Hamster/Build Revive System")]
    public static void BuildAll()
    {
        EnsureFolder("Assets/Animations/VFX");
        EnsureFolder("Assets/VFX/Prefabs");
        GameObject bubble = BuildBubble();
        BuildHero("Assets/Prefabs/Hamster.prefab", "Assets/Art/Hamster/Revive", "Hamster_Revive_",
                  "Assets/Animations/Hamster", "Hamster_Controller.controller", "Assets/Animations/Hamster/Hamster_Revive.anim", 93.9f, "Idle 0", bubble);
        BuildHero("Assets/Prefabs/HamsterGirl.prefab", "Assets/Art/HamsterGirl/Revive", "HamsterGirl_Revive_",
                  "Assets/Animations/HamsterGirl", "HamsterGirl.controller", "Assets/Animations/HamsterGirl/HG_Revive.anim", 98f, "HGIdle", bubble);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Revive] Готово: зелье 3 с, облачко и ReviveController");
    }

    private static GameObject BuildBubble()
    {
        Sprite[] frames = new Sprite[8];
        for (int i = 0; i < 8; i++)
        {
            string path = BubbleArt + "/ReviveBubble_" + (i + 1).ToString("00") + ".png";
            frames[i] = ImportSprite(path, 200f);
        }

        AnimationClip clip = LoadOrCreateClip(BubbleAnim);
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[8];
        for (int i = 0; i < 8; i++) keys[i] = new ObjectReferenceKeyframe { time = 0.1f * i, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = 0.8f; settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(BubbleController);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(BubbleController);
        AnimatorState state = controller.AddMotion(clip, 0); state.name = "Bubble";
        controller.layers[0].stateMachine.defaultState = state; EditorUtility.SetDirty(controller);

        GameObject go = new GameObject("ReviveBubble");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0]; sr.sortingLayerName = "FX"; sr.sortingOrder = 30;
        Animator animator = go.AddComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        GameObject canvasGo = new GameObject("TextCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(go.transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingLayerName = "FX"; canvas.sortingOrder = 31;
        RectTransform rt = canvasGo.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(300f, 110f); rt.localScale = Vector3.one * 0.01f; rt.localPosition = new Vector3(0f, 0f, -0.01f);
        Text text = canvasGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 46; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.12f, 0.08f, 0.05f, 1f); text.text = "УХУ ЕЛИ?";
        ReviveBubble bubble = go.AddComponent<ReviveBubble>();
        SerializedObject so = new SerializedObject(bubble); so.FindProperty("label").objectReferenceValue = text; so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, BubblePrefab); Object.DestroyImmediate(go);
        return prefab;
    }

    private static void BuildHero(string prefabPath, string art, string prefix, string animRoot, string controllerFile,
                                 string clipPath, float ppu, string idleState, GameObject bubblePrefab)
    {
        Sprite[] frames = new Sprite[6];
        for (int i = 0; i < 6; i++) frames[i] = ImportSprite(art + "/" + prefix + (i + 1).ToString("00") + ".png", ppu);

        AnimationClip clip = LoadOrCreateClip(clipPath);
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("Visual", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[6];
        for (int i = 0; i < 6; i++) keys[i] = new ObjectReferenceKeyframe { time = 0.5f * i, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = 3f; settings.loopTime = false; AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animRoot + "/" + controllerFile);
        AddState(controller, "Revive", clip);

        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        ReviveController revive = contents.GetComponent<ReviveController>();
        if (revive == null) revive = contents.AddComponent<ReviveController>();
        SerializedObject so = new SerializedObject(revive);
        so.FindProperty("input").objectReferenceValue = contents.GetComponent<PlayerInputReader>();
        so.FindProperty("movement").objectReferenceValue = contents.GetComponent<HamsterMovement>();
        so.FindProperty("attack").objectReferenceValue = contents.GetComponent<AttackController>();
        so.FindProperty("animator").objectReferenceValue = contents.GetComponent<Animator>();
        so.FindProperty("body").objectReferenceValue = contents.GetComponent<Rigidbody2D>();
        so.FindProperty("bubblePrefab").objectReferenceValue = bubblePrefab;
        so.FindProperty("reviveStateName").stringValue = "Revive";
        so.FindProperty("idleStateName").stringValue = idleState;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static Sprite ImportSprite(string path, float ppu)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.spritePixelsPerUnit = ppu;
        TextureImporterSettings s = new TextureImporterSettings(); imp.ReadTextureSettings(s); s.spriteMeshType = SpriteMeshType.FullRect; s.spriteAlignment = (int)SpriteAlignment.Center; imp.SetTextureSettings(s);
        imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Bilinear; imp.maxTextureSize = 1024; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static AnimationClip LoadOrCreateClip(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        return clip;
    }

    private static void AddState(AnimatorController controller, string name, AnimationClip clip)
    {
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
            if (child.state.name == name) { child.state.motion = clip; return; }
        controller.AddMotion(clip, 0).name = name;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, name);
    }
}

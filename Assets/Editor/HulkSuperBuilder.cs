using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Собирает суперудар Хомяка: кадры халка, анимации, HulkVisual,
/// шкалу суперудара и аркадный VFX превращения.
/// Меню: Tools/Hamster/Build Hulk Super.
/// </summary>
public static class HulkSuperBuilder
{
    private const string ArtRoot = "Assets/Art/Hamster/Super/Normalized";
    private const string AnimRoot = "Assets/Animations/Hamster";
    private const string ControllerPath = AnimRoot + "/Hamster_Controller.controller";
    private const string HamsterPrefab = "Assets/Prefabs/Hamster.prefab";
    private const string GirlPrefab = "Assets/Prefabs/HamsterGirl.prefab";
    private const string VfxArtRoot = "Assets/VFX/Art/HulkVFX";
    private const string VfxMaterialRoot = "Assets/VFX/Materials";
    private const string VfxPrefabPath = "Assets/VFX/Prefabs/Hulk_Transform_VFX.prefab";
    private const float HulkPpu = 25f;

    [MenuItem("Tools/Hamster/Build Hulk Super")]
    public static void BuildAll()
    {
        EnsureFolder("Assets/VFX/Art");
        EnsureFolder(VfxArtRoot);
        EnsureFolder(VfxMaterialRoot);
        EnsureFolder("Assets/VFX/Prefabs");

        InputMigrationBuilder.BuildAll();

        Sprite[] idle = ImportSet("Hulk_Idle", 8);
        Sprite[] run = ImportSet("Hulk_Run", 8);
        Sprite[] punch = ImportSet("Hulk_Punch", 8);
        Sprite[] jump = ImportSet("Hulk_Jump", 8);
        Sprite[] transformIn = ImportSet("Hulk_Transform", 7);
        Sprite[] transformOut = transformIn.Reverse().ToArray();

        AnimationClip idleClip = BuildClip("HulkIdle", idle, 0.10f, true, false);
        AnimationClip runClip = BuildClip("HulkRun", run, 0.075f, true, false);
        AnimationClip jumpClip = BuildClip("HulkJump", jump, 0.08f, false, false);
        AnimationClip punchClip = BuildClip("HulkPunch", punch, 0.07f, false, true);
        AnimationClip transformInClip = BuildClip("HulkTransformIn", transformIn, 0.10f, false, false);
        AnimationClip transformOutClip = BuildClip("HulkTransformOut", transformOut, 0.08f, false, false);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[Hulk] Нет контроллера " + ControllerPath);
            return;
        }

        AddState(controller, "HulkIdle", idleClip);
        AddState(controller, "HulkRun", runClip);
        AddState(controller, "HulkJump", jumpClip);
        AddState(controller, "HulkPunch", punchClip);
        AddState(controller, "HulkTransformIn", transformInClip);
        AddState(controller, "HulkTransformOut", transformOutClip);
        EditorUtility.SetDirty(controller);

        GameObject transformVfx = BuildTransformVfx();

        AddMeter(HamsterPrefab, true);
        AddMeter(GirlPrefab, false);
        AddHulkVisualAndController(transformVfx);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Hulk] Готово: 6 состояний халка, HulkVisual, шкала суперудара и VFX превращения.");
    }

    private static Sprite[] ImportSet(string prefix, int count)
    {
        Sprite[] result = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            string path = ArtRoot + "/" + prefix + "_" + (i + 1).ToString("00") + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[Hulk] Нет " + path);
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = HulkPpu;
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

    private static AnimationClip BuildClip(string name, Sprite[] frames, float frameSeconds, bool loop, bool punchEvents)
    {
        string path = AnimRoot + "/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("HulkVisual", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = frameSeconds * i, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = frameSeconds * frames.Length;
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (punchEvents)
        {
            AnimationUtility.SetAnimationEvents(clip, new[]
            {
                new AnimationEvent { functionName = "AnimEvent_EnableHitbox", time = 0.22f },
                new AnimationEvent { functionName = "AnimEvent_DisableHitbox", time = 0.38f }
            });
        }
        else
        {
            AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);
        }

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

    private static SuperMeter AddMeter(string prefabPath, bool hamsterBar)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        SuperMeter meter = contents.GetComponent<SuperMeter>();
        if (meter == null)
            meter = contents.AddComponent<SuperMeter>();

        SerializedObject so = new SerializedObject(meter);
        so.FindProperty("health").objectReferenceValue = contents.GetComponent<Health>();
        so.FindProperty("hamsterBar").boolValue = hamsterBar;
        so.FindProperty("maxCharge").floatValue = 100f;
        so.FindProperty("hitChargeGain").floatValue = 20f;
        so.FindProperty("damageChargeGain").floatValue = 10f;
        so.FindProperty("startingCharge").floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
        return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<SuperMeter>();
    }

    private static void AddHulkVisualAndController(GameObject transformVfxPrefab)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(HamsterPrefab);
        SpriteRenderer mainRenderer = contents.transform.Find("Visual")?.GetComponent<SpriteRenderer>();
        if (mainRenderer == null)
            mainRenderer = contents.GetComponentInChildren<SpriteRenderer>();

        Transform existing = contents.transform.Find("HulkVisual");
        GameObject hulkVisual;
        if (existing == null)
        {
            hulkVisual = new GameObject("HulkVisual");
            hulkVisual.transform.SetParent(contents.transform, false);
        }
        else
        {
            hulkVisual = existing.gameObject;
        }

        hulkVisual.transform.localPosition = new Vector3(0f, 4.76f, 0f);
        hulkVisual.transform.localRotation = Quaternion.identity;
        hulkVisual.transform.localScale = Vector3.one;

        SpriteRenderer hulkRenderer = hulkVisual.GetComponent<SpriteRenderer>();
        if (hulkRenderer == null)
            hulkRenderer = hulkVisual.AddComponent<SpriteRenderer>();
        hulkRenderer.enabled = true;
        hulkRenderer.sortingLayerName = mainRenderer != null ? mainRenderer.sortingLayerName : "Characters";
        hulkRenderer.sortingOrder = mainRenderer != null ? mainRenderer.sortingOrder : 0;
        hulkRenderer.color = Color.white;
        hulkVisual.SetActive(false);

        SuperMeter contentsMeter = contents.GetComponent<SuperMeter>();
        SuperHulkController hulk = contents.GetComponent<SuperHulkController>();
        if (hulk == null)
            hulk = contents.AddComponent<SuperHulkController>();

        SerializedObject so = new SerializedObject(hulk);
        so.FindProperty("input").objectReferenceValue = contents.GetComponent<PlayerInputReader>();
        so.FindProperty("meter").objectReferenceValue = contentsMeter;
        so.FindProperty("movement").objectReferenceValue = contents.GetComponent<HamsterMovement>();
        so.FindProperty("attack").objectReferenceValue = contents.GetComponent<AttackController>();
        so.FindProperty("health").objectReferenceValue = contents.GetComponent<Health>();
        so.FindProperty("combatState").objectReferenceValue = contents.GetComponent<PlayerCombatState>();
        so.FindProperty("idleSpecial").objectReferenceValue = contents.GetComponent<IdleSpecialController>();
        so.FindProperty("partnerThrow").objectReferenceValue = contents.GetComponent<PartnerThrowController>();
        so.FindProperty("animator").objectReferenceValue = contents.GetComponent<Animator>();
        so.FindProperty("body").objectReferenceValue = contents.GetComponent<Rigidbody2D>();
        so.FindProperty("mainRenderer").objectReferenceValue = mainRenderer;
        so.FindProperty("hulkVisual").objectReferenceValue = hulkVisual;
        so.FindProperty("hulkRenderer").objectReferenceValue = hulkRenderer;
        so.FindProperty("transformVfxPrefab").objectReferenceValue = transformVfxPrefab;

        so.FindProperty("hulkDuration").floatValue = 7f;
        so.FindProperty("transformInDuration").floatValue = 0.7f;
        so.FindProperty("transformOutDuration").floatValue = 0.56f;
        so.FindProperty("speedMultiplier").floatValue = 0.55f;
        so.FindProperty("jumpMultiplier").floatValue = 0.70f;
        so.FindProperty("hulkDamageMultiplier").floatValue = 3f;
        so.FindProperty("normalIdleState").stringValue = "Idle 0";
        so.FindProperty("transformInState").stringValue = "HulkTransformIn";
        so.FindProperty("transformOutState").stringValue = "HulkTransformOut";
        so.FindProperty("hulkIdleState").stringValue = "HulkIdle";
        so.FindProperty("hulkRunState").stringValue = "HulkRun";
        so.FindProperty("hulkJumpState").stringValue = "HulkJump";

        SerializedProperty states = so.FindProperty("hulkAttackStates");
        states.arraySize = 1;
        states.GetArrayElementAtIndex(0).stringValue = "HulkPunch";
        SerializedProperty durations = so.FindProperty("hulkAttackDurations");
        durations.arraySize = 1;
        durations.GetArrayElementAtIndex(0).floatValue = 0.56f;
        SerializedProperty cancels = so.FindProperty("hulkCancelTimes");
        cancels.arraySize = 1;
        cancels.GetArrayElementAtIndex(0).floatValue = 0.42f;
        SerializedProperty steps = so.FindProperty("hulkStepSpeeds");
        steps.arraySize = 1;
        steps.GetArrayElementAtIndex(0).floatValue = 1.2f;
        so.FindProperty("hulkMovementLock").floatValue = 0.24f;
        so.FindProperty("hulkComboResetTime").floatValue = 0.6f;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, HamsterPrefab);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static GameObject BuildTransformVfx()
    {
        Sprite auraSprite = ImportVfxSprite(VfxArtRoot + "/hulk_aura.png");
        Sprite ringSprite = ImportVfxSprite(VfxArtRoot + "/hulk_ring.png");
        Sprite raySprite = ImportVfxSprite(VfxArtRoot + "/hulk_ray.png");
        Sprite pillarSprite = ImportVfxSprite(VfxArtRoot + "/hulk_pillar.png");
        Sprite flashSprite = ImportVfxSprite(VfxArtRoot + "/hulk_flash.png");

        Material auraMaterial = CreateMaterial("HulkAura.mat", auraSprite.texture, new Color(1f, 0.48f, 0.08f, 1f), 2.3f);
        Material ringMaterial = CreateMaterial("HulkRing.mat", ringSprite.texture, new Color(1f, 0.72f, 0.20f, 1f), 2.5f);
        Material rayMaterial = CreateMaterial("HulkRay.mat", raySprite.texture, new Color(1f, 0.62f, 0.15f, 1f), 2.4f);
        Material pillarMaterial = CreateMaterial("HulkPillar.mat", pillarSprite.texture, new Color(1f, 0.55f, 0.10f, 1f), 2f);
        Material flashMaterial = CreateMaterial("HulkFlash.mat", flashSprite.texture, new Color(1f, 0.86f, 0.55f, 1f), 2.8f);

        GameObject root = new GameObject("Hulk_Transform_VFX");
        HulkTransformVFXController controller = root.AddComponent<HulkTransformVFXController>();

        SpriteRenderer aura = CreateSpriteChild(root.transform, "Aura", auraSprite, auraMaterial, "Characters", -20, new Vector3(0f, 1.2f, 0f), 2.4f);
        SpriteRenderer pillar = CreateSpriteChild(root.transform, "Pillar", pillarSprite, pillarMaterial, "Characters", -19, new Vector3(0f, 2.4f, 0f), 1f);
        pillar.transform.localScale = new Vector3(1.4f, 7f, 1f);

        GameObject rayRoot = new GameObject("Rays");
        rayRoot.transform.SetParent(root.transform, false);
        rayRoot.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        SpriteRenderer[] rays = new SpriteRenderer[12];
        for (int i = 0; i < rays.Length; i++)
        {
            float angle = i * 30f;
            float rad = angle * Mathf.Deg2Rad;
            GameObject rayObject = new GameObject("Ray" + (i + 1));
            rayObject.transform.SetParent(rayRoot.transform, false);
            rayObject.transform.localPosition = new Vector3(Mathf.Cos(rad) * 1.1f, Mathf.Sin(rad) * 1.1f, 0f);
            rayObject.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            rayObject.transform.localScale = new Vector3(1.7f, 0.7f, 1f);
            SpriteRenderer ray = rayObject.AddComponent<SpriteRenderer>();
            ray.sprite = raySprite;
            ray.sharedMaterial = rayMaterial;
            ray.sortingLayerName = "Characters";
            ray.sortingOrder = -18;
            ray.color = new Color(1f, 1f, 1f, 1f);
            rays[i] = ray;
        }

        SpriteRenderer ring = CreateSpriteChild(root.transform, "Ring", ringSprite, ringMaterial, "Characters", -17, new Vector3(0f, 1.2f, 0f), 1f);
        SpriteRenderer flash = CreateSpriteChild(root.transform, "Flash", flashSprite, flashMaterial, "Characters", -16, new Vector3(0f, 1.2f, 0f), 1f);

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("aura").objectReferenceValue = aura;
        so.FindProperty("ring").objectReferenceValue = ring;
        so.FindProperty("pillar").objectReferenceValue = pillar;
        so.FindProperty("flash").objectReferenceValue = flash;
        so.FindProperty("rayRoot").objectReferenceValue = rayRoot.transform;
        SerializedProperty rayArray = so.FindProperty("rays");
        rayArray.arraySize = rays.Length;
        for (int i = 0; i < rays.Length; i++)
            rayArray.GetArrayElementAtIndex(i).objectReferenceValue = rays[i];
        so.FindProperty("duration").floatValue = 0.7f;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, VfxPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static SpriteRenderer CreateSpriteChild(Transform parent, string name, Sprite sprite, Material material,
        string layer, int order, Vector3 localPosition, float scale)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = layer;
        renderer.sortingOrder = order;
        renderer.color = Color.white;
        return renderer;
    }

    private static Sprite ImportVfxSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
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
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static Material CreateMaterial(string fileName, Texture texture, Color tint, float intensity)
    {
        string path = VfxMaterialRoot + "/" + fileName;
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Hamster/VFX/AdditiveSprite"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = Shader.Find("Hamster/VFX/AdditiveSprite");
        material.SetTexture("_MainTex", texture);
        material.SetColor("_Color", tint);
        material.SetFloat("_Intensity", intensity);
        EditorUtility.SetDirty(material);
        return material;
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

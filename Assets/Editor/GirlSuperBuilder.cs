using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Собирает суперудар Хомячихи: анимацию крика, фиолетовую молнию,
/// звуковые кольца и контроллер усиленных атак.
/// Меню: Tools/Hamster/Build Girl Super.
/// </summary>
public static class GirlSuperBuilder
{
    private const string ArtRoot = "Assets/Art/HamsterGirl/Super/Normalized";
    private const string AnimRoot = "Assets/Animations/HamsterGirl";
    private const string ControllerPath = AnimRoot + "/HamsterGirl.controller";
    private const string PrefabPath = "Assets/Prefabs/HamsterGirl.prefab";
    private const string MaterialRoot = "Assets/VFX/Materials";
    private const string VfxPrefabRoot = "Assets/VFX/Prefabs";
    private const string AuraPrefabPath = VfxPrefabRoot + "/Girl_Super_Aura.prefab";
    private const string WavePrefabPath = VfxPrefabRoot + "/Girl_Sound_Wave.prefab";
    private const float GirlSuperPpu = 46.5f;

    [MenuItem("Tools/Hamster/Build Girl Super")]
    public static void BuildAll()
    {
        InputMigrationBuilder.BuildAll();

        Sprite[] frames = ImportFrames();
        AnimationClip clip = BuildClip("GirlSuperAttack", frames, 0.07f, false);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[GirlSuper] Нет контроллера " + ControllerPath);
            return;
        }
        AddState(controller, "GirlSuperAttack", clip);
        EditorUtility.SetDirty(controller);

        GameObject auraPrefab = BuildAuraPrefab();
        GameObject wavePrefab = BuildWavePrefab();
        AddVisualAndController(auraPrefab, wavePrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GirlSuper] Готово: GirlSuperAttack, аура молний и волос, звуковая волна и SuperGirlController.");
    }

    private static Sprite[] ImportFrames()
    {
        Sprite[] frames = new Sprite[8];
        for (int i = 0; i < frames.Length; i++)
        {
            string path = ArtRoot + "/GirlSuper_" + (i + 1).ToString("00") + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[GirlSuper] Нет " + path);
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = GirlSuperPpu;
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
            frames[i] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }
        return frames;
    }

    private static AnimationClip BuildClip(string name, Sprite[] frames, float frameSeconds, bool loop)
    {
        string path = AnimRoot + "/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("GirlSuperVisual", typeof(SpriteRenderer), "m_Sprite");
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

    private static GameObject BuildAuraPrefab()
    {
        Sprite auraSprite = LoadSprite("Assets/VFX/Art/HulkVFX/hulk_aura.png");
        Sprite raySprite = LoadSprite("Assets/VFX/Art/HulkVFX/hulk_ray.png");
        Sprite hairSprite = LoadSprite("Assets/VFX/Art/ThrowVFX/air_wisp.png");
        Material auraMaterial = CreateMaterial("GirlSuperAura.mat", auraSprite.texture, new Color(0.75f, 0.25f, 1f, 1f), 2.4f);
        Material rayMaterial = CreateMaterial("GirlSuperLightning.mat", raySprite.texture, new Color(0.80f, 0.32f, 1f, 1f), 2.8f);

        GameObject root = new GameObject("Girl_Super_Aura");
        GirlSuperAuraVFXController controller = root.AddComponent<GirlSuperAuraVFXController>();

        SpriteRenderer aura = CreateSprite(root.transform, "Aura", auraSprite, auraMaterial, "Characters", -20, Vector3.zero, 2.2f);

        GameObject rayRoot = new GameObject("Lightning");
        rayRoot.transform.SetParent(root.transform, false);
        SpriteRenderer[] rays = new SpriteRenderer[12];
        for (int i = 0; i < rays.Length; i++)
        {
            float angle = i * 30f + (i % 2) * 8f;
            float rad = angle * Mathf.Deg2Rad;
            GameObject rayObject = new GameObject("Lightning" + (i + 1));
            rayObject.transform.SetParent(rayRoot.transform, false);
            rayObject.transform.localPosition = new Vector3(Mathf.Cos(rad) * 1.25f, Mathf.Sin(rad) * 1.55f, 0f);
            rayObject.transform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            rayObject.transform.localScale = new Vector3(1.35f, 0.72f, 1f);
            SpriteRenderer ray = rayObject.AddComponent<SpriteRenderer>();
            ray.sprite = raySprite;
            ray.sharedMaterial = rayMaterial;
            ray.sortingLayerName = "Characters";
            ray.sortingOrder = -18;
            rays[i] = ray;
        }

        SpriteRenderer[] hairStrands = new SpriteRenderer[6];
        Vector3[] hairPositions =
        {
            new Vector3(-0.62f, 1.12f, 0f),
            new Vector3(-0.84f, 0.78f, 0f),
            new Vector3(-0.48f, 1.52f, 0f),
            new Vector3(0.62f, 1.12f, 0f),
            new Vector3(0.84f, 0.78f, 0f),
            new Vector3(0.48f, 1.52f, 0f)
        };
        float[] hairAngles = { 22f, -12f, 48f, 158f, 192f, 132f };
        float[] hairScales = { 0.66f, 0.58f, 0.56f, 0.66f, 0.58f, 0.56f };
        for (int i = 0; i < hairStrands.Length; i++)
        {
            GameObject hairObject = new GameObject("Hair" + (i + 1));
            hairObject.transform.SetParent(root.transform, false);
            hairObject.transform.localPosition = hairPositions[i];
            hairObject.transform.localRotation = Quaternion.Euler(0f, 0f, hairAngles[i]);
            hairObject.transform.localScale = new Vector3(hairScales[i], hairScales[i] * 0.55f, 1f);
            SpriteRenderer hair = hairObject.AddComponent<SpriteRenderer>();
            hair.sprite = hairSprite;
            hair.sortingLayerName = "Characters";
            hair.sortingOrder = -19;
            hair.color = new Color(0.55f, 0.20f, 0.34f, 0.78f);
            hairStrands[i] = hair;
        }

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("aura").objectReferenceValue = aura;
        so.FindProperty("rayRoot").objectReferenceValue = rayRoot.transform;
        SerializedProperty rayArray = so.FindProperty("rays");
        rayArray.arraySize = rays.Length;
        for (int i = 0; i < rays.Length; i++)
            rayArray.GetArrayElementAtIndex(i).objectReferenceValue = rays[i];
        SerializedProperty hairArray = so.FindProperty("hairStrands");
        hairArray.arraySize = hairStrands.Length;
        for (int i = 0; i < hairStrands.Length; i++)
            hairArray.GetArrayElementAtIndex(i).objectReferenceValue = hairStrands[i];
        so.FindProperty("duration").floatValue = 10f;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, AuraPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject BuildWavePrefab()
    {
        Sprite ringSprite = LoadSprite("Assets/VFX/Art/HulkVFX/hulk_ring.png");
        Sprite flashSprite = LoadSprite("Assets/VFX/Art/HulkVFX/hulk_flash.png");
        Material ringMaterial = CreateMaterial("GirlSoundRing.mat", ringSprite.texture, new Color(0.90f, 0.38f, 1f, 1f), 2.6f);
        Material flashMaterial = CreateMaterial("GirlSoundFlash.mat", flashSprite.texture, new Color(1f, 0.72f, 1f, 1f), 2.4f);

        GameObject root = new GameObject("Girl_Sound_Wave");
        GirlSoundWaveVFXController controller = root.AddComponent<GirlSoundWaveVFXController>();

        SpriteRenderer[] rings = new SpriteRenderer[3];
        for (int i = 0; i < rings.Length; i++)
            rings[i] = CreateSprite(root.transform, "Ring" + (i + 1), ringSprite, ringMaterial, "FX", 20 + i, Vector3.zero, 0.3f);

        SpriteRenderer flash = CreateSprite(root.transform, "Flash", flashSprite, flashMaterial, "FX", 24, Vector3.zero, 0.5f);

        SerializedObject so = new SerializedObject(controller);
        SerializedProperty ringArray = so.FindProperty("rings");
        ringArray.arraySize = rings.Length;
        for (int i = 0; i < rings.Length; i++)
            ringArray.GetArrayElementAtIndex(i).objectReferenceValue = rings[i];
        so.FindProperty("flash").objectReferenceValue = flash;
        so.FindProperty("duration").floatValue = 0.52f;
        so.FindProperty("maxScale").floatValue = 7f;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, WavePrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void AddVisualAndController(GameObject auraPrefab, GameObject wavePrefab)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        SpriteRenderer mainRenderer = contents.transform.Find("Visual")?.GetComponent<SpriteRenderer>();
        if (mainRenderer == null)
            mainRenderer = contents.GetComponentInChildren<SpriteRenderer>();

        Transform existing = contents.transform.Find("GirlSuperVisual");
        GameObject superVisual;
        if (existing == null)
        {
            superVisual = new GameObject("GirlSuperVisual");
            superVisual.transform.SetParent(contents.transform, false);
        }
        else
        {
            superVisual = existing.gameObject;
        }

        superVisual.transform.localPosition = Vector3.zero;
        superVisual.transform.localRotation = Quaternion.identity;
        superVisual.transform.localScale = Vector3.one;
        SpriteRenderer superRenderer = superVisual.GetComponent<SpriteRenderer>();
        if (superRenderer == null)
            superRenderer = superVisual.AddComponent<SpriteRenderer>();
        superRenderer.enabled = true;
        superRenderer.sortingLayerName = mainRenderer != null ? mainRenderer.sortingLayerName : "Characters";
        superRenderer.sortingOrder = mainRenderer != null ? mainRenderer.sortingOrder : 0;
        superRenderer.color = Color.white;
        superVisual.SetActive(false);

        SuperGirlController controller = contents.GetComponent<SuperGirlController>();
        if (controller == null)
            controller = contents.AddComponent<SuperGirlController>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("input").objectReferenceValue = contents.GetComponent<PlayerInputReader>();
        so.FindProperty("meter").objectReferenceValue = contents.GetComponent<SuperMeter>();
        so.FindProperty("movement").objectReferenceValue = contents.GetComponent<HamsterMovement>();
        so.FindProperty("attack").objectReferenceValue = contents.GetComponent<AttackController>();
        so.FindProperty("health").objectReferenceValue = contents.GetComponent<Health>();
        so.FindProperty("combatState").objectReferenceValue = contents.GetComponent<PlayerCombatState>();
        so.FindProperty("idleSpecial").objectReferenceValue = contents.GetComponent<IdleSpecialController>();
        so.FindProperty("partnerThrow").objectReferenceValue = contents.GetComponent<PartnerThrowController>();
        so.FindProperty("animator").objectReferenceValue = contents.GetComponent<Animator>();
        so.FindProperty("body").objectReferenceValue = contents.GetComponent<Rigidbody2D>();
        so.FindProperty("mainRenderer").objectReferenceValue = mainRenderer;
        so.FindProperty("superVisual").objectReferenceValue = superVisual;
        so.FindProperty("superRenderer").objectReferenceValue = superRenderer;
        so.FindProperty("auraPrefab").objectReferenceValue = auraPrefab;
        so.FindProperty("soundWavePrefab").objectReferenceValue = wavePrefab;
        so.FindProperty("dizzyStarsPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/Prefabs/DizzyStars.prefab");
        so.FindProperty("superDuration").floatValue = 10f;
        so.FindProperty("superAttackDuration").floatValue = 0.56f;
        so.FindProperty("damageMultiplier").floatValue = 3f;
        so.FindProperty("waveDamage").floatValue = 24f;
        so.FindProperty("waveRadius").floatValue = 6f;
        so.FindProperty("waveStunDuration").floatValue = 2f;
        so.FindProperty("waveHitStop").floatValue = 0.04f;
        so.FindProperty("attackCooldown").floatValue = 2.1f;
        so.FindProperty("normalIdleState").stringValue = "HGIdle";
        so.FindProperty("normalRunState").stringValue = "HGRun";
        so.FindProperty("normalJumpState").stringValue = "HGJump";
        so.FindProperty("superAttackState").stringValue = "GirlSuperAttack";
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite, Material material,
        string layer, int order, Vector3 position, float scale)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = layer;
        renderer.sortingOrder = order;
        renderer.color = Color.white;
        return renderer;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static Material CreateMaterial(string fileName, Texture texture, Color tint, float intensity)
    {
        string path = MaterialRoot + "/" + fileName;
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
}

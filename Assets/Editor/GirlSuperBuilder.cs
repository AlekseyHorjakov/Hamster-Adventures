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
        Debug.Log("[GirlSuper] Готово: GirlSuperAttack, молнии вокруг Хомячихи, звуковая волна и SuperGirlController.");
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
        Sprite[] boltFrames = new Sprite[8];
        for (int i = 0; i < boltFrames.Length; i++)
            boltFrames[i] = ImportLightningFrame("lightning_" + (i + 1).ToString("00") + ".png");

        Sprite[] cageFrames = new Sprite[4];
        for (int i = 0; i < cageFrames.Length; i++)
            cageFrames[i] = ImportLightningFrame("lightning_" + (12 + i).ToString("00") + ".png");

        GameObject root = new GameObject("Girl_Super_Aura");
        GirlSuperAuraVFXController controller = root.AddComponent<GirlSuperAuraVFXController>();

        GameObject cageObject = new GameObject("LightningCage");
        cageObject.transform.SetParent(root.transform, false);
        cageObject.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        cageObject.transform.localScale = new Vector3(1.55f, 1.75f, 1f);
        SpriteRenderer cage = cageObject.AddComponent<SpriteRenderer>();
        cage.sprite = cageFrames[0];
        cage.sortingLayerName = "Characters";
        cage.sortingOrder = -16;
        cage.color = Color.white;

        Vector3[] positions =
        {
            new Vector3(-0.92f, 1.22f, 0f),
            new Vector3(-1.10f, 0.48f, 0f),
            new Vector3(-0.68f, 1.78f, 0f),
            new Vector3(-0.38f, -0.12f, 0f),
            new Vector3(0.92f, 1.22f, 0f),
            new Vector3(1.10f, 0.48f, 0f),
            new Vector3(0.68f, 1.78f, 0f),
            new Vector3(0.38f, -0.12f, 0f)
        };
        float[] angles = { 24f, -28f, 52f, -62f, 156f, 208f, 128f, 242f };
        float[] scales = { 0.58f, 0.48f, 0.46f, 0.42f, 0.58f, 0.48f, 0.46f, 0.42f };

        SpriteRenderer[] bolts = new SpriteRenderer[positions.Length];
        for (int i = 0; i < bolts.Length; i++)
        {
            GameObject boltObject = new GameObject("LightningBolt" + (i + 1));
            boltObject.transform.SetParent(root.transform, false);
            boltObject.transform.localPosition = positions[i];
            boltObject.transform.localRotation = Quaternion.Euler(0f, 0f, angles[i]);
            Vector3 scale = new Vector3(scales[i], scales[i], 1f);
            if (i >= bolts.Length / 2)
                scale.x = -scale.x;
            boltObject.transform.localScale = scale;

            SpriteRenderer bolt = boltObject.AddComponent<SpriteRenderer>();
            bolt.sprite = boltFrames[i % boltFrames.Length];
            bolt.sortingLayerName = "Characters";
            bolt.sortingOrder = -15;
            bolt.color = Color.white;
            bolts[i] = bolt;
        }

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("cage").objectReferenceValue = cage;

        SerializedProperty cageArray = so.FindProperty("cageFrames");
        cageArray.arraySize = cageFrames.Length;
        for (int i = 0; i < cageFrames.Length; i++)
            cageArray.GetArrayElementAtIndex(i).objectReferenceValue = cageFrames[i];

        SerializedProperty boltArray = so.FindProperty("bolts");
        boltArray.arraySize = bolts.Length;
        for (int i = 0; i < bolts.Length; i++)
            boltArray.GetArrayElementAtIndex(i).objectReferenceValue = bolts[i];

        SerializedProperty frameArray = so.FindProperty("boltFrames");
        frameArray.arraySize = boltFrames.Length;
        for (int i = 0; i < boltFrames.Length; i++)
            frameArray.GetArrayElementAtIndex(i).objectReferenceValue = boltFrames[i];

        so.FindProperty("duration").floatValue = 10f;
        so.FindProperty("cageFrameRate").floatValue = 11f;
        so.FindProperty("boltFrameRate").floatValue = 15f;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, AuraPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Sprite ImportLightningFrame(string fileName)
    {
        string path = "Assets/VFX/Art/GirlLightningVFX/" + fileName;
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

using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Собирает бросок напарника: воздушные VFX, эффект столкновения, HitData
/// и компонент PartnerThrowController на обоих героях.
/// Меню: Tools/Hamster/Build Partner Throw.
/// </summary>
public static class PartnerThrowBuilder
{
    private const string ArtFolder = "Assets/VFX/Art/ThrowVFX";
    private const string MaterialFolder = "Assets/VFX/Materials";
    private const string PrefabFolder = "Assets/VFX/Prefabs";
    private const string FlightPrefabPath = PrefabFolder + "/PartnerThrowFlight.prefab";
    private const string ImpactPrefabPath = PrefabFolder + "/PartnerThrowImpact.prefab";
    private const string HitDataPath = "Assets/Combat/HitData_ThrownPartner.asset";

    [MenuItem("Tools/Hamster/Build Partner Throw")]
    public static void BuildAll()
    {
        EnsureFolder("Assets/VFX");
        EnsureFolder("Assets/VFX/Art");
        EnsureFolder(ArtFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder("Assets/Combat");

        InputMigrationBuilder.BuildAll();

        Sprite hamsterFlight = ImportSprite("Assets/Art/Hamster/Flight/Hamster_Flight.png", 150f);
        Sprite girlFlight = ImportSprite("Assets/Art/HamsterGirl/Flight/HamsterGirl_Flight.png", 150f);
        Sprite hamsterCarry = ImportSprite("Assets/Art/Hamster/Carry/Hamster_Carry.png", 160f);
        Sprite girlCarry = ImportSprite("Assets/Art/HamsterGirl/Carry/HamsterGirl_Carry.png", 160f);

        Sprite streak = ImportSprite(ArtFolder + "/air_streak.png", 100f);
        Sprite wisp = ImportSprite(ArtFolder + "/air_wisp.png", 100f);
        Sprite vortex = ImportSprite(ArtFolder + "/air_vortex.png", 100f);
        Sprite distortion = ImportSprite(ArtFolder + "/air_distortion.png", 100f);
        Sprite impactRing = ImportSprite(ArtFolder + "/air_impact_ring.png", 180f);
        Sprite impactFlash = ImportSprite(ArtFolder + "/air_impact_flash.png", 180f);

        Material streakMaterial = CreateMaterial("AirStreak.mat", streak.texture, new Color(0.62f, 0.92f, 1f, 1f), 2.2f);
        Material wispMaterial = CreateMaterial("AirWisp.mat", wisp.texture, new Color(0.52f, 0.86f, 1f, 1f), 1.8f);
        Material vortexMaterial = CreateMaterial("AirVortex.mat", vortex.texture, new Color(0.72f, 0.96f, 1f, 1f), 2.3f);
        Material distortionMaterial = CreateMaterial("AirDistortion.mat", distortion.texture, new Color(0.72f, 0.94f, 1f, 1f), 0.7f);
        Material ringMaterial = CreateMaterial("AirImpactRing.mat", impactRing.texture, new Color(0.72f, 0.95f, 1f, 1f), 2.4f);
        Material flashMaterial = CreateMaterial("AirImpactFlash.mat", impactFlash.texture, new Color(0.88f, 0.98f, 1f, 1f), 2.8f);

        HitData throwHit = BuildHitData();
        GameObject flightPrefab = BuildFlightVfx(streakMaterial, wispMaterial, vortexMaterial, distortionMaterial, wisp, vortex, distortion);
        GameObject impactPrefab = BuildImpactVfx(ringMaterial, flashMaterial, impactRing, impactFlash);

        WireHero("Assets/Prefabs/Hamster.prefab", "HJump", "JumpKick", "Idle 0", hamsterFlight, hamsterCarry, 0.84f, throwHit, flightPrefab, impactPrefab);
        WireHero("Assets/Prefabs/HamsterGirl.prefab", "HGJump", "HGJumpKick", "HGIdle", girlFlight, girlCarry, 0.98f, throwHit, flightPrefab, impactPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Throw] Готово: бросок напарника, воздушный VFX и эффект столкновения.");
    }

    private static HitData BuildHitData()
    {
        HitData hit = AssetDatabase.LoadAssetAtPath<HitData>(HitDataPath);
        if (hit == null)
        {
            hit = ScriptableObject.CreateInstance<HitData>();
            AssetDatabase.CreateAsset(hit, HitDataPath);
        }

        hit.damage = 24f;
        hit.knockback = 14f;
        hit.hitStop = 0.1f;
        hit.hitStun = 0.55f;
        hit.launchVelocity = new Vector2(11f, 4.2f);
        hit.hitboxSize = new Vector2(1.6f, 1.3f);
        hit.hitboxOffset = new Vector2(0.8f, 0f);
        EditorUtility.SetDirty(hit);
        return hit;
    }

    private static GameObject BuildFlightVfx(Material streakMaterial, Material wispMaterial,
        Material vortexMaterial, Material distortionMaterial,
        Sprite wisp, Sprite vortex, Sprite distortion)
    {
        GameObject root = new GameObject("PartnerThrowFlight");
        PartnerThrowVFXController controller = root.AddComponent<PartnerThrowVFXController>();

        TrailRenderer[] trails = new TrailRenderer[3];
        string[] trailNames = { "SpeedTrailTop", "SpeedTrailCenter", "SpeedTrailBottom" };
        float[] trailY = { 0.48f, 0f, -0.48f };

        for (int i = 0; i < trails.Length; i++)
        {
            GameObject trailObject = new GameObject(trailNames[i]);
            trailObject.transform.SetParent(root.transform, false);
            trailObject.transform.localPosition = new Vector3(-0.25f, trailY[i], 0f);

            TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
            trail.material = streakMaterial;
            trail.time = 0.24f;
            trail.widthMultiplier = 0.22f;
            trail.minVertexDistance = 0.04f;
            trail.numCapVertices = 4;
            trail.numCornerVertices = 6;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.sortingLayerName = "FX";
            trail.sortingOrder = 8;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.62f, 0.92f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 1f, 1f), 0.22f),
                    new GradientColorKey(new Color(0.48f, 0.82f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.55f, 0.35f),
                    new GradientAlphaKey(0f, 1f)
                });
            trail.colorGradient = gradient;
            trail.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

            trails[i] = trail;
        }

        SpriteRenderer[] streams = new SpriteRenderer[2];
        for (int i = 0; i < streams.Length; i++)
        {
            GameObject streamObject = new GameObject("AirStream" + (i + 1));
            streamObject.transform.SetParent(root.transform, false);
            streamObject.transform.localPosition = new Vector3(-0.15f, i == 0 ? 0.62f : -0.62f, 0f);
            streamObject.transform.localScale = new Vector3(1.2f, 0.72f, 1f);

            SpriteRenderer renderer = streamObject.AddComponent<SpriteRenderer>();
            renderer.sprite = wisp;
            renderer.sharedMaterial = wispMaterial;
            renderer.sortingLayerName = "FX";
            renderer.sortingOrder = 12;
            renderer.color = new Color(1f, 1f, 1f, 0f);
            streams[i] = renderer;
        }

        GameObject vortexObject = new GameObject("FistVortex");
        vortexObject.transform.SetParent(root.transform, false);
        vortexObject.transform.localPosition = new Vector3(1.15f, 0.12f, 0f);
        vortexObject.transform.localScale = Vector3.one * 0.8f;
        SpriteRenderer vortexRenderer = vortexObject.AddComponent<SpriteRenderer>();
        vortexRenderer.sprite = vortex;
        vortexRenderer.sharedMaterial = vortexMaterial;
        vortexRenderer.sortingLayerName = "FX";
        vortexRenderer.sortingOrder = 14;
        vortexRenderer.color = new Color(1f, 1f, 1f, 0f);

        GameObject distortionObject = new GameObject("AirDistortion");
        distortionObject.transform.SetParent(root.transform, false);
        distortionObject.transform.localPosition = new Vector3(1.45f, 0.05f, 0f);
        distortionObject.transform.localScale = new Vector3(0.9f, 0.72f, 1f);
        SpriteRenderer distortionRenderer = distortionObject.AddComponent<SpriteRenderer>();
        distortionRenderer.sprite = distortion;
        distortionRenderer.sharedMaterial = distortionMaterial;
        distortionRenderer.sortingLayerName = "FX";
        distortionRenderer.sortingOrder = 9;
        distortionRenderer.color = new Color(1f, 1f, 1f, 0f);

        SerializedObject so = new SerializedObject(controller);
        SerializedProperty speedTrails = so.FindProperty("speedTrails");
        speedTrails.arraySize = trails.Length;
        for (int i = 0; i < trails.Length; i++)
            speedTrails.GetArrayElementAtIndex(i).objectReferenceValue = trails[i];

        SerializedProperty airStreams = so.FindProperty("airStreams");
        airStreams.arraySize = streams.Length;
        for (int i = 0; i < streams.Length; i++)
            airStreams.GetArrayElementAtIndex(i).objectReferenceValue = streams[i];

        so.FindProperty("fistVortex").objectReferenceValue = vortexRenderer;
        so.FindProperty("distortion").objectReferenceValue = distortionRenderer;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, FlightPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject BuildImpactVfx(Material ringMaterial, Material flashMaterial,
        Sprite ringSprite, Sprite flashSprite)
    {
        GameObject root = new GameObject("PartnerThrowImpact");
        ThrowImpactVFX controller = root.AddComponent<ThrowImpactVFX>();

        GameObject ringObject = new GameObject("Ring");
        ringObject.transform.SetParent(root.transform, false);
        SpriteRenderer ring = ringObject.AddComponent<SpriteRenderer>();
        ring.sprite = ringSprite;
        ring.sharedMaterial = ringMaterial;
        ring.sortingLayerName = "FX";
        ring.sortingOrder = 20;
        ring.transform.localScale = Vector3.one * 0.35f;

        GameObject flashObject = new GameObject("Flash");
        flashObject.transform.SetParent(root.transform, false);
        SpriteRenderer flash = flashObject.AddComponent<SpriteRenderer>();
        flash.sprite = flashSprite;
        flash.sharedMaterial = flashMaterial;
        flash.sortingLayerName = "FX";
        flash.sortingOrder = 21;
        flash.transform.localScale = Vector3.one * 0.4f;

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("ring").objectReferenceValue = ring;
        so.FindProperty("flash").objectReferenceValue = flash;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ImpactPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void WireHero(string prefabPath, string carriedState, string thrownState, string idleState,
        Sprite flightSprite, Sprite carrySprite, float carryPoseLocalY,
        HitData throwHit, GameObject flightPrefab, GameObject impactPrefab)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        PartnerThrowController controller = contents.GetComponent<PartnerThrowController>();
        if (controller == null)
            controller = contents.AddComponent<PartnerThrowController>();

        SpriteRenderer mainSprite = contents.transform.Find("Visual")?.GetComponent<SpriteRenderer>();
        if (mainSprite == null)
            mainSprite = contents.GetComponentInChildren<SpriteRenderer>();

        Transform poseTransform = contents.transform.Find("CarryPose");
        GameObject poseObject;
        if (poseTransform == null)
        {
            poseObject = new GameObject("CarryPose");
            poseObject.transform.SetParent(contents.transform, false);
        }
        else
        {
            poseObject = poseTransform.gameObject;
        }

        poseObject.transform.localPosition = new Vector3(0f, carryPoseLocalY, 0f);
        poseObject.transform.localRotation = Quaternion.identity;
        poseObject.transform.localScale = Vector3.one;

        SpriteRenderer carryRenderer = poseObject.GetComponent<SpriteRenderer>();
        if (carryRenderer == null)
            carryRenderer = poseObject.AddComponent<SpriteRenderer>();
        carryRenderer.sprite = carrySprite;
        carryRenderer.color = mainSprite != null ? mainSprite.color : Color.white;
        carryRenderer.sortingLayerName = mainSprite != null ? mainSprite.sortingLayerName : "Characters";
        carryRenderer.sortingOrder = mainSprite != null ? mainSprite.sortingOrder + 1 : 1;
        carryRenderer.enabled = false;

        CarryPoseController poseController = contents.GetComponent<CarryPoseController>();
        if (poseController == null)
            poseController = contents.AddComponent<CarryPoseController>();

        SerializedObject poseSo = new SerializedObject(poseController);
        poseSo.FindProperty("throwController").objectReferenceValue = controller;
        poseSo.FindProperty("movement").objectReferenceValue = contents.GetComponent<HamsterMovement>();
        poseSo.FindProperty("body").objectReferenceValue = contents.GetComponent<Rigidbody2D>();
        poseSo.FindProperty("mainRenderer").objectReferenceValue = mainSprite;
        poseSo.FindProperty("carryRenderer").objectReferenceValue = carryRenderer;
        poseSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("input").objectReferenceValue = contents.GetComponent<PlayerInputReader>();
        so.FindProperty("movement").objectReferenceValue = contents.GetComponent<HamsterMovement>();
        so.FindProperty("attack").objectReferenceValue = contents.GetComponent<AttackController>();
        so.FindProperty("animator").objectReferenceValue = contents.GetComponent<Animator>();
        so.FindProperty("body").objectReferenceValue = contents.GetComponent<Rigidbody2D>();
        so.FindProperty("bodyCollider").objectReferenceValue = contents.GetComponent<Collider2D>();
        so.FindProperty("sprite").objectReferenceValue = mainSprite;
        so.FindProperty("flightSprite").objectReferenceValue = flightSprite;
        so.FindProperty("health").objectReferenceValue = contents.GetComponent<Health>();
        so.FindProperty("combatState").objectReferenceValue = contents.GetComponent<PlayerCombatState>();

        so.FindProperty("idleStateName").stringValue = idleState;
        so.FindProperty("carriedStateName").stringValue = carriedState;
        so.FindProperty("thrownStateName").stringValue = thrownState;
        so.FindProperty("pickupRange").floatValue = 2.4f;
        so.FindProperty("launchSpeed").floatValue = 13f;
        so.FindProperty("launchAcceleration").floatValue = 24f;
        so.FindProperty("flightDuration").floatValue = 0.55f;
        so.FindProperty("launchForwardOffset").floatValue = 1f;
        so.FindProperty("afterHitFlightTime").floatValue = 0.1f;
        so.FindProperty("throwHit").objectReferenceValue = throwHit;
        so.FindProperty("flightVfxPrefab").objectReferenceValue = flightPrefab;
        so.FindProperty("impactVfxPrefab").objectReferenceValue = impactPrefab;
        so.FindProperty("impactShakeStrength").floatValue = 0.55f;
        so.FindProperty("impactShakeDuration").floatValue = 0.24f;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static Material CreateMaterial(string fileName, Texture texture, Color tint, float intensity)
    {
        string path = MaterialFolder + "/" + fileName;
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

    private static Sprite ImportSprite(string path, float ppu)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
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

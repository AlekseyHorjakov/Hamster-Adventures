using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Сборщик VFX заряженного удара из присланных кадров.
/// Запускается из меню Tools/Hamster/Build Charge VFX или в batchmode:
/// Unity.exe -batchmode -quit -projectPath ... -executeMethod ChargeVFXBuilder.BuildAll
///
/// Кадры лежат в Assets/VFX/Art/ChargeVFX (шесть наборов: flame_tongue,
/// fire_ring, embers, ground_glow, charge_flash, aura_flame).
/// Скрипт настраивает импорт, собирает материалы и префаб Hamster_Charge_VFX,
/// а затем прописывает ссылку на префаб в оба персонажа.
/// </summary>
public static class ChargeVFXBuilder
{
    private const string VfxRoot = "Assets/VFX";
    private const string ArtRoot = "Assets/VFX/Art/ChargeVFX";
    private const string MaterialRoot = "Assets/VFX/Materials";
    private const string PrefabRoot = "Assets/VFX/Prefabs";
    private const string ShaderPath = "Assets/VFX/Shaders/AdditiveSprite.shader";
    private const string PrefabPath = PrefabRoot + "/Hamster_Charge_VFX.prefab";

    [MenuItem("Tools/Hamster/Build Charge VFX")]
    public static void BuildAll()
    {
        EnsureFolder(VfxRoot);
        EnsureFolder("Assets/VFX/Art");
        EnsureFolder(ArtRoot);
        EnsureFolder(MaterialRoot);
        EnsureFolder(PrefabRoot);

        AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceUpdate);
        Shader additive = Shader.Find("Hamster/VFX/AdditiveSprite");
        if (additive == null)
        {
            Debug.LogError("[VFX] Не найден шейдер Hamster/VFX/AdditiveSprite");
            return;
        }

        Texture2D flameSheet = ImportSheet("flame_tongue_spritesheet");
        Texture2D emberSheet = ImportSheet("embers_spritesheet");

        Sprite[] ringFrames = ImportFrames("fire_ring", 8);
        Sprite[] auraFrames = ImportFrames("aura_flame", 8);
        Sprite[] glowFrames = ImportFrames("ground_glow", 6);
        Sprite[] flashFrames = ImportFrames("charge_flash", 6);

        if (ringFrames.Length == 0 || auraFrames.Length == 0 || glowFrames.Length == 0 || flashFrames.Length == 0)
        {
            Debug.LogError("[VFX] Не хватает кадров: проверь Assets/VFX/Art/ChargeVFX");
            return;
        }

        Material flameMaterial = BuildMaterial("Flame", additive, flameSheet);
        Material emberMaterial = BuildMaterial("Ember", additive, emberSheet);
        Material ringMaterial = BuildMaterial("Ring", additive, ringFrames[0].texture);
        Material auraMaterial = BuildMaterial("Aura", additive, auraFrames[0].texture);
        Material glowMaterial = BuildMaterial("GroundGlow", additive, glowFrames[0].texture);
        Material flashMaterial = BuildMaterial("Flash", additive, flashFrames[0].texture);

        GameObject root = new GameObject("Hamster_Charge_VFX");
        ChargeVFXController controller = root.AddComponent<ChargeVFXController>();

        SpriteRenderer ring = CreateSprite(root.transform, "GroundFire", new Vector3(0f, 0.05f, 0f), ringFrames[0], ringMaterial, 6);
        SpriteRenderer aura = CreateSprite(root.transform, "FireAura", new Vector3(0f, 1.6f, 0f), auraFrames[0], auraMaterial, 10);
        SpriteRenderer glow = CreateSprite(root.transform, "GroundGlow", new Vector3(0f, 0.02f, 0f), glowFrames[0], glowMaterial, 4);
        SpriteRenderer flash = CreateSprite(root.transform, "BurstFlash", new Vector3(0f, 1.6f, 0f), flashFrames[0], flashMaterial, 30);

        ParticleSystem flames = CreateParticles(root.transform, "FlameParticles", new Vector3(0f, 0.3f, 0f), flameMaterial, 14, 8, 1);
        ConfigureFlames(flames);

        ParticleSystem embers = CreateParticles(root.transform, "FlyingEmbers", new Vector3(0f, 0.6f, 0f), emberMaterial, 20, 8, 1);
        ConfigureEmbers(embers);

        controller.Configure(
            ring, ringFrames,
            aura, auraFrames,
            glow, glowFrames,
            flash, flashFrames,
            flames, embers);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            AssetDatabase.DeleteAsset(PrefabPath);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        WireCharacter("Assets/Prefabs/Hamster.prefab", prefab);
        WireCharacter("Assets/Prefabs/HamsterGirl.prefab", prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VFX] Hamster_Charge_VFX собран: " + PrefabPath);
    }

    // ------------------------------------------------------------------ слои

    private static SpriteRenderer CreateSprite(
        Transform parent,
        string name,
        Vector3 localPosition,
        Sprite sprite,
        Material material,
        int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "FX";
        renderer.sortingOrder = sortingOrder;
        renderer.color = new Color(1f, 1f, 1f, 0f);
        renderer.drawMode = SpriteDrawMode.Simple;

        return renderer;
    }

    private static ParticleSystem CreateParticles(
        Transform parent,
        string name,
        Vector3 localPosition,
        Material material,
        int sortingOrder,
        int frames,
        int rows)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 1f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.arc = 360f;

        // Каждый партикл проигрывает весь лист кадров за свою жизнь.
        ParticleSystem.TextureSheetAnimationModule sheet = ps.textureSheetAnimation;
        sheet.enabled = true;
        sheet.numTilesX = frames;
        sheet.numTilesY = rows;
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
        sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 0f);
        sheet.cycleCount = 1;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "FX";
        renderer.sortingOrder = sortingOrder;
        renderer.maxParticleSize = 6f;

        return ps;
    }

    private static void ConfigureFlames(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
        main.maxParticles = 120;
        main.gravityModifier = 0f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f);

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.radius = 0.3f;
        shape.radiusThickness = 0f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.y = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.4f);
        noise.frequency = 1.1f;
        noise.scrollSpeed = 0.7f;
        noise.damping = true;
        noise.octaveCount = 2;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeOutGradient());

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, SizeCurve(0.35f, 1f, 0.15f));

        ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-0.7f, 0.7f);
    }

    private static void ConfigureEmbers(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.maxParticles = 80;
        main.gravityModifier = 0.25f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.radius = 0.4f;
        shape.radiusThickness = 0.2f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 0.9f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.7f);
        noise.frequency = 1.6f;
        noise.scrollSpeed = 0.9f;
        noise.damping = true;
        noise.octaveCount = 2;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeOutGradient());

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, SizeCurve(1f, 0.8f, 0f));
    }

    private static Gradient FadeOutGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private static AnimationCurve SizeCurve(float start, float peak, float end)
    {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, start);
        curve.AddKey(0.25f, peak);
        curve.AddKey(1f, end);
        return curve;
    }

    // ------------------------------------------------------------- материалы

    private static Material BuildMaterial(string name, Shader shader, Texture texture)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        material.SetTexture("_MainTex", texture);
        material.SetColor("_Color", Color.white);
        material.SetFloat("_Intensity", 1f);
        EditorUtility.SetDirty(material);
        return material;
    }

    // -------------------------------------------------------------- импорт

    private static Texture2D ImportSheet(string fileName)
    {
        string path = ArtRoot + "/" + fileName + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Sprite[] ImportFrames(string prefix, int count)
    {
        List<Sprite> frames = new List<Sprite>(count);

        for (int i = 1; i <= count; i++)
        {
            string path = ArtRoot + "/" + prefix + "_" + i.ToString("00") + ".png";

            if (!File.Exists(path))
            {
                Debug.LogError("[VFX] Нет кадра " + path);
                continue;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null)
                Debug.LogError("[VFX] Не удалось загрузить спрайт " + path);
            else
                frames.Add(sprite);
        }

        return frames.ToArray();
    }

    // -------------------------------------------------------------- служебное

    private static void WireCharacter(string prefabPath, GameObject vfxPrefab)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);

        AttackController controller = contents.GetComponent<AttackController>();
        if (controller == null)
        {
            Debug.LogWarning("[VFX] В " + prefabPath + " нет AttackController");
            PrefabUtility.UnloadPrefabContents(contents);
            return;
        }

        SerializedObject serialized = new SerializedObject(controller);

        SerializedProperty prefabProperty = serialized.FindProperty("chargeVfxPrefab");
        if (prefabProperty != null)
            prefabProperty.objectReferenceValue = vfxPrefab;

        // По решению от 2026-11-07 заряд и удар больше не играют анимацию хомяка:
        // огонь и вспышку делает VFX, персонаж остаётся в покое.
        SerializedProperty chargeState = serialized.FindProperty("chargeStateName");
        if (chargeState != null)
            chargeState.stringValue = string.Empty;

        SerializedProperty chargedState = serialized.FindProperty("chargedStateName");
        if (chargedState != null)
            chargedState.stringValue = string.Empty;

        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, name);
    }
}

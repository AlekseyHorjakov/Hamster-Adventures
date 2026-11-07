using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Собирает петлю зарядки «присед с кулаком в землю» из четырёх кадров,
/// которые пользователь нарисовал 2026-11-07.
///
/// Кадры уже нормализованы под тот же холст 960×900 и ту же линию ступней,
/// что и остальные анимации: 730 для Хомяка, 742 для Хомячихи.
///
/// Запуск: Tools/Hamster/Build Charge Ground или
/// Unity.exe -batchmode -quit -executeMethod ChargeGroundBuilder.BuildAll
/// </summary>
public static class ChargeGroundBuilder
{
    private const float HamsterPpu = 93.9f;
    private const float GirlPpu = 98f;
    private const float ClipSeconds = 0.36f;

    [MenuItem("Tools/Hamster/Build Charge Ground")]
    public static void BuildAll()
    {
        Sprite[] hamsterFrames = ImportFrames(
            "Assets/Art/Hamster/ChargeGround", "Hamster_ChargeGround", 4, HamsterPpu);

        Sprite[] girlFrames = ImportFrames(
            "Assets/Art/HamsterGirl/ChargeGround", "Girl_ChargeGround", 4, GirlPpu);

        if (hamsterFrames.Length != 4 || girlFrames.Length != 4)
        {
            Debug.LogError("[ChargeGround] Не хватает кадров: ожидалось по 4");
            return;
        }

        // Четвёртый кадр — «выдох» для петли; в игре заряд проигрывается один раз
        // и держит позу пика, поэтому в клип идут три кадра.
        ReplaceClip("Assets/Animations/Hamster/Hamster_Charge.anim", hamsterFrames.Take(3).ToArray(), false);
        ReplaceClip("Assets/Animations/HamsterGirl/HG_Charge.anim", girlFrames.Take(3).ToArray(), false);

        WireCharacter("Assets/Prefabs/Hamster.prefab", "Charge");
        WireCharacter("Assets/Prefabs/HamsterGirl.prefab", "Charge");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ChargeGround] Стойка зарядки собрана: 3 кадра, " + ClipSeconds + " с, без цикла");
    }

    private static Sprite[] ImportFrames(string folder, string prefix, int count, float ppu)
    {
        List<Sprite> frames = new List<Sprite>(count);

        for (int i = 1; i <= count; i++)
        {
            string path = folder + "/" + prefix + "_" + i.ToString("00") + ".png";

            if (!File.Exists(path))
            {
                Debug.LogError("[ChargeGround] Нет файла " + path);
                continue;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null)
                Debug.LogError("[ChargeGround] Не удалось загрузить спрайт " + path);
            else
                frames.Add(sprite);
        }

        return frames.ToArray();
    }

    private static void ReplaceClip(string path, Sprite[] frames, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            Debug.LogError("[ChargeGround] Нет клипа " + path);
            return;
        }

        EditorCurveBinding binding = default;
        bool found = false;

        foreach (EditorCurveBinding candidate in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            if (candidate.propertyName == "m_Sprite")
            {
                binding = candidate;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogError("[ChargeGround] В клипе нет кривой m_Sprite: " + path);
            return;
        }

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = ClipSeconds * i / frames.Length,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = ClipSeconds;
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
    }

    private static void WireCharacter(string prefabPath, string stateName)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        AttackController controller = contents.GetComponent<AttackController>();

        if (controller == null)
        {
            Debug.LogWarning("[ChargeGround] В " + prefabPath + " нет AttackController");
            PrefabUtility.UnloadPrefabContents(contents);
            return;
        }

        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty state = serialized.FindProperty("chargeStateName");
        if (state != null)
            state.stringValue = stateName;

        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}

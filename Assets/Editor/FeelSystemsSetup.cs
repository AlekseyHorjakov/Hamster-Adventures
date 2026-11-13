using UnityEditor;
using UnityEngine;

/// <summary>
/// Однократная настройка prefab-ов и HitData для Character Feel / Combat Feel.
/// Запускается через Tools → Hamster → Apply Feel Setup.
/// </summary>
public static class FeelSystemsSetup
{
    [MenuItem("Tools/Hamster/Apply Feel Setup")]
    public static void Apply()
    {
        ConfigurePlayerPrefab("Assets/Prefabs/Hamster.prefab");
        ConfigurePlayerPrefab("Assets/Prefabs/HamsterGirl.prefab");
        ConfigureHyenaPrefab();
        ConfigureHitDataAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Feel] Настройка Character Feel / Combat Feel применена.");
    }

    private static void ConfigurePlayerPrefab(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Health health = root.GetComponent<Health>();
            HamsterMovement movement = root.GetComponent<HamsterMovement>();
            AttackController attack = root.GetComponent<AttackController>();
            PlayerCombatState combatState = root.GetComponent<PlayerCombatState>();
            HitReaction reaction = root.GetComponent<HitReaction>();
            CharacterMotionVisual motionVisual = root.GetComponent<CharacterMotionVisual>();

            if (health != null)
            {
                SerializedObject healthSo = new SerializedObject(health);
                healthSo.FindProperty("invulnerabilityAfterHit").floatValue = 0.55f;
                healthSo.ApplyModifiedPropertiesWithoutUndo();
            }

            if (reaction == null)
                reaction = root.AddComponent<HitReaction>();

            if (motionVisual == null)
                motionVisual = root.AddComponent<CharacterMotionVisual>();

            Transform visual = root.transform.Find("Visual");
            SpriteRenderer sprite = visual != null ? visual.GetComponent<SpriteRenderer>() : null;

            SerializedObject motionSo = new SerializedObject(motionVisual);
            motionSo.FindProperty("movement").objectReferenceValue = movement;
            motionSo.FindProperty("attack").objectReferenceValue = attack;
            motionSo.FindProperty("hitReaction").objectReferenceValue = reaction;
            motionSo.FindProperty("combatState").objectReferenceValue = combatState;
            motionSo.FindProperty("visual").objectReferenceValue = visual;
            motionSo.FindProperty("spriteRenderer").objectReferenceValue = sprite;
            motionSo.FindProperty("dustPrefab").objectReferenceValue = Resources.Load<GameObject>("HitSpark");
            motionSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject attackSo = new SerializedObject(attack);
            SerializedProperty durations = attackSo.FindProperty("attackDurations");
            durations.arraySize = 3;
            durations.GetArrayElementAtIndex(0).floatValue = 0.2638889f;
            durations.GetArrayElementAtIndex(1).floatValue = 0.225f;
            durations.GetArrayElementAtIndex(2).floatValue = 0.6f;

            SerializedProperty cancels = attackSo.FindProperty("cancelTimes");
            cancels.arraySize = 3;
            cancels.GetArrayElementAtIndex(0).floatValue = 0.22f;
            cancels.GetArrayElementAtIndex(1).floatValue = 0.19f;
            cancels.GetArrayElementAtIndex(2).floatValue = 0.42f;
            attackSo.FindProperty("airDuration").floatValue = 0.225f;
            attackSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureHyenaPrefab()
    {
        const string path = "Assets/Prefabs/Hyena.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Health health = root.GetComponent<Health>();
            if (health != null)
            {
                SerializedObject so = new SerializedObject(health);
                so.FindProperty("invulnerabilityAfterHit").floatValue = 0.08f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureHitDataAssets()
    {
        ConfigureHit("Assets/Combat/HitData_Light1.asset", HitImpactWeight.Light,
            new Color(1f, 1f, 1f, 1f), 0.07f,
            new Color(1f, 0.9f, 0.65f, 1f), 0.9f, 1,
            0.08f, 0.08f, 5f, 0.12f);

        ConfigureHit("Assets/Combat/HitData_Light2.asset", HitImpactWeight.Medium,
            new Color(1f, 0.95f, 0.85f, 1f), 0.08f,
            new Color(1f, 0.82f, 0.45f, 1f), 1f, 1,
            0.11f, 0.10f, 8f, 0.14f);

        ConfigureHit("Assets/Combat/HitData_Strong.asset", HitImpactWeight.Launch,
            new Color(1f, 0.7f, 0.35f, 1f), 0.12f,
            new Color(1f, 0.55f, 0.2f, 1f), 1.35f, 2,
            0.28f, 0.18f, 22f, 0.22f);

        ConfigureHit("Assets/Combat/HitData_Air.asset", HitImpactWeight.Medium,
            new Color(1f, 0.95f, 0.75f, 1f), 0.08f,
            new Color(1f, 0.85f, 0.55f, 1f), 1.05f, 1,
            0.12f, 0.10f, 10f, 0.16f);

        ConfigureHit("Assets/Combat/HitData_Charged.asset", HitImpactWeight.Launch,
            new Color(1f, 0.55f, 0.2f, 1f), 0.15f,
            new Color(1f, 0.42f, 0.12f, 1f), 1.55f, 3,
            0.38f, 0.24f, 28f, 0.26f);

        ConfigureHit("Assets/Combat/HitData_HyenaBite.asset", HitImpactWeight.Medium,
            new Color(1f, 0.45f, 0.3f, 1f), 0.08f,
            new Color(1f, 0.45f, 0.25f, 1f), 1f, 1,
            0.12f, 0.10f, 10f, 0.16f);

        ConfigureHit("Assets/Combat/HitData_ThrownPartner.asset", HitImpactWeight.Launch,
            new Color(1f, 0.8f, 0.5f, 1f), 0.12f,
            new Color(1f, 0.65f, 0.25f, 1f), 1.35f, 2,
            0.3f, 0.2f, 20f, 0.22f);
    }

    private static void ConfigureHit(string path, HitImpactWeight impact,
        Color flashColor, float flashDuration, Color sparkTint, float sparkScale, int sparkCount,
        float shakeStrength, float shakeDuration, float reactionTilt, float recovery)
    {
        HitData data = AssetDatabase.LoadAssetAtPath<HitData>(path);
        if (data == null)
        {
            Debug.LogError("[Feel] Не найден HitData: " + path);
            return;
        }

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("impactWeight").enumValueIndex = (int)impact;
        so.FindProperty("hitFlashColor").colorValue = flashColor;
        so.FindProperty("hitFlashDuration").floatValue = flashDuration;
        so.FindProperty("sparkTint").colorValue = sparkTint;
        so.FindProperty("sparkScale").floatValue = sparkScale;
        so.FindProperty("sparkCount").intValue = sparkCount;
        so.FindProperty("cameraShakeStrength").floatValue = shakeStrength;
        so.FindProperty("cameraShakeDuration").floatValue = shakeDuration;
        so.FindProperty("reactionTilt").floatValue = reactionTilt;
        so.FindProperty("reactionRecovery").floatValue = recovery;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
    }
}

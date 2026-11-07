using UnityEditor;
using UnityEngine;

/// <summary>
/// Точечная доводка боевых таймингов после плейтеста 2026-11-07:
/// заряд растянут до 3 секунд, а у финишёров кадр бота/сумочки раньше
/// уходил в конец клипа и обрывался переходом в покой.
///
/// Запуск из меню Tools/Hamster/Apply Combat Timing или в batchmode:
/// Unity.exe -batchmode -quit -projectPath ... -executeMethod CombatTuning.ApplyAllAndRebuildVfx
/// </summary>
public static class CombatTuning
{
    private const float ChargeSeconds = 3f;

    [MenuItem("Tools/Hamster/Apply Combat Timing")]
    public static void ApplyAll()
    {
        FixFinisher("Assets/Animations/Hamster/Spin.anim");
        FixFinisher("Assets/Animations/HamsterGirl/HGSpin.anim");
        FixCharacters();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Tuning] Заряд 3 с, финишёры удерживают кадр удара");
    }

    /// <summary>Пересобрать VFX и сразу применить тайминги одним запуском.</summary>
    public static void ApplyAllAndRebuildVfx()
    {
        ChargeVFXBuilder.BuildAll();
        ApplyAll();
    }

    /// <summary>
    /// Кадр удара у финишёра стоит в конце клипа (0.28 с), а хитбокс
    /// включается на 0.16 с — враг улетал раньше, чем появлялся ботинок
    /// или сумочка. Ставим кадр и хитбокс на 0.24 с и держим кадр до 0.40 с.
    /// </summary>
    private static void FixFinisher(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            Debug.LogError("[Tuning] Нет клипа " + path);
            return;
        }

        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (EditorCurveBinding binding in bindings)
        {
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys == null || keys.Length == 0)
                continue;

            int last = keys.Length - 1;
            if (Mathf.Abs(keys[last].time - 0.28f) < 0.011f)
            {
                keys[last].time = 0.24f;
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            }
        }

        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
        for (int i = 0; i < events.Length; i++)
        {
            if (events[i].functionName == "AnimEvent_EnableHitbox" && Mathf.Abs(events[i].time - 0.16f) < 0.011f)
                events[i].time = 0.24f;

            if (events[i].functionName == "AnimEvent_DisableHitbox" && Mathf.Abs(events[i].time - 0.24f) < 0.011f)
                events[i].time = 0.34f;
        }

        AnimationUtility.SetAnimationEvents(clip, events);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = 0.40f;
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
    }

    private static void FixCharacters()
    {
        string[] prefabs =
        {
            "Assets/Prefabs/Hamster.prefab",
            "Assets/Prefabs/HamsterGirl.prefab"
        };

        foreach (string path in prefabs)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            AttackController controller = contents.GetComponent<AttackController>();

            if (controller == null)
            {
                Debug.LogWarning("[Tuning] В " + path + " нет AttackController");
                PrefabUtility.UnloadPrefabContents(contents);
                continue;
            }

            SerializedObject serialized = new SerializedObject(controller);

            SerializedProperty chargeTime = serialized.FindProperty("minChargeTime");
            if (chargeTime != null)
                chargeTime.floatValue = ChargeSeconds;

            SerializedProperty durations = serialized.FindProperty("attackDurations");
            if (durations != null && durations.arraySize >= 3)
                durations.GetArrayElementAtIndex(2).floatValue = 0.42f;

            SerializedProperty cancels = serialized.FindProperty("cancelTimes");
            if (cancels != null && cancels.arraySize >= 3)
                cancels.GetArrayElementAtIndex(2).floatValue = 0.30f;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}

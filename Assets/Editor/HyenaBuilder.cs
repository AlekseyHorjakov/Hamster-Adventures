using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Собирает гиену-прототип: здоровье, физика, HitReaction, хитбокс укуса,
/// HyenaAI и Animator с анимациями ходьбы, урона и смерти.
/// Меню: Tools -> Hamster -> Build Hyena Prototype.
/// </summary>
public static class HyenaBuilder
{
    private const string PrefabPath = "Assets/Prefabs/Hyena.prefab";
    private const string HitDataPath = "Assets/Combat/HitData_HyenaBite.asset";
    private const string ScenePath = "Assets/Scenes/CombatTest.unity";
    private const string AttackFramePath = "Assets/Art/Enemies/Hyena/Attack/Hyena_Attack_01.png";
    private const string ControllerPath = "Assets/Animations/Enemies/Hyena/Hyena.controller";

    [MenuItem("Tools/Hamster/Build Hyena Prototype")]
    public static void BuildAll()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Combat");

        // Анимации пересобираются первыми: префаб сразу получает актуальный контроллер.
        HyenaAnimationBuilder.BuildAll();

        HitData bite = CreateOrUpdateBite();
        GameObject prefab = BuildPrefab(bite);
        PlaceInCombatTest(prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Hyena] Прототип собран: здоровье 45, укус 10, анимации Run/Attack/Hurt/Death.");
    }

    private static HitData CreateOrUpdateBite()
    {
        HitData bite = AssetDatabase.LoadAssetAtPath<HitData>(HitDataPath);

        if (bite == null)
        {
            bite = ScriptableObject.CreateInstance<HitData>();
            AssetDatabase.CreateAsset(bite, HitDataPath);
        }

        bite.damage = 10f;
        bite.knockback = 8f;
        bite.hitStop = 0.06f;
        bite.hitStun = 0.32f;
        bite.launchVelocity = Vector2.zero;
        bite.hitboxSize = new Vector2(1.3f, 0.9f);
        bite.hitboxOffset = new Vector2(0.95f, 0f);

        EditorUtility.SetDirty(bite);
        return bite;
    }

    private static GameObject BuildPrefab(HitData bite)
    {
        GameObject root = new GameObject("Hyena");
        root.layer = 0;

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.mass = 2f;
        body.gravityScale = 2.5f;
        body.linearDamping = 0.1f;
        body.angularDamping = 0.05f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.82f, 1.5f);
        collider.offset = new Vector2(0f, -0.08f);

        Health health = root.AddComponent<Health>();
        SetPrivate(health, "maxHealth", 45f);
        SetPrivate(health, "applyKnockback", true);
        SetPrivate(health, "invulnerabilityAfterHit", 0.05f);
        SetPrivate(health, "logDamage", false);

        HitReaction reaction = root.AddComponent<HitReaction>();
        SetPrivate(reaction, "flashColor", Color.white);
        SetPrivate(reaction, "flashDuration", 0.1f);
        SetPrivate(reaction, "hitScale", 1.08f);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        // Спрайты нарезаны с пивотом внизу по центру, поэтому ставим низ
        // визуала на низ коллайдера — это линия ступней.
        visual.transform.localPosition = new Vector3(0f, collider.offset.y - collider.size.y * 0.5f, 0f);

        SpriteRenderer sprite = visual.AddComponent<SpriteRenderer>();
        sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AttackFramePath);
        sprite.sortingLayerName = "Characters";
        sprite.sortingOrder = 0;

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        GameObject hitboxObject = new GameObject("Hitbox");
        hitboxObject.transform.SetParent(root.transform, false);
        Hitbox hitbox = hitboxObject.AddComponent<Hitbox>();
        SetPrivate(hitbox, "targetMask", (LayerMask)(1 << 3));

        HyenaAI ai = root.AddComponent<HyenaAI>();
        SetPrivate(ai, "biteHit", bite);
        SetPrivate(ai, "hitbox", hitbox);
        SetPrivate(ai, "animator", animator);
        SetPrivate(ai, "idleStateName", "Idle");
        SetPrivate(ai, "walkStateName", "Run");
        SetPrivate(ai, "attackStateName", "Attack");
        SetPrivate(ai, "attackWindup", 0.16f);
        SetPrivate(ai, "attackActive", 0.16f);
        SetPrivate(ai, "attackRecovery", 0.22f);
        SetPrivate(ai, "attackCooldown", 0.55f);
        SetPrivate(ai, "hurtStateName", "Hurt");
        SetPrivate(ai, "deathStateName", "Death");
        SetPrivate(ai, "hurtAnimationDuration", 0.48f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void PlaceInCombatTest(GameObject prefab)
    {
        if (!System.IO.File.Exists(ScenePath))
        {
            Debug.LogWarning("[Hyena] CombatTest.unity не найден, префаб собран без расстановки.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Повторный запуск сборщика не должен плодить копии.
        HyenaAI[] old = Object.FindObjectsByType<HyenaAI>(FindObjectsSortMode.None);
        foreach (HyenaAI hyena in old)
            Object.DestroyImmediate(hyena.gameObject);

        Vector3[] positions =
        {
            new Vector3(8.5f, 1f, 0f),
            new Vector3(12f, 1f, 0f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            instance.name = "Hyena " + (i + 1);
            instance.transform.position = positions[i];
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetPrivate(Object target, string field, object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);

        if (property == null)
        {
            Debug.LogWarning($"[Hyena] Поле {field} не найдено у {target.GetType().Name}");
            return;
        }

        switch (value)
        {
            case float number:
                property.floatValue = number;
                break;
            case bool flag:
                property.boolValue = flag;
                break;
            case int number:
                property.intValue = number;
                break;
            case string text:
                property.stringValue = text;
                break;
            case Color color:
                property.colorValue = color;
                break;
            case LayerMask mask:
                property.intValue = mask.value;
                break;
            case Object reference:
                property.objectReferenceValue = reference;
                break;
            default:
                Debug.LogWarning($"[Hyena] Неподдерживаемый тип значения для {field}");
                return;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, name);
    }
}

using UnityEngine;

/// <summary>
/// Процедурная презентация движения: наклон корпуса, squash при приземлении,
/// пыль и слабая реакция камеры. Не управляет физикой и не блокирует ввод.
/// </summary>
[DisallowMultipleComponent]
public class CharacterMotionVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HamsterMovement movement;
    [SerializeField] private AttackController attack;
    [SerializeField] private HitReaction hitReaction;
    [SerializeField] private PlayerCombatState combatState;
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Lean")]
    [SerializeField] private float referenceSpeed = 15f;
    [SerializeField] private float maxLeanDegrees = 8f;
    [SerializeField] private float leanSmooth = 12f;
    [SerializeField] private float turnLeanMultiplier = 1.35f;

    [Header("Landing Squash")]
    [SerializeField] private float landingSquashY = 0.86f;
    [SerializeField] private float landingStretchX = 1.08f;
    [SerializeField] private float landingSquashDuration = 0.10f;
    [SerializeField] private float minLandingImpact = 6f;

    [Header("Landing Dust")]
    [SerializeField] private GameObject dustPrefab;
    [SerializeField] private Color dustTint = new Color(0.65f, 0.55f, 0.42f, 0.55f);
    [SerializeField] private float dustScale = 0.55f;
    [SerializeField] private float heavyLandingImpact = 16f;

    [Header("Landing Camera")]
    [SerializeField] private float landingCameraShakeStrength = 0.08f;
    [SerializeField] private float landingCameraShakeDuration = 0.08f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 landingBaseScale;
    private float currentLean;
    private float leanVelocity;
    private float squashRemaining;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<HamsterMovement>();
        if (attack == null)
            attack = GetComponent<AttackController>();
        if (hitReaction == null)
            hitReaction = GetComponent<HitReaction>();
        if (combatState == null)
            combatState = GetComponent<PlayerCombatState>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (visual == null && spriteRenderer != null)
            visual = spriteRenderer.transform;

        if (visual != null)
        {
            baseLocalPosition = visual.localPosition;
            baseLocalRotation = visual.localRotation;
            landingBaseScale = visual.localScale;
        }
    }

    private void OnEnable()
    {
        if (movement != null)
        {
            movement.Landed += OnLanded;
            movement.DirectionChanged += OnDirectionChanged;
        }
    }

    private void OnDisable()
    {
        if (movement != null)
        {
            movement.Landed -= OnLanded;
            movement.DirectionChanged -= OnDirectionChanged;
        }

        RestoreVisual();
    }

    private void LateUpdate()
    {
        if (visual == null || movement == null || movement.Body == null)
            return;

        if (combatState != null && (combatState.IsDowned || combatState.IsDead))
        {
            RestoreVisual();
            return;
        }

        // Пока HitReaction ведёт полёт/наклон, не перебиваем его позу.
        if (hitReaction != null && hitReaction.IsReacting)
            return;

        UpdateLean();
        UpdateSquash();
    }

    private void UpdateLean()
    {
        float horizontalSpeed = movement.HorizontalSpeed;
        float speed01 = Mathf.Clamp01(Mathf.Abs(horizontalSpeed) / Mathf.Max(0.01f, referenceSpeed));

        float direction = Mathf.Abs(horizontalSpeed) > 0.05f
            ? Mathf.Sign(horizontalSpeed)
            : Mathf.Sign(movement.MoveInput);

        float targetLean = -direction * maxLeanDegrees * speed01;

        if (movement.Grounded &&
            Mathf.Abs(movement.MoveInput) > 0.01f &&
            Mathf.Abs(horizontalSpeed) > 0.15f &&
            Mathf.Sign(movement.MoveInput) != Mathf.Sign(horizontalSpeed))
        {
            targetLean *= turnLeanMultiplier;
        }

        if (attack != null && attack.IsAttacking)
            targetLean *= 0.35f;

        currentLean = Mathf.SmoothDamp(
            currentLean,
            targetLean,
            ref leanVelocity,
            1f / Mathf.Max(0.01f, leanSmooth),
            Mathf.Infinity,
            Time.deltaTime
        );

        visual.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, currentLean);

        // Небольшой вертикальный offset подчёркивает перенос веса в воздухе,
        // не вмешиваясь в позицию, которую задаёт физика.
        float verticalOffset = Mathf.Clamp(movement.VerticalSpeed * 0.002f, -0.035f, 0.035f);
        visual.localPosition = baseLocalPosition + Vector3.up * verticalOffset;
    }

    private void UpdateSquash()
    {
        if (squashRemaining <= 0f)
            return;

        squashRemaining = Mathf.Max(0f, squashRemaining - Time.deltaTime);
        float progress = 1f - squashRemaining / Mathf.Max(0.01f, landingSquashDuration);
        float pulse = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);

        float scaleX = Mathf.LerpUnclamped(1f, landingStretchX, pulse);
        float scaleY = Mathf.LerpUnclamped(1f, landingSquashY, pulse);

        visual.localScale = new Vector3(
            landingBaseScale.x * scaleX,
            landingBaseScale.y * scaleY,
            landingBaseScale.z
        );

        if (squashRemaining <= 0f)
            visual.localScale = landingBaseScale;
    }

    private void OnLanded(float impactSpeed)
    {
        if (visual == null)
            return;

        if (squashRemaining <= 0f)
            landingBaseScale = visual.localScale;

        squashRemaining = landingSquashDuration;

        if (impactSpeed >= minLandingImpact)
            SpawnDust(impactSpeed);

        if (impactSpeed >= heavyLandingImpact)
            CameraFollow.Shake(landingCameraShakeStrength, landingCameraShakeDuration);
    }

    private void OnDirectionChanged(int direction)
    {
        // Разворот уже читается через lean; отдельная анимация не нужна.
        if (Mathf.Abs(currentLean) < 0.5f)
            currentLean = -direction * 1.5f;
    }

    private void SpawnDust(float impactSpeed)
    {
        GameObject prefab = dustPrefab;

        if (prefab == null)
            prefab = Resources.Load<GameObject>("HitSpark");

        if (prefab == null || spriteRenderer == null)
            return;

        Bounds bounds = spriteRenderer.bounds;
        Vector3 position = new Vector3(bounds.center.x, bounds.min.y + 0.05f, transform.position.z);
        GameObject dust = Instantiate(prefab, position, Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f)));

        float strength = Mathf.InverseLerp(minLandingImpact, heavyLandingImpact + 6f, impactSpeed);
        dust.transform.localScale = new Vector3(
            dustScale * Mathf.Lerp(0.75f, 1.35f, strength),
            dustScale * Mathf.Lerp(0.35f, 0.65f, strength),
            1f
        );

        HitSpark spark = dust.GetComponent<HitSpark>();
        if (spark != null)
            spark.Play(strength, dustTint);

        SpriteRenderer dustRenderer = dust.GetComponent<SpriteRenderer>();
        if (dustRenderer != null)
            dustRenderer.sortingLayerName = "FX";
    }

    private void RestoreVisual()
    {
        if (visual == null)
            return;

        currentLean = 0f;
        leanVelocity = 0f;
        squashRemaining = 0f;
        visual.localPosition = baseLocalPosition;
        visual.localRotation = baseLocalRotation;
        visual.localScale = landingBaseScale;
    }
}

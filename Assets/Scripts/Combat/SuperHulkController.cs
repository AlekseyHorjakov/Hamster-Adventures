using System.Collections;
using UnityEngine;

/// <summary>
/// Суперудар Хомяка: при полной шкале превращает его в монстра на 7 секунд.
/// Отдельный HulkVisual проигрывает большие кадры, а обычный герой скрыт.
/// </summary>
[DisallowMultipleComponent]
public class SuperHulkController : MonoBehaviour
{
    private enum FormState
    {
        Normal,
        TransformIn,
        Hulk,
        TransformOut
    }

    [Header("Ссылки")]
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private SuperMeter meter;
    [SerializeField] private HamsterMovement movement;
    [SerializeField] private AttackController attack;
    [SerializeField] private Health health;
    [SerializeField] private PlayerCombatState combatState;
    [SerializeField] private IdleSpecialController idleSpecial;
    [SerializeField] private PartnerThrowController partnerThrow;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private SpriteRenderer mainRenderer;
    [SerializeField] private GameObject hulkVisual;
    [SerializeField] private SpriteRenderer hulkRenderer;
    [SerializeField] private GameObject transformVfxPrefab;

    [Header("Форма халка")]
    [SerializeField] private float hulkDuration = 10f;
    [SerializeField] private float transformInDuration = 0.7f;
    [SerializeField] private float transformOutDuration = 0.56f;
    [Tooltip("Скорость передвижения в форме халка")]
    [SerializeField] private float speedMultiplier = 0.55f;
    [Tooltip("Сила прыжка в форме халка")]
    [SerializeField] private float jumpMultiplier = 0.70f;
    [Tooltip("Множитель урона: обычные 8/10 урона превращаются в 24/30")]
    [SerializeField] private float hulkDamageMultiplier = 3f;

    [Header("Анимации")]
    [SerializeField] private string normalIdleState = "Idle 0";
    [SerializeField] private string transformInState = "HulkTransformIn";
    [SerializeField] private string transformOutState = "HulkTransformOut";
    [SerializeField] private string hulkIdleState = "HulkIdle";
    [SerializeField] private string hulkRunState = "HulkRun";
    [SerializeField] private string hulkJumpState = "HulkJump";
    [SerializeField] private string[] hulkAttackStates = { "HulkPunch" };
    [SerializeField] private float[] hulkAttackDurations = { 0.56f };
    [SerializeField] private float[] hulkCancelTimes = { 0.42f };
    [SerializeField] private float[] hulkStepSpeeds = { 1.2f };
    [SerializeField] private float hulkMovementLock = 0.24f;
    [SerializeField] private float hulkComboResetTime = 0.6f;

    private FormState state = FormState.Normal;
    private Coroutine transformRoutine;
    private float hulkEndsAt;
    private bool idleSpecialWasEnabled;
    private bool throwWasEnabled;

    private void Awake()
    {
        if (input == null) input = GetComponent<PlayerInputReader>();
        if (meter == null) meter = GetComponent<SuperMeter>();
        if (movement == null) movement = GetComponent<HamsterMovement>();
        if (attack == null) attack = GetComponent<AttackController>();
        if (health == null) health = GetComponent<Health>();
        if (combatState == null) combatState = GetComponent<PlayerCombatState>();
        if (idleSpecial == null) idleSpecial = GetComponent<IdleSpecialController>();
        if (partnerThrow == null) partnerThrow = GetComponent<PartnerThrowController>();
        if (animator == null) animator = GetComponent<Animator>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (mainRenderer == null) mainRenderer = GetComponentInChildren<SpriteRenderer>();

        if (hulkVisual != null)
            hulkVisual.SetActive(false);
    }

    private void Update()
    {
        if (state == FormState.Normal)
        {
            if (input != null && input.SuperPressed && CanStartHulk())
                StartTransformIn();

            return;
        }

        if (state == FormState.Hulk)
        {
            if (health != null && !health.IsAlive)
            {
                AbortHulkForm();
                return;
            }

            if (Time.time >= hulkEndsAt)
            {
                StartTransformOut();
                return;
            }

            UpdateHulkAnimation();
        }
    }

    private bool CanStartHulk()
    {
        if (meter == null || !meter.IsFull || hulkVisual == null || mainRenderer == null)
            return false;

        if (state != FormState.Normal || !movement.Grounded)
            return false;

        if (health != null && !health.IsAlive)
            return false;

        if (combatState != null && (combatState.IsDowned || combatState.IsDead))
            return false;

        if (partnerThrow != null &&
            (partnerThrow.IsCarryingPartner || partnerThrow.IsBeingCarried || partnerThrow.IsFlying))
            return false;

        return true;
    }

    private void StartTransformIn()
    {
        if (!meter.ConsumeIfFull())
            return;

        state = FormState.TransformIn;
        meter.SetGainLocked(true);
        attack.CancelCurrentAttack();

        idleSpecialWasEnabled = idleSpecial != null && idleSpecial.enabled;
        throwWasEnabled = partnerThrow != null && partnerThrow.enabled;

        if (idleSpecial != null) idleSpecial.enabled = false;
        if (partnerThrow != null) partnerThrow.enabled = false;

        movement.SetControlLocked(true);
        attack.enabled = false;

        if (body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);

        mainRenderer.enabled = false;
        hulkVisual.SetActive(true);
        if (hulkRenderer != null) hulkRenderer.enabled = true;
        AlignHulkVisualToGround();
        playState(transformInState);

        SpawnTransformVfx();
        CameraFollow.Shake(0.55f, 0.35f);
        HitStop.Play(0.07f);

        transformRoutine = StartCoroutine(TransformInRoutine());
    }

    private IEnumerator TransformInRoutine()
    {
        yield return new WaitForSeconds(transformInDuration);
        transformRoutine = null;
        state = FormState.Hulk;
        hulkEndsAt = Time.time + hulkDuration;

        movement.SetRuntimeMovementMultipliers(speedMultiplier, jumpMultiplier);
        attack.SetRuntimeAttackProfile(
            hulkAttackStates,
            hulkAttackDurations,
            hulkCancelTimes,
            hulkStepSpeeds,
            hulkMovementLock,
            hulkComboResetTime,
            hulkDamageMultiplier);

        attack.enabled = true;
        movement.SetControlLocked(false);
        playState(hulkIdleState);
    }

    private void StartTransformOut()
    {
        if (state != FormState.Hulk)
            return;

        state = FormState.TransformOut;
        attack.CancelCurrentAttack();
        attack.enabled = false;
        movement.SetControlLocked(true);
        movement.SetRuntimeMovementMultipliers(1f, 1f);

        if (body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);

        playState(transformOutState);
        SpawnTransformVfx();
        CameraFollow.Shake(0.4f, 0.25f);

        transformRoutine = StartCoroutine(TransformOutRoutine());
    }

    private IEnumerator TransformOutRoutine()
    {
        yield return new WaitForSeconds(transformOutDuration);
        FinishHulkForm();
    }

    private void FinishHulkForm()
    {
        if (transformRoutine != null)
        {
            StopCoroutine(transformRoutine);
            transformRoutine = null;
        }

        attack.CancelCurrentAttack();
        attack.RestoreRuntimeAttackProfile();
        movement.SetRuntimeMovementMultipliers(1f, 1f);

        if (hulkRenderer != null) hulkRenderer.enabled = false;
        if (hulkVisual != null) hulkVisual.SetActive(false);
        if (mainRenderer != null) mainRenderer.enabled = true;

        if (idleSpecial != null) idleSpecial.enabled = idleSpecialWasEnabled;
        if (partnerThrow != null) partnerThrow.enabled = throwWasEnabled;

        if (health != null && health.IsAlive &&
            (combatState == null || (!combatState.IsDowned && !combatState.IsDead)))
        {
            attack.enabled = true;
            movement.SetControlLocked(false);
            playState(normalIdleState);
        }

        meter.SetGainLocked(false);
        state = FormState.Normal;
    }

    private void AbortHulkForm()
    {
        if (transformRoutine != null)
        {
            StopCoroutine(transformRoutine);
            transformRoutine = null;
        }

        attack.CancelCurrentAttack();
        attack.RestoreRuntimeAttackProfile();
        movement.SetRuntimeMovementMultipliers(1f, 1f);

        if (hulkRenderer != null) hulkRenderer.enabled = false;
        if (hulkVisual != null) hulkVisual.SetActive(false);
        if (mainRenderer != null) mainRenderer.enabled = true;

        if (idleSpecial != null) idleSpecial.enabled = idleSpecialWasEnabled;
        if (partnerThrow != null) partnerThrow.enabled = throwWasEnabled;

        meter.SetGainLocked(false);
        state = FormState.Normal;
    }

    private void UpdateHulkAnimation()
    {
        if (animator == null || attack.IsAttacking)
            return;

        if (hulkRenderer != null && mainRenderer != null)
            hulkRenderer.flipX = mainRenderer.flipX;

        string desired;
        if (movement != null && !movement.Grounded)
            desired = hulkJumpState;
        else if (movement != null &&
                 (Mathf.Abs(movement.MoveInput) > 0.01f ||
                  (body != null && Mathf.Abs(body.linearVelocity.x) > 0.08f)))
            desired = hulkRunState;
        else
            desired = hulkIdleState;

        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(desired))
            playState(desired);
    }

    /// <summary>
    /// Кадры халка нормализованы на холст 512 px с отступом 64 px под ступнями.
    /// Ставим низ видимой фигуры ровно на низ коллайдера героя.
    /// </summary>
    private void AlignHulkVisualToGround()
    {
        if (hulkVisual == null || bodyCollider == null)
            return;

        const float canvasHeightPixels = 512f;
        const float feetInsetPixels = 64f;
        const float ppu = 25f;

        float worldScaleY = hulkVisual.transform.lossyScale.y;
        float fullHeightWorld = canvasHeightPixels / ppu * worldScaleY;
        float feetInsetWorld = feetInsetPixels / ppu * worldScaleY;
        float desiredCenterY = bodyCollider.bounds.min.y + fullHeightWorld * 0.5f - feetInsetWorld;

        Vector3 position = hulkVisual.transform.position;
        position.y = desiredCenterY;
        hulkVisual.transform.position = position;
    }

    private void SpawnTransformVfx()
    {
        if (transformVfxPrefab == null)
            return;

        GameObject vfxObject = Instantiate(transformVfxPrefab, transform.position, Quaternion.identity);
        HulkTransformVFXController vfx = vfxObject.GetComponent<HulkTransformVFXController>();
        if (vfx != null)
            vfx.Play(transform, mainRenderer, transformInDuration);
    }

    private void playState(string stateName)
    {
        if (animator != null && !string.IsNullOrEmpty(stateName))
            animator.Play(stateName, 0, 0f);
    }
}

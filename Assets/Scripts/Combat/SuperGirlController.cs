using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Суперудар Хомячихи: 10 секунд усиленных атак и фиолетовых молний.
/// Во время ударов во все стороны расходится звуковая волна, которая
/// наносит 50% урона и оглушает врагов на 2 секунды.
/// </summary>
[DisallowMultipleComponent]
public class SuperGirlController : MonoBehaviour
{
    private enum State
    {
        Normal,
        Activating,
        Active
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
    [SerializeField] private SpriteRenderer mainRenderer;
    [SerializeField] private GameObject superVisual;
    [SerializeField] private SpriteRenderer superRenderer;
    [SerializeField] private GameObject auraPrefab;
    [SerializeField] private GameObject soundWavePrefab;
    [SerializeField] private GameObject dizzyStarsPrefab;

    [Header("Суперспособность")]
    [SerializeField] private float superDuration = 10f;
    [SerializeField] private float activationLock = 0.55f;
    [SerializeField] private float damageMultiplier = 3f;
    [SerializeField] private float waveDamage = 12f;
    [SerializeField] private float waveRadius = 6f;
    [SerializeField] private float waveStunDuration = 2f;
    [SerializeField] private float waveHitStop = 0.04f;

    [Header("Анимации")]
    [SerializeField] private string normalIdleState = "HGIdle";
    [SerializeField] private string normalRunState = "HGRun";
    [SerializeField] private string normalJumpState = "HGJump";
    [SerializeField] private string superIdleState = "GirlSuperIdle";

    private State state = State.Normal;
    private Coroutine activationRoutine;
    private float superEndsAt;
    private GirlSuperAuraVFXController aura;
    private bool wasStriking;
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
        if (mainRenderer == null) mainRenderer = GetComponentInChildren<SpriteRenderer>();

        if (superVisual != null)
            superVisual.SetActive(false);
    }

    private void Update()
    {
        if (state == State.Normal)
        {
            if (input != null && input.SuperPressed && CanStart())
                StartActivation();
            return;
        }

        if (state == State.Active)
        {
            if (health != null && !health.IsAlive)
            {
                EndSuper(false);
                return;
            }

            if (Time.time >= superEndsAt)
                EndSuper(true);
        }
    }

    private void LateUpdate()
    {
        if (state != State.Active)
            return;

        bool striking = attack != null && attack.IsStriking;
        if (striking && !wasStriking)
            EmitSoundWave();
        wasStriking = striking;

        UpdateVisualsAndAnimation();
    }

    private bool CanStart()
    {
        if (meter == null || !meter.IsFull || superVisual == null || mainRenderer == null)
            return false;

        if (state != State.Normal || movement == null || !movement.Grounded)
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

    private void StartActivation()
    {
        if (!meter.ConsumeIfFull())
            return;

        state = State.Activating;
        meter.SetGainLocked(true);
        attack.CancelCurrentAttack();
        attack.enabled = false;

        idleSpecialWasEnabled = idleSpecial != null && idleSpecial.enabled;
        throwWasEnabled = partnerThrow != null && partnerThrow.enabled;
        if (idleSpecial != null) idleSpecial.enabled = false;
        if (partnerThrow != null) partnerThrow.enabled = false;

        movement.SetControlLocked(true);
        if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);

        mainRenderer.enabled = false;
        superVisual.SetActive(true);
        if (superRenderer != null) superRenderer.enabled = true;
        animator.Play(superIdleState, 0, 0f);
        animator.Update(0f);
        AlignSuperVisualToGround();

        if (auraPrefab != null)
        {
            GameObject auraObject = Instantiate(auraPrefab, transform.position, Quaternion.identity);
            aura = auraObject.GetComponent<GirlSuperAuraVFXController>();
            if (aura != null)
                aura.Play(transform, mainRenderer, superDuration + activationLock);
        }

        SpawnWaveVfx();
        CameraFollow.Shake(0.45f, 0.3f);
        HitStop.Play(0.05f);

        activationRoutine = StartCoroutine(ActivationRoutine());
    }

    private IEnumerator ActivationRoutine()
    {
        yield return new WaitForSeconds(activationLock);
        activationRoutine = null;
        state = State.Active;
        superEndsAt = Time.time + superDuration;

        attack.enabled = true;
        attack.SetTemporaryDamageMultiplier(damageMultiplier);
        movement.SetControlLocked(false);
    }

    private void EndSuper(bool restoreControl)
    {
        if (state == State.Normal)
            return;

        if (activationRoutine != null)
        {
            StopCoroutine(activationRoutine);
            activationRoutine = null;
        }

        state = State.Normal;
        attack.CancelCurrentAttack();
        attack.ClearTemporaryDamageMultiplier();

        if (aura != null)
        {
            aura.StopAndDestroy();
            aura = null;
        }

        if (superRenderer != null) superRenderer.enabled = false;
        if (superVisual != null) superVisual.SetActive(false);
        if (mainRenderer != null) mainRenderer.enabled = true;

        if (idleSpecial != null) idleSpecial.enabled = idleSpecialWasEnabled;
        if (partnerThrow != null) partnerThrow.enabled = throwWasEnabled;

        if (restoreControl && health != null && health.IsAlive &&
            (combatState == null || (!combatState.IsDowned && !combatState.IsDead)))
        {
            attack.enabled = true;
            movement.SetControlLocked(false);
            animator.Play(normalIdleState, 0, 0f);
        }

        meter.SetGainLocked(false);
        wasStriking = false;
    }

    private void UpdateVisualsAndAnimation()
    {
        bool attacking = attack != null && attack.IsAttacking;
        bool moving = movement != null &&
                      (Mathf.Abs(movement.MoveInput) > 0.01f ||
                       (body != null && Mathf.Abs(body.linearVelocity.x) > 0.08f));
        bool idle = !attacking && movement != null && movement.Grounded && !moving;

        SetSuperVisual(idle);

        if (attacking)
            return;

        if (idle)
        {
            if (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName(superIdleState))
                animator.Play(superIdleState, 0, 0f);
            return;
        }

        if (movement != null && !movement.Grounded)
            PlayNormalState(normalJumpState);
        else if (moving)
            PlayNormalState(normalRunState);
        else
            PlayNormalState(normalIdleState);
    }

    private void PlayNormalState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return;

        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            animator.Play(stateName, 0, 0f);
    }

    private void SetSuperVisual(bool visible)
    {
        if (mainRenderer != null) mainRenderer.enabled = !visible;
        if (superRenderer != null) superRenderer.enabled = visible;

        if (visible && animator != null &&
            !animator.GetCurrentAnimatorStateInfo(0).IsName(superIdleState))
            animator.Play(superIdleState, 0, 0f);
    }

    private void EmitSoundWave()
    {
        SpawnWaveVfx();

        HashSet<Health> hitTargets = new HashSet<Health>();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, waveRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.transform.root == transform.root)
                continue;

            Health target = hit.GetComponentInParent<Health>();
            if (target == null || !target.IsAlive || !hitTargets.Add(target))
                continue;

            if (target.GetComponentInParent<HamsterMovement>() != null)
                continue;

            target.TakeDamage(new DamageInfo
            {
                amount = waveDamage,
                hitPoint = target.transform.position,
                knockback = Vector2.zero,
                hitStop = waveHitStop,
                hitStun = waveStunDuration,
                launchVelocity = Vector2.zero,
                source = gameObject
            });

            SpawnDizzyStars(target);
        }

        CameraFollow.Shake(0.34f, 0.2f);
        HitStop.Play(waveHitStop);
    }

    private void SpawnDizzyStars(Health target)
    {
        if (dizzyStarsPrefab == null || target == null)
            return;

        GameObject stars = Instantiate(dizzyStarsPrefab, target.transform);
        float parentScale = target.transform.lossyScale.y;
        stars.transform.localScale = Vector3.one / Mathf.Max(0.0001f, parentScale);

        SpriteRenderer targetSprite = target.GetComponentInChildren<SpriteRenderer>();
        if (targetSprite != null)
        {
            Bounds bounds = targetSprite.bounds;
            stars.transform.position = new Vector3(bounds.center.x, bounds.max.y - 0.15f, target.transform.position.z);
        }

        Destroy(stars, waveStunDuration);
    }

    private void SpawnWaveVfx()
    {
        if (soundWavePrefab == null)
            return;

        Vector3 center = transform.position + Vector3.up * 1.2f;
        if (mainRenderer != null)
            center = mainRenderer.bounds.center;

        Instantiate(soundWavePrefab, center, Quaternion.identity);
    }

    private void AlignSuperVisualToGround()
    {
        if (superVisual == null || superRenderer == null)
            return;

        const float canvasHeightPixels = 512f;
        const float feetInsetPixels = 64f;
        float groundY = mainRenderer != null ? mainRenderer.bounds.min.y : transform.position.y;
        Bounds bounds = superRenderer.bounds;
        if (bounds.size.y <= 0.01f)
            return;

        float visibleBottom = bounds.min.y + bounds.size.y * (feetInsetPixels / canvasHeightPixels);
        Vector3 position = superVisual.transform.position;
        position.y += groundY - visibleBottom;
        superVisual.transform.position = position;
    }
}

using System.Collections;
using UnityEngine;

/// <summary>
/// Единая реакция цели на попадание: вспышка, i-frame мерцание, флинч,
/// тяжёлое отшатывание или контролируемый запуск. Не управляет AI и не
/// дублирует Health/HitStop/CameraShake.
/// </summary>
[RequireComponent(typeof(Health))]
public class HitReaction : MonoBehaviour
{
    [Header("Вспышка")]
    [SerializeField] private Color defaultFlashColor = Color.white;
    [SerializeField] private float defaultFlashDuration = 0.08f;
    [SerializeField] private float hitScale = 1.06f;

    [Header("Реакция")]
    [SerializeField] private float lightRecoilSpeed = 2.2f;
    [SerializeField] private float mediumLift = 2.2f;
    [SerializeField] private float heavyLift = 4.2f;
    [SerializeField] private float maxFlightTime = 1.35f;
    [SerializeField] private float groundCheckDistance = 0.16f;
    [SerializeField] private float recoveryFallbackTime = 0.28f;
    [SerializeField] private float maxVisualTilt = 28f;

    private Health health;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private SpriteRenderer sprite;
    private Transform visual;

    private Color baseColor;
    private Vector3 reactionBaseScale;
    private Vector3 flashBaseScale;
    private Quaternion reactionBaseRotation;
    private Coroutine flashRoutine;
    private Coroutine reactionRoutine;
    private bool launched;
    private bool reacting;
    private float stunnedUntil;

    public bool IsStunned => Time.time < stunnedUntil;
    public bool IsLaunched => launched;
    public bool IsReacting => reacting;

    private void Awake()
    {
        health = GetComponent<Health>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        visual = sprite != null ? sprite.transform : transform;

        if (sprite != null)
            baseColor = sprite.color;

        if (visual != null)
        {
            reactionBaseScale = visual.localScale;
            reactionBaseRotation = visual.localRotation;
        }
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void OnDied()
    {
        StopAllReactions();
        launched = false;
        reacting = false;

        if (visual != null)
        {
            visual.localRotation = reactionBaseRotation;
            visual.localScale = reactionBaseScale;
        }

        if (sprite != null)
        {
            sprite.color = baseColor;
            sprite.enabled = true;
        }
    }

    private void OnDamaged(DamageInfo info)
    {
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + Mathf.Max(0.08f, info.hitStun));

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        Color flashColor = info.hitFlashColor.a > 0.001f
            ? info.hitFlashColor
            : defaultFlashColor;
        float flashDuration = info.hitFlashDuration > 0.001f
            ? info.hitFlashDuration
            : defaultFlashDuration;

        flashRoutine = StartCoroutine(Flash(flashColor, flashDuration));

        // Уже летящую цель не перезапускаем: иначе повторные попадания
        // дают хаотичные рывки. Вспышка и hitstun при этом обновляются.
        if (launched)
            return;

        if (reactionRoutine != null)
        {
            StopCoroutine(reactionRoutine);
            reactionRoutine = null;
        }

        if (info.impactWeight == HitImpactWeight.Launch &&
            info.launchVelocity.sqrMagnitude > 0.01f)
        {
            reactionRoutine = StartCoroutine(LaunchRoutine(info));
            return;
        }

        if (info.impactWeight == HitImpactWeight.Heavy)
        {
            reactionRoutine = StartCoroutine(HeavyRoutine(info));
            return;
        }

        if (info.impactWeight == HitImpactWeight.Medium)
        {
            reactionRoutine = StartCoroutine(MediumRoutine(info));
            return;
        }

        reactionRoutine = StartCoroutine(LightRoutine(info));
    }

    private IEnumerator Flash(Color flashColor, float duration)
    {
        if (sprite == null)
            yield break;

        flashBaseScale = visual != null ? visual.localScale : Vector3.one;

        sprite.enabled = true;
        sprite.color = flashColor;
        if (visual != null)
            visual.localScale = flashBaseScale * hitScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        sprite.color = baseColor;
        if (visual != null)
            visual.localScale = flashBaseScale;

        // Мерцание i-frames должно быть видимым даже во время hit stop.
        if (health != null && health.InvulnerableRemaining > 0f)
        {
            float flickerClock = 0f;
            while (health.IsAlive && health.InvulnerableRemaining > 0f)
            {
                if (sprite != null)
                    sprite.enabled = (int)(flickerClock / 0.055f) % 2 == 0;

                flickerClock += Time.unscaledDeltaTime;
                yield return null;
            }

            if (sprite != null)
                sprite.enabled = true;
        }

        flashRoutine = null;
    }

    private IEnumerator LightRoutine(DamageInfo info)
    {
        reacting = true;

        float direction = ResolveDirection(info);
        float recoil = Mathf.Max(lightRecoilSpeed, Mathf.Abs(info.knockback.x));
        ApplyHorizontalVelocity(direction * recoil, 1f);

        float tilt = ResolveTilt(info, 5f);
        yield return AnimateTilt(tilt, 0.05f);

        yield return new WaitForSeconds(0.08f);
        ApplyHorizontalVelocity(0f, 0f);
        yield return AnimateTilt(0f, 0.08f);

        reacting = false;
    }

    private IEnumerator MediumRoutine(DamageInfo info)
    {
        reacting = true;

        float direction = ResolveDirection(info);
        float recoil = Mathf.Max(3.5f, Mathf.Abs(info.knockback.x));
        ApplyHorizontalVelocity(direction * recoil, mediumLift);

        float tilt = ResolveTilt(info, 12f);
        yield return AnimateTilt(tilt, 0.07f);

        yield return new WaitForSeconds(Mathf.Max(0.12f, info.hitStun * 0.55f));
        ApplyHorizontalVelocity(0f, 0f);
        yield return AnimateTilt(0f, 0.12f);

        reacting = false;
    }

    private IEnumerator HeavyRoutine(DamageInfo info)
    {
        reacting = true;
        launched = true;

        float direction = ResolveDirection(info);
        float horizontal = Mathf.Max(6f, Mathf.Abs(info.knockback.x));
        float vertical = Mathf.Max(heavyLift, info.launchVelocity.y);

        if (body != null)
            body.linearVelocity = new Vector2(direction * horizontal, vertical);

        float tilt = ResolveTilt(info, 18f);
        yield return AnimateTilt(tilt, 0.08f);

        yield return WaitForGroundOrTimeout(maxFlightTime);

        if (body != null)
            body.linearVelocity = new Vector2(body.linearVelocity.x * 0.25f, body.linearVelocity.y);

        yield return AnimateTilt(0f, Mathf.Max(0.12f, info.reactionRecovery));
        launched = false;
        reacting = false;
    }

    private IEnumerator LaunchRoutine(DamageInfo info)
    {
        reacting = true;
        launched = true;

        float direction = ResolveDirection(info);
        Vector2 launch = info.launchVelocity;
        launch.x = Mathf.Abs(launch.x) * direction;
        if (launch.sqrMagnitude < 0.01f)
            launch = new Vector2(direction * Mathf.Max(6f, Mathf.Abs(info.knockback.x)), heavyLift);

        if (body != null)
            body.linearVelocity = launch;

        float tilt = ResolveTilt(info, maxVisualTilt);
        yield return AnimateTilt(tilt, 0.09f);

        yield return WaitForGroundOrTimeout(maxFlightTime);

        if (body != null)
            body.linearVelocity = new Vector2(body.linearVelocity.x * 0.18f, body.linearVelocity.y);

        yield return AnimateTilt(0f, Mathf.Max(0.12f, info.reactionRecovery));
        launched = false;
        reacting = false;
    }

    private IEnumerator AnimateTilt(float targetTilt, float duration)
    {
        if (visual == null || duration <= 0f)
            yield break;

        Quaternion from = visual.localRotation;
        Quaternion to = reactionBaseRotation * Quaternion.Euler(0f, 0f, targetTilt);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            visual.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        visual.localRotation = to;
    }

    private IEnumerator WaitForGroundOrTimeout(float timeout)
    {
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (health != null && !health.IsAlive)
                yield break;

            if (IsGrounded())
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private float ResolveDirection(DamageInfo info)
    {
        if (Mathf.Abs(info.knockback.x) > 0.01f)
            return Mathf.Sign(info.knockback.x);

        if (Mathf.Abs(info.launchVelocity.x) > 0.01f)
            return Mathf.Sign(info.launchVelocity.x);

        if (Mathf.Abs(info.hitPoint.x - transform.position.x) > 0.01f)
            return info.hitPoint.x < transform.position.x ? 1f : -1f;

        return 1f;
    }

    private float ResolveTilt(DamageInfo info, float fallback)
    {
        float amount = info.reactionTilt > 0.01f ? info.reactionTilt : fallback;
        amount = Mathf.Clamp(amount, 0f, maxVisualTilt);
        return -ResolveDirection(info) * amount;
    }

    private void ApplyHorizontalVelocity(float velocityX, float lift)
    {
        if (body == null)
            return;

        body.linearVelocity = new Vector2(velocityX, Mathf.Max(body.linearVelocity.y, lift));
    }

    private bool IsGrounded()
    {
        if (bodyCollider == null)
            return true;

        Bounds bounds = bodyCollider.bounds;
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            new Vector2(bounds.center.x, bounds.min.y + 0.02f),
            Vector2.down,
            groundCheckDistance
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger)
                continue;

            if (hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.collider.GetComponentInParent<Health>() != null)
                continue;

            return true;
        }

        return false;
    }

    private void StopAllReactions()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (reactionRoutine != null)
        {
            StopCoroutine(reactionRoutine);
            reactionRoutine = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (bodyCollider == null)
            return;

        Bounds bounds = bodyCollider.bounds;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            new Vector3(bounds.center.x, bounds.min.y, 0f),
            new Vector3(bounds.center.x, bounds.min.y - groundCheckDistance, 0f)
        );
    }
}

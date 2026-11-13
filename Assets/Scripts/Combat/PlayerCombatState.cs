using System.Collections;
using UnityEngine;

/// <summary>
/// Визуальные боевые состояния героя: урон, потеря сознания и окончательная смерть.
/// Потеря сознания от врагов и смерть от падения обрабатываются раздельно.
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerCombatState : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private HamsterMovement movement;
    [SerializeField] private AttackController attack;
    [SerializeField] private Rigidbody2D body;

    [Header("Состояния")]
    [SerializeField] private string idleStateName = "Idle 0";
    [SerializeField] private string hurtStateName = "Hurt";
    [SerializeField] private string downedStateName = "Downed";
    [SerializeField] private string deathStateName = "Death";

    [SerializeField] private float hurtDuration = 0.48f;
    [SerializeField] private GameObject dizzyStarsPrefab;

    private Health health;
    private Coroutine hurtRoutine;
    private bool hurtPlaying;
    private float currentHurtDuration;
    private GameObject activeDizzyStars;

    public bool IsDowned { get; private set; }
    public bool IsDead { get; private set; }

    private void Awake()
    {
        health = GetComponent<Health>();
        if (animator == null) animator = GetComponent<Animator>();
        if (movement == null) movement = GetComponent<HamsterMovement>();
        if (attack == null) attack = GetComponent<AttackController>();
        if (body == null) body = GetComponent<Rigidbody2D>();
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

    private void OnDamaged(DamageInfo info)
    {
        if (IsDowned || IsDead || !health.IsAlive)
            return;

        if (hurtRoutine != null)
            StopCoroutine(hurtRoutine);

        hurtPlaying = true;
        currentHurtDuration = Mathf.Max(hurtDuration, info.hitStun);

        if (attack != null)
            attack.CancelCurrentAttack();

        if (movement != null)
        {
            movement.SetControlLocked(true);
            movement.SetJumpLocked(true);
        }

        if (animator != null) animator.Play(hurtStateName, 0, 0f);
        hurtRoutine = StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine()
    {
        yield return new WaitForSeconds(currentHurtDuration);
        hurtPlaying = false;
        hurtRoutine = null;

        if (IsDowned || IsDead || !health.IsAlive)
            yield break;

        if (movement != null)
        {
            movement.SetControlLocked(false);
            movement.SetJumpLocked(false);
        }

        if (animator != null) animator.Play(idleStateName, 0, 0f);
    }

    private void OnDied()
    {
        if (IsDead) return;
        EnterDowned();
    }

    public void EnterDowned()
    {
        if (IsDead) return;

        IsDowned = true;
        hurtPlaying = false;

        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
            hurtRoutine = null;
        }

        DisableControl();
        if (animator != null) animator.Play(downedStateName, 0, 0f);
    }

    public void Revive(float healthFraction)
    {
        if (IsDead) return;

        IsDowned = false;
        HideDizzyStars();
        health.Revive(healthFraction);

        if (movement != null)
        {
            movement.enabled = true;
            movement.SetControlLocked(false);
            movement.SetJumpLocked(false);
        }

        if (attack != null)
            attack.enabled = true;

        if (animator != null)
            animator.Play(idleStateName, 0, 0f);
    }

    /// <summary>Окончательная смерть, например при падении в яму.</summary>
    public void DiePermanently()
    {
        if (IsDead) return;

        IsDead = true;
        IsDowned = false;
        hurtPlaying = false;
        health.Kill();

        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
            hurtRoutine = null;
        }

        DisableControl();
        HideDizzyStars();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        if (animator != null) animator.Play(deathStateName, 0, 0f);
    }

    private void ShowDizzyStars()
    {
        if (activeDizzyStars != null || dizzyStarsPrefab == null)
            return;

        activeDizzyStars = Instantiate(dizzyStarsPrefab, transform);
        float parentScale = transform.lossyScale.y;
        if (parentScale > 0.0001f)
            activeDizzyStars.transform.localScale = Vector3.one * (1.45f / parentScale);

        SpriteRenderer heroSprite = GetComponentInChildren<SpriteRenderer>();
        if (heroSprite != null)
        {
            Bounds bounds = heroSprite.bounds;
            Vector3 worldPoint = new Vector3(bounds.center.x, bounds.max.y - 0.8f, transform.position.z);
            activeDizzyStars.transform.position = worldPoint;
        }
    }

    private void HideDizzyStars()
    {
        if (activeDizzyStars != null)
            Destroy(activeDizzyStars);
        activeDizzyStars = null;
    }

    private void DisableControl()
    {
        if (movement != null)
        {
            movement.SetControlLocked(true);
            movement.SetJumpLocked(true);
            movement.enabled = false;
        }

        if (attack != null)
            attack.enabled = false;
    }
}

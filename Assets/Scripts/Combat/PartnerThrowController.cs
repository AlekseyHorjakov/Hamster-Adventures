using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Подъём и бросок напарника. Один и тот же компонент стоит на обоих героях:
/// в обычном состоянии он ищет напарника рядом, при повторном нажатии бросает
/// его вперёд, а у брошенного героя ведёт полёт, урон врагам и возврат управления.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HamsterMovement))]
public class PartnerThrowController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private HamsterMovement movement;
    [SerializeField] private AttackController attack;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Health health;
    [SerializeField] private PlayerCombatState combatState;

    [Header("Захват")]
    [Tooltip("На каком расстоянии между корнями героев разрешён захват")]
    [SerializeField] private float pickupRange = 2.4f;
    [SerializeField] private float carryLift = 0.08f;
    [SerializeField] private string idleStateName = "Idle 0";
    [SerializeField] private string carriedStateName = "HJump";
    [SerializeField] private string thrownStateName = "HJump";

    [Header("Полёт")]
    [Tooltip("Стартовая горизонтальная скорость броска")]
    [SerializeField] private float launchSpeed = 13f;
    [Tooltip("Разгон за время полёта: эффекты и удар становятся сильнее")]
    [SerializeField] private float launchAcceleration = 24f;
    [SerializeField] private float flightDuration = 0.55f;
    [SerializeField] private float launchForwardOffset = 1f;
    [SerializeField] private float afterHitFlightTime = 0.1f;
    [SerializeField] private HitData throwHit;
    [SerializeField] private GameObject flightVfxPrefab;
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private float impactShakeStrength = 0.55f;
    [SerializeField] private float impactShakeDuration = 0.24f;

    private HamsterMovement[] players;
    private HamsterMovement carried;
    private PartnerThrowController carriedController;
    private GameObject thrower;
    private Collider2D throwerCollider;
    private PartnerThrowVFXController flightVfx;
    private readonly HashSet<Health> hitTargets = new HashSet<Health>();

    private bool isCarried;
    private bool isFlying;
    private int throwFacing = 1;
    private float nextPickupAt;
    private float flightStartedAt;
    private float flightEndsAt;
    private float originalGravityScale;
    private bool movementWasEnabled;
    private bool attackWasEnabled;
    private bool colliderWasEnabled;
    private bool colliderWasTrigger;
    private int originalSortingOrder;
    private bool sortingChanged;

    public bool IsCarried => isCarried;
    public bool IsFlying => isFlying;
    public bool CanBePickedUp => !isCarried && !isFlying && health != null && health.IsAlive &&
                                 (combatState == null || (!combatState.IsDowned && !combatState.IsDead));

    private void Awake()
    {
        if (movement == null) movement = GetComponent<HamsterMovement>();
        if (input == null) input = GetComponent<PlayerInputReader>();
        if (attack == null) attack = GetComponent<AttackController>();
        if (animator == null) animator = GetComponent<Animator>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (health == null) health = GetComponent<Health>();
        if (combatState == null) combatState = GetComponent<PlayerCombatState>();
    }

    private void Update()
    {
        if (isCarried || isFlying || movement == null || input == null)
            return;

        if (carried != null)
        {
            if (carriedController == null || carried == null ||
                (health != null && !health.IsAlive) ||
                (combatState != null && combatState.IsDowned))
            {
                ReleaseCarriedWithoutThrow();
                return;
            }

            if (input.ThrowPressed && Time.time >= nextPickupAt)
                ThrowCarried();

            return;
        }

        if (input.ThrowPressed && Time.time >= nextPickupAt)
            TryPickup();
    }

    private void LateUpdate()
    {
        if (carriedController == null || isFlying)
            return;

        Bounds bounds = sprite != null ? sprite.bounds : new Bounds(transform.position, Vector3.one);
        Vector3 position = new Vector3(
            bounds.center.x + movement.Facing * 0.12f,
            bounds.max.y + carryLift,
            transform.position.z);

        carriedController.transform.position = position;
        carriedController.FaceInDirection(movement.Facing);
    }

    private void FixedUpdate()
    {
        if (!isFlying || body == null)
            return;

        if (Time.time >= flightEndsAt)
        {
            FinishFlight();
            return;
        }

        float elapsed = Time.time - flightStartedAt;
        float speed = launchSpeed + launchAcceleration * elapsed;
        body.linearVelocity = new Vector2(throwFacing * speed, body.linearVelocity.y);

        if (flightVfx != null)
        {
            float range = Mathf.Max(0.01f, launchAcceleration * flightDuration);
            flightVfx.SetIntensity(Mathf.Clamp01((speed - launchSpeed) / range));
        }

        CheckHitsAndObstacles();
    }

    private void TryPickup()
    {
        if (players == null || players.Length == 0)
            players = FindObjectsByType<HamsterMovement>(FindObjectsSortMode.None);

        HamsterMovement nearest = null;
        PartnerThrowController nearestController = null;
        float nearestDistance = pickupRange;

        foreach (HamsterMovement player in players)
        {
            if (player == null || player == movement)
                continue;

            PartnerThrowController candidate = player.GetComponent<PartnerThrowController>();
            if (candidate == null || !candidate.CanBePickedUp)
                continue;

            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance > pickupRange || distance >= nearestDistance)
                continue;

            if (Mathf.Abs(player.transform.position.y - transform.position.y) > 2f)
                continue;

            nearest = player;
            nearestController = candidate;
            nearestDistance = distance;
        }

        if (nearest == null || nearestController == null)
            return;

        carried = nearest;
        carriedController = nearestController;
        nearestController.BeginCarried(gameObject);
        nextPickupAt = Time.time + 0.12f;
    }

    public void BeginCarried(GameObject carrier)
    {
        isCarried = true;
        isFlying = false;
        hitTargets.Clear();

        movementWasEnabled = movement != null && movement.enabled;
        attackWasEnabled = attack != null && attack.enabled;

        if (health != null) health.SetDamageImmune(true);
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        if (bodyCollider != null)
        {
            colliderWasEnabled = bodyCollider.enabled;
            colliderWasTrigger = bodyCollider.isTrigger;
            bodyCollider.enabled = false;
        }

        if (sprite != null)
        {
            originalSortingOrder = sprite.sortingOrder;
            sprite.sortingOrder += 4;
            sortingChanged = true;
        }

        PlayState(carriedStateName, true);
    }

    private void ThrowCarried()
    {
        if (carried == null || carriedController == null)
            return;

        Bounds bounds = sprite != null ? sprite.bounds : new Bounds(transform.position, Vector3.one);
        Vector3 launchPosition = new Vector3(
            bounds.center.x + movement.Facing * launchForwardOffset,
            transform.position.y + 0.05f,
            transform.position.z);

        PartnerThrowController thrown = carriedController;
        Collider2D carrierCollider = bodyCollider;

        carried = null;
        carriedController = null;
        thrown.LaunchAsProjectile(gameObject, movement.Facing, launchPosition, carrierCollider);
        nextPickupAt = Time.time + 0.15f;
    }

    private void LaunchAsProjectile(GameObject carrier, int direction, Vector3 launchPosition, Collider2D carrierCollider)
    {
        isCarried = false;
        isFlying = true;
        thrower = carrier;
        throwerCollider = carrierCollider;
        throwFacing = direction;
        hitTargets.Clear();

        transform.position = launchPosition;
        FaceInDirection(direction);

        if (health != null) health.SetDamageImmune(true);
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;

        if (body != null)
        {
            originalGravityScale = body.gravityScale;
            body.simulated = true;
            body.gravityScale = 0f;
            body.linearVelocity = new Vector2(direction * launchSpeed, 0f);
        }

        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
            bodyCollider.isTrigger = true;
        }

        if (throwerCollider != null && bodyCollider != null)
            Physics2D.IgnoreCollision(throwerCollider, bodyCollider, true);

        PlayState(thrownStateName, true);

        if (flightVfxPrefab != null)
        {
            GameObject vfxObject = Instantiate(flightVfxPrefab);
            flightVfx = vfxObject.GetComponent<PartnerThrowVFXController>();
            if (flightVfx != null)
                flightVfx.Play(transform, sprite, direction);
        }

        flightStartedAt = Time.time;
        flightEndsAt = Time.time + flightDuration;
    }

    private void CheckHitsAndObstacles()
    {
        if (sprite == null || bodyCollider == null)
            return;

        Bounds bounds = sprite.bounds;
        Vector2 center = bounds.center;
        Vector2 size = new Vector2(
            Mathf.Max(0.7f, bounds.size.x * 0.42f),
            Mathf.Max(0.55f, bounds.size.y * 0.34f));

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.transform.root == transform.root)
                continue;

            if (thrower != null && hit.transform.root == thrower.transform.root)
                continue;

            Health target = hit.GetComponentInParent<Health>();

            if (target != null)
            {
                if (target.GetComponentInParent<HamsterMovement>() != null)
                    continue;

                if (!target.IsAlive || !hitTargets.Add(target))
                    continue;

                ApplyThrowHit(target, center);
                return;
            }

            if (!hit.isTrigger)
            {
                FinishFlight();
                return;
            }
        }
    }

    private void ApplyThrowHit(Health target, Vector2 hitPoint)
    {
        if (throwHit == null)
            return;

        target.TakeDamage(new DamageInfo
        {
            amount = throwHit.damage,
            hitPoint = hitPoint,
            knockback = new Vector2(throwHit.knockback * throwFacing, 0f),
            hitStop = throwHit.hitStop,
            hitStun = throwHit.hitStun,
            launchVelocity = new Vector2(throwHit.launchVelocity.x * throwFacing, throwHit.launchVelocity.y),
            source = thrower
        });

        HitStop.Play(throwHit.hitStop);
        CameraFollow.Shake(impactShakeStrength, impactShakeDuration);
        SpawnImpact(hitPoint);

        // После первого попадания бросок быстро затухает: это удар, а не бесконечный снаряд.
        flightEndsAt = Mathf.Min(flightEndsAt, Time.time + afterHitFlightTime);
        if (body != null)
            body.linearVelocity = new Vector2(throwFacing * launchSpeed * 0.35f, 1.2f);
    }

    private void SpawnImpact(Vector2 point)
    {
        if (impactVfxPrefab != null)
        {
            GameObject vfxObject = Instantiate(impactVfxPrefab, point, Quaternion.identity);
            ThrowImpactVFX impact = vfxObject.GetComponent<ThrowImpactVFX>();
            if (impact != null)
                impact.Play(throwFacing);
            return;
        }

        GameObject sparkPrefab = Resources.Load<GameObject>("HitSpark");
        if (sparkPrefab != null)
        {
            GameObject spark = Instantiate(sparkPrefab, point, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            spark.transform.localScale = Vector3.one * 1.8f;
        }
    }

    private void FinishFlight()
    {
        if (!isFlying)
            return;

        isFlying = false;

        if (flightVfx != null)
        {
            flightVfx.StopAndDestroy();
            flightVfx = null;
        }

        if (body != null)
        {
            body.simulated = true;
            body.gravityScale = originalGravityScale;
            body.linearVelocity = new Vector2(body.linearVelocity.x * 0.35f, Mathf.Min(body.linearVelocity.y, 1.5f));
        }

        RestoreColliderAndImmunity();

        if (movement != null) movement.enabled = movementWasEnabled;
        if (attack != null) attack.enabled = attackWasEnabled;

        RestoreSorting();
        PlayState(idleStateName, false);

        if (throwerCollider != null && bodyCollider != null)
            Physics2D.IgnoreCollision(throwerCollider, bodyCollider, false);

        thrower = null;
        throwerCollider = null;
    }

    public void DropFromCarrier()
    {
        if (!isCarried)
            return;

        isCarried = false;

        if (body != null)
        {
            body.simulated = true;
            body.gravityScale = originalGravityScale > 0f ? originalGravityScale : 2.5f;
            body.linearVelocity = new Vector2(0f, -1f);
        }

        RestoreColliderAndImmunity();

        if (movement != null) movement.enabled = movementWasEnabled;
        if (attack != null) attack.enabled = attackWasEnabled;

        RestoreSorting();
        PlayState(idleStateName, false);
    }

    private void ReleaseCarriedWithoutThrow()
    {
        if (carriedController != null)
            carriedController.DropFromCarrier();

        carried = null;
        carriedController = null;
        nextPickupAt = Time.time + 0.2f;
    }

    private void RestoreColliderAndImmunity()
    {
        if (bodyCollider != null)
        {
            bodyCollider.enabled = colliderWasEnabled;
            bodyCollider.isTrigger = colliderWasTrigger;
        }

        if (health != null)
            health.SetDamageImmune(false);
    }

    private void RestoreSorting()
    {
        if (!sortingChanged || sprite == null)
            return;

        sprite.sortingOrder = originalSortingOrder;
        sortingChanged = false;
    }

    private void FaceInDirection(int direction)
    {
        if (sprite != null)
            sprite.flipX = direction < 0;
    }

    private void PlayState(string stateName, bool airborne)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return;

        // Пока управление выключено, Animator не должен сам уйти в Idle
        // по старым значениям параметров движения.
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsJumping", airborne);
        animator.SetBool("IsGrounded", !airborne);
        animator.Play(stateName, 0, 0f);
    }

    private void OnDisable()
    {
        if (isCarried)
            DropFromCarrier();

        if (isFlying)
            FinishFlight();
    }
}

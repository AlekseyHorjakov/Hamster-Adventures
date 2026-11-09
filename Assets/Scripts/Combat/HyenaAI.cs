using System.Collections;
using UnityEngine;

/// <summary>
/// Простая гиена: патрулирует у дома, замечает ближайшего живого героя,
/// догоняет и делает один укус с заметным замахом. Это прототип поведения,
/// без прыжков и сложных тактик — чтобы проверить боевой цикл целиком.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class HyenaAI : MonoBehaviour
{
    [Header("Обнаружение")]
    [SerializeField] private float detectionRange = 9f;
    [SerializeField] private float loseRange = 12f;
    [SerializeField] private float verticalTolerance = 2.5f;

    [Header("Движение")]
    [SerializeField] private float patrolRadius = 3f;
    [SerializeField] private float patrolSpeed = 1.4f;
    [SerializeField] private float chaseSpeed = 3.2f;
    [SerializeField] private float patrolPause = 0.8f;

    [Header("Атака")]
    [SerializeField] private float attackRange = 1.55f;
    [SerializeField] private float attackWindup = 0.38f;
    [SerializeField] private float attackActive = 0.18f;
    [SerializeField] private float attackRecovery = 0.5f;
    [SerializeField] private float attackCooldown = 0.7f;
    [SerializeField] private HitData biteHit;
    [SerializeField] private Hitbox hitbox;

    [Header("Визуал")]
    [SerializeField] private Color windupColor = new Color(1f, 0.55f, 0.35f, 1f);
    [SerializeField] private float deathFadeTime = 0.7f;
    [SerializeField] private float corpseLifetime = 1.2f;

    private Rigidbody2D body;
    private Health health;
    private HitReaction reaction;
    private SpriteRenderer sprite;
    private Collider2D bodyCollider;
    private Color baseColor;

    private HamsterMovement[] heroes;
    private Vector2 home;
    private Vector2 patrolPoint;
    private float nextPatrolAt;
    private float nextAttackAt;
    private bool busy;
    private bool dead;
    private int facing = 1;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        reaction = GetComponent<HitReaction>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        baseColor = sprite != null ? sprite.color : Color.white;

        home = transform.position;
        patrolPoint = home;
    }

    private void OnEnable()
    {
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Died -= OnDied;
    }

    private void Start()
    {
        heroes = FindObjectsByType<HamsterMovement>(FindObjectsSortMode.None);
        PickPatrolPoint();
    }

    private void FixedUpdate()
    {
        if (dead || body == null)
            return;

        // Пока гиена оглушена или летит от удара, не перебиваем физику.
        if (reaction != null && (reaction.IsStunned || reaction.IsLaunched))
        {
            StopHorizontal();
            return;
        }

        if (busy)
        {
            StopHorizontal();
            return;
        }

        HamsterMovement target = FindNearestHero();

        if (target == null)
        {
            Patrol();
            return;
        }

        float dx = target.transform.position.x - transform.position.x;
        float dy = Mathf.Abs(target.transform.position.y - transform.position.y);
        float distance = Mathf.Abs(dx);

        if (distance > loseRange || dy > verticalTolerance)
        {
            Patrol();
            return;
        }

        Face(Mathf.Sign(dx));

        if (distance <= attackRange)
        {
            StopHorizontal();

            if (Time.time >= nextAttackAt)
                StartCoroutine(AttackRoutine(target));
            return;
        }

        if (distance <= detectionRange)
        {
            Move(Mathf.Sign(dx), chaseSpeed);
            return;
        }

        Patrol();
    }

    private HamsterMovement FindNearestHero()
    {
        if (heroes == null || heroes.Length == 0)
            heroes = FindObjectsByType<HamsterMovement>(FindObjectsSortMode.None);

        HamsterMovement nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (HamsterMovement hero in heroes)
        {
            if (hero == null)
                continue;

            Health heroHealth = hero.GetComponent<Health>();
            if (heroHealth != null && !heroHealth.IsAlive)
                continue;

            float distance = Mathf.Abs(hero.transform.position.x - transform.position.x);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = hero;
            }
        }

        return nearest;
    }

    private void Patrol()
    {
        if (Time.time < nextPatrolAt)
        {
            StopHorizontal();
            return;
        }

        float dx = patrolPoint.x - transform.position.x;

        if (Mathf.Abs(dx) < 0.2f)
        {
            StopHorizontal();
            nextPatrolAt = Time.time + patrolPause;
            PickPatrolPoint();
            return;
        }

        Face(Mathf.Sign(dx));
        Move(Mathf.Sign(dx), patrolSpeed);
    }

    private void PickPatrolPoint()
    {
        float x = home.x + Random.Range(-patrolRadius, patrolRadius);
        patrolPoint = new Vector2(x, home.y);
    }

    private void Move(float direction, float speed)
    {
        body.linearVelocity = new Vector2(direction * speed, body.linearVelocity.y);
        Face(direction);
    }

    private void StopHorizontal()
    {
        if (body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    private void Face(float direction)
    {
        if (Mathf.Abs(direction) < 0.01f)
            return;

        facing = direction > 0f ? 1 : -1;

        if (sprite != null)
            sprite.flipX = facing < 0;
    }

    private IEnumerator AttackRoutine(HamsterMovement target)
    {
        busy = true;
        StopHorizontal();

        float dx = target.transform.position.x - transform.position.x;
        Face(Mathf.Sign(dx));

        if (sprite != null)
            sprite.color = windupColor;

        yield return new WaitForSeconds(attackWindup);

        // За время замаха герой мог уйти, а гиена — получить удар.
        if (dead || reaction != null && reaction.IsStunned)
        {
            FinishAttack();
            yield break;
        }

        if (hitbox != null && biteHit != null)
            hitbox.Activate(biteHit, facing);

        yield return new WaitForSeconds(attackActive);

        if (hitbox != null)
            hitbox.Deactivate();

        if (sprite != null)
            sprite.color = baseColor;

        yield return new WaitForSeconds(attackRecovery);

        nextAttackAt = Time.time + attackCooldown;
        busy = false;
    }

    private void FinishAttack()
    {
        if (hitbox != null)
            hitbox.Deactivate();

        if (sprite != null)
            sprite.color = baseColor;

        nextAttackAt = Time.time + attackCooldown;
        busy = false;
    }

    private void OnDied()
    {
        if (dead)
            return;

        dead = true;

        if (hitbox != null)
            hitbox.Deactivate();

        if (bodyCollider != null)
            bodyCollider.enabled = false;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        if (sprite == null)
        {
            Destroy(gameObject, corpseLifetime);
            yield break;
        }

        Color start = sprite.color;
        float timer = 0f;

        while (timer < deathFadeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(start.a, 0f, timer / deathFadeTime);
            sprite.color = new Color(start.r, start.g, start.b, alpha);
            yield return null;
        }

        sprite.enabled = false;
        Destroy(gameObject, corpseLifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.55f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

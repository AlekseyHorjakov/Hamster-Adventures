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
    [Tooltip("Совпадает с началом кадра удара в клипе Attack")]
    [SerializeField] private float attackWindup = 0.16f;
    [SerializeField] private float attackActive = 0.16f;
    [SerializeField] private float attackRecovery = 0.22f;
    [SerializeField] private float attackCooldown = 0.55f;
    [SerializeField] private HitData biteHit;
    [SerializeField] private Hitbox hitbox;

    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string walkStateName = "Run";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private string hurtStateName = "Hurt";
    [SerializeField] private string deathStateName = "Death";
    [SerializeField] private float hurtAnimationDuration = 0.48f;

    [Header("Визуал")]
    [SerializeField] private float deathHoldTime = 0.75f;
    [SerializeField] private float deathFadeTime = 0.45f;
    [SerializeField] private float corpseLifetime = 0.8f;

    private Rigidbody2D body;
    private Health health;
    private HitReaction reaction;
    private SpriteRenderer sprite;
    private Collider2D bodyCollider;

    private HamsterMovement[] heroes;
    private Vector2 home;
    private Vector2 patrolPoint;
    private float nextPatrolAt;
    private float nextAttackAt;
    private bool busy;
    private bool hurtPlaying;
    private bool dead;
    private int facing = 1;
    private string currentState;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        reaction = GetComponent<HitReaction>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        home = transform.position;
        patrolPoint = home;
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

    private void Start()
    {
        heroes = FindObjectsByType<HamsterMovement>(FindObjectsSortMode.None);
        PickPatrolPoint();
        PlayState(idleStateName);
    }

    private void FixedUpdate()
    {
        if (dead || body == null)
            return;

        // Пока гиена оглушена или летит от удара, не перебиваем физику
        // и не включаем ходьбу поверх анимации получения урона.
        if (reaction != null && (reaction.IsStunned || reaction.IsLaunched))
        {
            StopHorizontal();
            return;
        }

        if (hurtPlaying || busy)
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
            PlayState(idleStateName);

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
            PlayState(idleStateName);
            return;
        }

        float dx = patrolPoint.x - transform.position.x;

        if (Mathf.Abs(dx) < 0.2f)
        {
            StopHorizontal();
            PlayState(idleStateName);
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
        PlayState(walkStateName);
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
        PlayState(attackStateName, true);

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

        yield return new WaitForSeconds(attackRecovery);

        nextAttackAt = Time.time + attackCooldown;
        busy = false;

        if (!hurtPlaying && !dead)
            PlayState(idleStateName);
    }

    private void FinishAttack()
    {
        if (hitbox != null)
            hitbox.Deactivate();

        nextAttackAt = Time.time + attackCooldown;
        busy = false;

        if (!hurtPlaying && !dead)
            PlayState(idleStateName);
    }

    private void OnDamaged(DamageInfo info)
    {
        if (dead) return;

        PlayState(hurtStateName, true);
        hurtPlaying = true;

        if (hitbox != null)
            hitbox.Deactivate();

        StopCoroutine(nameof(HurtRoutine));
        StartCoroutine(nameof(HurtRoutine));
    }

    private IEnumerator HurtRoutine()
    {
        yield return new WaitForSeconds(hurtAnimationDuration);
        hurtPlaying = false;

        if (!dead)
            PlayState(idleStateName);
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

        PlayState(deathStateName, true);
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // Даём клипу смерти проиграться до последнего кадра, и только потом
        // растворяем тело — иначе игрок не увидит саму анимацию.
        yield return new WaitForSeconds(deathHoldTime);

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

    private void PlayState(string stateName, bool restart = false)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return;

        if (!restart && currentState == stateName)
            return;

        animator.Play(stateName, 0, 0f);
        currentState = stateName;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.55f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

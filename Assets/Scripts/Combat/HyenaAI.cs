using System.Collections;
using UnityEngine;

/// <summary>
/// Гиена-прототип: патрулирует вокруг дома, замечает ближайшего живого героя
/// на своём уровне, преследует его и делает укус с рывком. После получения
/// урона переключается на атаковавшего и ещё некоторое время не теряет цель.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class HyenaAI : MonoBehaviour
{
    [Header("Обнаружение")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float loseRange = 16f;
    [SerializeField] private float verticalTolerance = 2f;
    [Tooltip("Сколько секунд гиена помнит цель, если та вышла из радиуса обнаружения")]
    [SerializeField] private float aggroMemory = 3f;

    [Header("Движение")]
    [SerializeField] private float patrolRadius = 3f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 5.2f;
    [SerializeField] private float patrolPause = 0.45f;

    [Header("Атака")]
    [SerializeField] private float attackRange = 2.3f;
    [Tooltip("Скорость короткого рывка во время замаха")]
    [SerializeField] private float attackLungeSpeed = 7f;
    [Tooltip("На этой дистанции рывок заканчивается и гиена кусает")]
    [SerializeField] private float attackLungeStopDistance = 1.25f;
    [Tooltip("Совпадает с началом кадра удара в клипе Attack")]
    [SerializeField] private float attackWindup = 0.16f;
    [SerializeField] private float attackActive = 0.16f;
    [SerializeField] private float attackRecovery = 0.22f;
    [SerializeField] private float attackCooldown = 0.4f;
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
    private HamsterMovement aggroTarget;
    private Vector2 home;
    private Vector2 patrolPoint;
    private float aggroUntil;
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

        HamsterMovement target = FindTarget();

        if (target == null)
        {
            Patrol();
            return;
        }

        float dx = target.transform.position.x - transform.position.x;
        float dy = Mathf.Abs(target.transform.position.y - transform.position.y);
        float distance = Mathf.Abs(dx);

        if (distance > loseRange)
        {
            ClearAggro();
            Patrol();
            return;
        }

        // Герой временно оказался на другом ярусе — не теряем его сразу,
        // но и не пытаемся укусить воздух. Ждём, пока он вернётся.
        if (dy > verticalTolerance)
        {
            StopHorizontal();
            PlayState(idleStateName);
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

        if (!Move(Mathf.Sign(dx), chaseSpeed))
        {
            // Не бежим с платформы вслед за недосягаемой целью.
            PlayState(idleStateName);
        }
    }

    private HamsterMovement FindTarget()
    {
        if (heroes == null || heroes.Length == 0)
            heroes = FindObjectsByType<HamsterMovement>(FindObjectsSortMode.None);

        // Уже выбранную цель не теряем сразу за границей обнаружения.
        if (aggroTarget != null)
        {
            Health targetHealth = aggroTarget.GetComponent<Health>();
            float dx = Mathf.Abs(aggroTarget.transform.position.x - transform.position.x);
            float dy = Mathf.Abs(aggroTarget.transform.position.y - transform.position.y);
            bool valid = targetHealth != null && targetHealth.IsAlive && dx <= loseRange;

            if (!valid)
            {
                ClearAggro();
            }
            else
            {
                if (dx <= detectionRange && dy <= verticalTolerance)
                    aggroUntil = Time.time + aggroMemory;

                if (Time.time < aggroUntil)
                    return aggroTarget;

                ClearAggro();
            }
        }

        HamsterMovement nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (HamsterMovement hero in heroes)
        {
            if (hero == null)
                continue;

            Health heroHealth = hero.GetComponent<Health>();
            if (heroHealth != null && !heroHealth.IsAlive)
                continue;

            float dx = Mathf.Abs(hero.transform.position.x - transform.position.x);
            float dy = Mathf.Abs(hero.transform.position.y - transform.position.y);

            // Не отвлекаемся на героя с другого яруса, до которого не дотянуться.
            if (dx > detectionRange || dy > verticalTolerance)
                continue;

            if (dx < nearestDistance)
            {
                nearestDistance = dx;
                nearest = hero;
            }
        }

        if (nearest != null)
        {
            aggroTarget = nearest;
            aggroUntil = Time.time + aggroMemory;
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

        if (!Move(Mathf.Sign(dx), patrolSpeed))
        {
            nextPatrolAt = Time.time + patrolPause;
            PickPatrolPoint();
        }
    }

    private void PickPatrolPoint()
    {
        float x = home.x + Random.Range(-patrolRadius, patrolRadius);
        patrolPoint = new Vector2(x, home.y);
    }

    /// <summary>Двигает гиену и возвращает false, если впереди обрыв.</summary>
    private bool Move(float direction, float speed)
    {
        if (!HasGroundAhead(direction))
        {
            StopHorizontal();
            return false;
        }

        body.linearVelocity = new Vector2(direction * speed, body.linearVelocity.y);
        Face(direction);
        PlayState(walkStateName);
        return true;
    }

    private bool HasGroundAhead(float direction)
    {
        if (bodyCollider == null)
            return true;

        Bounds bounds = bodyCollider.bounds;
        Vector2 origin = new Vector2(
            direction > 0f ? bounds.max.x + 0.08f : bounds.min.x - 0.08f,
            bounds.min.y + 0.08f);

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 0.35f);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider == bodyCollider)
                continue;

            if (hit.collider.transform.IsChildOf(transform))
                continue;

            // Герои и другие гиены не считаются полом.
            if (hit.collider.GetComponentInParent<Health>() != null)
                continue;

            return true;
        }

        return false;
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

        // Во время замаха гиена делает короткий рывок: так укус не остаётся
        // бесполезным, если герой пытается отойти на полшага.
        float windupTimer = 0f;
        while (windupTimer < attackWindup)
        {
            if (dead || target == null || reaction != null && reaction.IsStunned)
            {
                FinishAttack();
                yield break;
            }

            float distance = Mathf.Abs(target.transform.position.x - transform.position.x);
            if (distance > attackLungeStopDistance && HasGroundAhead(facing))
                body.linearVelocity = new Vector2(facing * attackLungeSpeed, body.linearVelocity.y);
            else
                StopHorizontal();

            windupTimer += Time.deltaTime;
            yield return null;
        }

        StopHorizontal();

        // За время замаха цель могла перепрыгнуть на другую сторону.
        if (target != null)
        {
            float biteDx = target.transform.position.x - transform.position.x;
            if (Mathf.Abs(biteDx) > 0.01f)
                Face(Mathf.Sign(biteDx));
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
        if (dead)
            return;

        // Получив удар, гиена переключается на того, кто её ударил.
        if (info.source != null)
        {
            HamsterMovement attacker = info.source.GetComponentInParent<HamsterMovement>();
            if (attacker != null)
            {
                Health attackerHealth = attacker.GetComponent<Health>();
                if (attackerHealth == null || attackerHealth.IsAlive)
                {
                    aggroTarget = attacker;
                    aggroUntil = Time.time + aggroMemory;
                }
            }
        }

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
        ClearAggro();

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

    private void ClearAggro()
    {
        aggroTarget = null;
        aggroUntil = 0f;
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

        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, loseRange);

        Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.55f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

using UnityEngine;

/// <summary>
/// Удары, комбо и заряженный удар. Логика в коде, а не в Animator: так удар
/// можно прервать восстановлением и запустить заново — это и есть ощущение
/// «машу кулаками» из битемапов, где игра не ждёт конца предыдущей анимации.
///
/// Весь бой идёт одной кнопкой: короткое нажатие — комбо из трёх ударов,
/// удержание с отпусканием — заряженный удар с рывком вперёд.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AttackController : MonoBehaviour
{
    [Header("Удары")]
    [Tooltip("Параметры ударов по порядку: сюда кладутся HitData")]
    [SerializeField] private HitData[] comboHits;

    [Tooltip("Имена состояний Animator по порядку ударов комбо")]
    [SerializeField] private string[] attackStates = { "FastPunch" };

    [Header("Тайминги")]
    [Tooltip("Длительность каждого удара: у финишёра дольше, потому что кадр попадания удерживается")]
    [SerializeField] private float[] attackDurations = { 0.25f, 0.25f, 0.32f };

    [Tooltip("Через сколько секунд после начала удар можно прервать следующим")]
    [SerializeField] private float[] cancelTimes = { 0.10f, 0.10f, 0.14f };

    [Tooltip("Сколько секунд герой не может двигаться с начала удара")]
    [SerializeField] private float movementLock = 0.14f;

    [Tooltip("Через сколько секунд после удара комбо сбрасывается на первый удар")]
    [SerializeField] private float comboResetTime = 0.45f;

    [Tooltip("Скорость шага вперёд на каждый удар: у финишёра шаг заметнее")]
    [SerializeField] private float[] stepSpeeds = { 1.5f, 1.5f, 4f };

    [Header("Удар в прыжке")]
    [Tooltip("Параметры удара в воздухе")]
    [SerializeField] private HitData airHit;

    [Tooltip("Состояние Animator для удара в воздухе")]
    [SerializeField] private string airStateName = "JumpKick";

    [Tooltip("Длительность удара в воздухе")]
    [SerializeField] private float airDuration = 0.3f;

    [Header("Заряженный удар")]
    [Tooltip("Параметры заряженного удара. Пусто — заряд не сработает, останется обычное комбо")]
    [SerializeField] private HitData chargedHit;

    [Tooltip("Состояние Animator для заряженного удара. Пусто — проигрывается последний удар комбо")]
    [SerializeField] private string chargedStateName = "";

    [Tooltip("Состояние Animator для накопления заряда. Пусто — анимация не меняется")]
    [SerializeField] private string chargeStateName = "";

    [Tooltip("Сколько секунд держать кнопку, чтобы удар стал заряженным")]
    [SerializeField] private float minChargeTime = 0.3f;

    [Tooltip("Скорость рывка в юнитах в секунду: 30 при 0,22 с даёт около 6,5 юнита, это 3–4 корпуса героя")]
    [SerializeField] private float dashSpeed = 30f;

    [Tooltip("Длительность рывка в секундах")]
    [SerializeField] private float dashDuration = 0.22f;

    [Tooltip("Насколько далеко впереди ищется стена, чтобы оборвать рывок")]
    [SerializeField] private float dashProbeDistance = 0.8f;

    [Tooltip("Слои, об которые рывок останавливается. Пусто — любые твёрдые коллайдеры")]
    [SerializeField] private LayerMask dashBlockMask;

    [Tooltip("Префаб горящего следа. Растягивается от точки старта до точки удара")]
    [SerializeField] private GameObject trailPrefab;

    [Tooltip("Сколько секунд живёт горящий след")]
    [SerializeField] private float trailLifetime = 2f;

    [Header("Ссылки")]
    [SerializeField] private HamsterMovement movement;
    [SerializeField] private Hitbox hitbox;

    private Animator animator;
    private int comboIndex;
    private bool attacking;
    private bool airAttack;
    private bool airAttackUsed;
    private bool charging;
    private bool dashing;
    private bool codeDrivenHitbox;
    private float attackStartedAt;
    private float chargeStartedAt;
    private float dashEndsAt;
    private float lastAttackEnd;
    private float lockUntil;
    private Vector3 dashFrom;
    private HitData currentHit;

    public void Configure(HamsterMovement movementRef, Hitbox hitboxRef, HitData[] hits, string[] states)
    {
        movement = movementRef;
        hitbox = hitboxRef;
        comboHits = hits;
        attackStates = states;
    }

    /// <summary>Настройка удара в воздухе.</summary>
    public void ConfigureAir(string stateName, HitData hit)
    {
        airStateName = stateName;
        airHit = hit;
    }

    /// <summary>Настройка заряженного удара.</summary>
    public void ConfigureCharged(HitData hit, string stateName, string chargeState, float minCharge, float speed, float duration)
    {
        chargedHit = hit;
        chargedStateName = stateName;
        chargeStateName = chargeState;
        minChargeTime = minCharge;
        dashSpeed = speed;
        dashDuration = duration;
    }

    /// <summary>Тайминги удара — выносятся в конфигурацию, чтобы править их из сборки.</summary>
    public void ConfigureTimings(float[] durations, float[] cancels, float[] steps, float lockTime, float resetTime)
    {
        attackDurations = durations;
        cancelTimes = cancels;
        stepSpeeds = steps;
        movementLock = lockTime;
        comboResetTime = resetTime;
    }

    private float DurationAt(int index)
    {
        return attackDurations != null && attackDurations.Length > 0
            ? attackDurations[Mathf.Min(index, attackDurations.Length - 1)]
            : 0.25f;
    }

    private float CancelAt(int index)
    {
        return cancelTimes != null && cancelTimes.Length > 0
            ? cancelTimes[Mathf.Min(index, cancelTimes.Length - 1)]
            : 0.10f;
    }

    private float StepAt(int index)
    {
        return stepSpeeds != null && stepSpeeds.Length > 0
            ? stepSpeeds[Mathf.Min(index, stepSpeeds.Length - 1)]
            : 4f;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        lastAttackEnd = -999f;

        if (movement == null)
            movement = GetComponent<HamsterMovement>();
    }

    private void Update()
    {
        if (movement == null)
            return;

        if (movement.Grounded)
            airAttackUsed = false;

        if (lockUntil > 0f && Time.unscaledTime >= lockUntil)
            ReleaseLock();

        // Рывком управляет FixedUpdate: там задаётся скорость.
        if (dashing)
            return;

        if (charging)
        {
            // В воздухе заряд не копится и не выполняется: приём наземный.
            if (!movement.Grounded)
            {
                CancelCharge();
                return;
            }

            if (!movement.AttackHeld)
                ReleaseCharge();

            return;
        }

        if (attacking)
        {
            float duration = airAttack ? airDuration : DurationAt(comboIndex);

            if (Time.time - attackStartedAt >= duration)
            {
                attacking = false;
                airAttack = false;
                lastAttackEnd = Time.time;
            }

            // Наземное комбо можно прервать следующим ударом, воздушное — нет.
            if (attacking && !airAttack &&
                Time.time - attackStartedAt >= CancelAt(comboIndex) &&
                movement.ConsumeAttackPress())
            {
                StartAttack(NextIndex());
            }

            return;
        }

        if (!movement.ConsumeAttackPress())
            return;

        if (!movement.Grounded)
        {
            if (!airAttackUsed)
                StartAirAttack();

            return;
        }

        BeginCharge();
    }

    private void FixedUpdate()
    {
        if (!dashing || movement == null)
            return;

        Rigidbody2D body = movement.Body;

        if (body == null || Time.time >= dashEndsAt || IsBlockedAhead(body, movement.Facing))
        {
            EndDash();
            return;
        }

        body.linearVelocity = new Vector2(movement.Facing * dashSpeed, 0f);
    }

    /// <summary>Есть ли твёрдое препятствие впереди: в стену не летим.</summary>
    private bool IsBlockedAhead(Rigidbody2D body, int facing)
    {
        Vector2 direction = Vector2.right * facing;
        RaycastHit2D[] hits = dashBlockMask.value == 0
            ? Physics2D.RaycastAll(body.position, direction, dashProbeDistance)
            : Physics2D.RaycastAll(body.position, direction, dashProbeDistance, dashBlockMask);

        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger)
                continue;

            if (hit.collider.transform.IsChildOf(transform))
                continue;

            return true;
        }

        return false;
    }

    private void BeginCharge()
    {
        charging = true;
        chargeStartedAt = Time.time;
        movement.SetControlLocked(true);

        if (movement.Body != null)
            movement.Body.linearVelocity = new Vector2(0f, movement.Body.linearVelocity.y);

        if (animator != null && !string.IsNullOrEmpty(chargeStateName))
            animator.Play(chargeStateName, 0, 0f);
    }

    private void CancelCharge()
    {
        charging = false;
        movement.SetControlLocked(false);
    }

    private void ReleaseCharge()
    {
        float held = Time.time - chargeStartedAt;
        charging = false;
        movement.SetControlLocked(false);

        if (held >= minChargeTime && chargedHit != null)
        {
            StartChargedAttack();
            return;
        }

        // Короткое нажатие — обычное комбо, включая продолжение серии.
        bool chainExpired = Time.time - lastAttackEnd > comboResetTime;
        StartAttack(chainExpired ? 0 : NextIndex());
    }

    private void StartChargedAttack()
    {
        dashing = true;
        attacking = false;
        airAttack = false;
        comboIndex = 0;
        currentHit = chargedHit;
        dashFrom = transform.position;
        dashEndsAt = Time.time + dashDuration;

        string stateName = !string.IsNullOrEmpty(chargedStateName)
            ? chargedStateName
            : (attackStates != null && attackStates.Length > 0
                ? attackStates[attackStates.Length - 1]
                : "FastPunch");

        if (animator != null)
            animator.Play(stateName, 0, 0f);

        movement.SetControlLocked(true);

        // Хитбокс ведёт код, а не события анимации: удар активен весь рывок.
        codeDrivenHitbox = true;

        if (hitbox != null)
            hitbox.Activate(chargedHit, movement.Facing);
    }

    private void EndDash()
    {
        dashing = false;
        codeDrivenHitbox = false;

        if (hitbox != null)
            hitbox.Deactivate();

        SpawnTrail();

        // После рывка короткая пауза, чтобы удар не переходил сразу в бег.
        lockUntil = Time.unscaledTime + movementLock;
    }

    private void SpawnTrail()
    {
        if (trailPrefab == null)
            return;

        Vector3 position = transform.position;
        float length = Mathf.Abs(position.x - dashFrom.x);

        if (length < 0.05f)
            return;

        Vector3 center = (dashFrom + position) * 0.5f;
        GameObject trail = Instantiate(trailPrefab, center, Quaternion.identity);

        // Префаб следа рисуется полосой длиной в один юнит — растягиваем по рывку.
        Vector3 scale = trail.transform.localScale;
        trail.transform.localScale = new Vector3(length * scale.x, scale.y, scale.z);

        if (position.x < dashFrom.x)
        {
            Vector3 flipped = trail.transform.localScale;
            flipped.x = -Mathf.Abs(flipped.x);
            trail.transform.localScale = flipped;
        }

        Destroy(trail, trailLifetime);
    }

    private void StartAirAttack()
    {
        airAttack = true;
        airAttackUsed = true;
        attacking = true;
        currentHit = airHit;
        attackStartedAt = Time.time;

        animator.Play(airStateName, 0, 0f);

        // В воздухе управление не блокируем: прыжок остаётся управляемым.
    }

    private void StartAttack(int index)
    {
        comboIndex = index;
        currentHit = comboHits != null && index < comboHits.Length ? comboHits[index] : null;
        attackStartedAt = Time.time;
        attacking = true;

        // Play вместо триггера: так удар гарантированно перезапускается с нуля,
        // даже если предыдущий ещё проигрывался.
        string stateName = attackStates != null && attackStates.Length > 0
            ? attackStates[Mathf.Min(index, attackStates.Length - 1)]
            : "FastPunch";

        animator.Play(stateName, 0, 0f);

        movement.SetControlLocked(true);
        lockUntil = Time.unscaledTime + movementLock;

        if (movement.Body != null)
        {
            int facing = movement.Facing;
            float step = StepAt(index);
            movement.Body.linearVelocity = new Vector2(facing * step, movement.Body.linearVelocity.y);
        }
    }

    private int NextIndex()
    {
        if (comboHits == null || comboHits.Length == 0)
            return 0;

        return (comboIndex + 1) % comboHits.Length;
    }

    private void ReleaseLock()
    {
        lockUntil = 0f;
        lastAttackEnd = Time.time;

        if (movement != null)
            movement.SetControlLocked(false);
    }

    // Вызываются событиями анимации в клипах удара.
    public void AnimEvent_EnableHitbox()
    {
        // Во время рывка хитбокс ведёт код: событие не должно перезапускать набор целей.
        if (codeDrivenHitbox)
            return;

        if (hitbox != null && currentHit != null)
            hitbox.Activate(currentHit, movement != null ? movement.Facing : 1);
    }

    public void AnimEvent_DisableHitbox()
    {
        if (codeDrivenHitbox)
            return;

        if (hitbox != null)
            hitbox.Deactivate();
    }
}

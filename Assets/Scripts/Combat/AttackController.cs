using UnityEngine;

/// <summary>
/// Удары и комбо. Логика в коде, а не в Animator: так удар можно прервать
/// восстановлением и запустить заново — это и есть ощущение «машу кулаками»
/// из битемапов, где игра не ждёт конца предыдущей анимации.
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

    [Header("Ссылки")]
    [SerializeField] private HamsterMovement movement;
    [SerializeField] private Hitbox hitbox;

    private Animator animator;
    private int comboIndex;
    private bool attacking;
    private float attackStartedAt;
    private float lastAttackEnd;
    private float lockUntil;
    private HitData currentHit;

    public void Configure(HamsterMovement movementRef, Hitbox hitboxRef, HitData[] hits, string[] states)
    {
        movement = movementRef;
        hitbox = hitboxRef;
        comboHits = hits;
        attackStates = states;
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
    {        if (movement == null)
            return;

        if (lockUntil > 0f && Time.unscaledTime >= lockUntil)
            ReleaseLock();

        if (attacking && Time.time - attackStartedAt >= DurationAt(comboIndex))
            attacking = false;

        // Во время удара нажатие не теряется: оно ждёт окна отмены
        // и сразу запускает следующий удар. Это и есть «машу быстро».
        if (attacking)
        {
            if (Time.time - attackStartedAt >= CancelAt(comboIndex) && movement.ConsumeAttackPress())
                StartAttack(NextIndex());

            return;
        }

        if (movement.ConsumeAttackPress())
        {
            bool chainExpired = Time.time - lastAttackEnd > comboResetTime;
            StartAttack(chainExpired ? 0 : NextIndex());
        }
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
        if (hitbox != null && currentHit != null)
            hitbox.Activate(currentHit, movement != null ? movement.Facing : 1);
    }

    public void AnimEvent_DisableHitbox()
    {
        if (hitbox != null)
            hitbox.Deactivate();
    }
}

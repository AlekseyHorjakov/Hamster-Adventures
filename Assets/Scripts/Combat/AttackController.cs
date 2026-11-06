using System.Collections;
using UnityEngine;

/// <summary>
/// Комбо из трёх ударов. Логика живёт в коде, а не в Animator:
/// так проще управлять окном ввода, отменой анимации и блокировкой движения.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AttackController : MonoBehaviour
{
    [Header("Удары")]
    [Tooltip("Параметры ударов по порядку: сюда кладутся HitData")]
    [SerializeField] private HitData[] comboHits;

    [Header("Тайминги")]
    [Tooltip("Сколько секунд герой не может двигаться с начала удара")]
    [SerializeField] private float movementLock = 0.22f;

    [Tooltip("Скорость шага вперёд в начале удара")]
    [SerializeField] private float stepSpeed = 4f;

    [Tooltip("Через сколько секунд после удара комбо сбрасывается")]
    [SerializeField] private float comboResetTime = 0.6f;

    [Header("Ссылки")]
    [SerializeField] private HamsterMovement movement;
    [SerializeField] private Hitbox hitbox;

    private Animator animator;
    private int comboIndex;
    private bool attacking;
    private bool queued;
    private float comboResetAt;
    private float lockUntil;
    private HitData currentHit;

    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private void ReleaseLock()
    {
        lockUntil = 0f;

        if (movement != null)
            movement.SetControlLocked(false);
    }

    /// <summary>
    /// Настройка связей из кода (используется сборкой проекта и тестами).
    /// Явный метод надёжнее записи в сериализованные поля через SerializedObject.
    /// </summary>
    public void Configure(HamsterMovement movementRef, Hitbox hitboxRef, HitData[] hits)
    {
        movement = movementRef;
        hitbox = hitboxRef;
        comboHits = hits;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (movement == null)
            movement = GetComponent<HamsterMovement>();
    }

    private void Update()
    {
        if (movement == null)
            return;

        // Страховка: управление не может остаться заблокированным навсегда,
        // даже если корутина прервалась или время было остановлено.
        if (lockUntil > 0f && Time.unscaledTime >= lockUntil)
            ReleaseLock();

        bool pressed = movement.ConsumeAttackPress();

        if (pressed)
        {
            if (!attacking && Time.time >= comboResetAt)
                StartCoroutine(AttackSequence(0));
            else if (attacking)
                queued = true;
        }

        if (!attacking && Time.time >= comboResetAt && comboIndex != 0)
            comboIndex = 0;
    }

    private IEnumerator AttackSequence(int index)
    {
        attacking = true;
        queued = false;
        comboIndex = index;

        currentHit = comboHits != null && index < comboHits.Length ? comboHits[index] : null;

        // Триггер переводит Animator в состояние удара.
        animator.SetTrigger(AttackHash);

        // Блокировка движения и короткий шаг вперёд — стандарт битемапа.
        movement.SetControlLocked(true);
        lockUntil = Time.unscaledTime + movementLock;
        int facing = movement.Facing;
        if (movement.Body != null)
            movement.Body.linearVelocity = new Vector2(facing * stepSpeed, movement.Body.linearVelocity.y);

        yield return new WaitForSeconds(movementLock);

        ReleaseLock();

        // Если игрок нажал ещё раз во время удара — продолжаем комбо.
        if (queued && index + 1 < comboHits.Length)
        {
            yield return new WaitForSeconds(0.05f);
            attacking = false;
            StartCoroutine(AttackSequence(index + 1));
            yield break;
        }

        attacking = false;
        comboResetAt = Time.time + comboResetTime;
    }

    // Вызываются событиями анимации в клипах FastPunch / HGFastPunch.
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

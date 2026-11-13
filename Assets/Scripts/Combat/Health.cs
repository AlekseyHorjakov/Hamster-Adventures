using System;
using UnityEngine;

/// <summary>
/// Здоровье персонажа или объекта. Единственная точка, через которую
/// проходит урон, поэтому сюда же можно повесить HUD и эффекты.
/// </summary>
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool applyKnockback = true;

    [Tooltip("Сколько секунд после попадания урон игнорируется (i-frames)")]
    [SerializeField] private float invulnerabilityAfterHit;

    [Header("Отладка")]
    [SerializeField] private bool logDamage;

    /// <summary>Текущее и максимальное здоровье.</summary>
    public event Action<float, float> Changed;

    /// <summary>Успешное попадание с параметрами.</summary>
    public event Action<DamageInfo> Damaged;

    public event Action Died;

    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsAlive => Current > 0f;
    public bool IsDamageImmune => damageImmune;
    public bool IsInvulnerable => damageImmune || Time.time < invulnerableUntil;
    public float InvulnerableRemaining => Mathf.Max(0f, invulnerableUntil - Time.time);

    private float invulnerableUntil;
    private bool damageImmune;

    private void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(DamageInfo info)
    {
        if (!IsAlive)
            return;

        if (IsInvulnerable)
            return;

        Current = Mathf.Max(0f, Current - info.amount);

        if (invulnerabilityAfterHit > 0f)
            invulnerableUntil = Time.time + invulnerabilityAfterHit;

        // Если у цели есть HitReaction, именно он управляет физикой реакции.
        // Для манекенов и простых объектов оставляем прежний fallback.
        if (applyKnockback && info.knockback.sqrMagnitude > 0.01f &&
            !TryGetComponent<HitReaction>(out _))
        {
            var body = GetComponent<Rigidbody2D>();
            if (body != null)
                body.linearVelocity = new Vector2(info.knockback.x, body.linearVelocity.y);
        }

        if (logDamage)
            Debug.Log($"[health] {name}: -{info.amount}, осталось {Current:0}", this);

        // Любой урон, у которого указан источник-герой, заряжает его супершкалу.
        // Это надёжнее, чем искать источник внутри конкретного Hitbox.
        if (info.source != null)
        {
            SuperMeter sourceMeter = info.source.GetComponentInParent<SuperMeter>();
            if (sourceMeter != null)
                sourceMeter.AddCharge(sourceMeter.HitChargeGain);
        }

        Damaged?.Invoke(info);
        Changed?.Invoke(Current, maxHealth);

        if (Current <= 0f)
            Died?.Invoke();
    }

    /// <summary>Мгновенно убивает объект, например при падении в яму.</summary>
    public void Kill()
    {
        if (!IsAlive)
            return;

        Current = 0f;
        Changed?.Invoke(Current, maxHealth);
        Died?.Invoke();
    }
    public void Revive(float fraction)
    {
        Current = Mathf.Clamp(maxHealth * fraction, 1f, maxHealth);
        invulnerableUntil = Time.time + 1f;
        Changed?.Invoke(Current, maxHealth);
    }

    /// <summary>Временная неуязвимость для переносимого или летящего напарника.</summary>
    public void SetDamageImmune(bool value)
    {
        damageImmune = value;
    }

    public void ResetHealth()
    {
        Current = maxHealth;
        invulnerableUntil = 0f;
        Changed?.Invoke(Current, maxHealth);
    }
}

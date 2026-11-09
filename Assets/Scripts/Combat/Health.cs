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

    private float invulnerableUntil;

    private void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(DamageInfo info)
    {
        if (!IsAlive)
            return;

        if (Time.time < invulnerableUntil)
            return;

        Current = Mathf.Max(0f, Current - info.amount);

        if (invulnerabilityAfterHit > 0f)
            invulnerableUntil = Time.time + invulnerabilityAfterHit;

        if (applyKnockback && info.knockback.sqrMagnitude > 0.01f)
        {
            var body = GetComponent<Rigidbody2D>();
            if (body != null)
                body.linearVelocity = new Vector2(info.knockback.x, body.linearVelocity.y);
        }

        if (logDamage)
            Debug.Log($"[health] {name}: -{info.amount}, осталось {Current:0}", this);

        Damaged?.Invoke(info);
        Changed?.Invoke(Current, maxHealth);

        if (Current <= 0f)
            Died?.Invoke();
    }

    public void Revive(float fraction)
    {
        Current = Mathf.Clamp(maxHealth * fraction, 1f, maxHealth);
        invulnerableUntil = Time.time + 1f;
        Changed?.Invoke(Current, maxHealth);
    }

    public void ResetHealth()
    {
        Current = maxHealth;
        invulnerableUntil = 0f;
        Changed?.Invoke(Current, maxHealth);
    }
}

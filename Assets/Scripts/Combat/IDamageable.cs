/// <summary>
/// Всё, что может получить урон. Реализуется компонентом Health.
/// </summary>
public interface IDamageable
{
    bool IsAlive { get; }

    void TakeDamage(DamageInfo info);
}

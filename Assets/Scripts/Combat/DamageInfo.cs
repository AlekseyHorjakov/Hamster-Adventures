using UnityEngine;

/// <summary>
/// Данные одного попадания: сколько урона, куда отбросить и на сколько
/// заморозить игру. Передаётся от хитбокса к Health.
/// </summary>
public struct DamageInfo
{
    public float amount;
    public Vector2 hitPoint;
    public Vector2 knockback;
    public float hitStop;
    public float hitStun;

    /// <summary>Если не ноль — цель уходит в полёт, а не просто отходит.</summary>
    public Vector2 launchVelocity;

    public GameObject source;
}

using UnityEngine;

public enum HitImpactWeight
{
    Light,
    Medium,
    Heavy,
    Launch
}

/// <summary>
/// Параметры одного удара. Числа живут в ассетах, а не в коде —
/// поэтому баланс правится в инспекторе без пересборки.
/// </summary>
[CreateAssetMenu(menuName = "Hamster Adventures/Hit Data", fileName = "HitData")]
public class HitData : ScriptableObject
{
    [Header("Урон")]
    public float damage = 8f;

    [Header("Отбрасывание")]
    public float knockback = 6f;

    [Header("Ощущение")]
    [Tooltip("Сколько секунд игра замирает на попадании")]
    public float hitStop = 0.05f;

    [Tooltip("Сколько секунд цель не может действовать после попадания")]
    public float hitStun = 0.18f;

    [Header("Тип реакции")]
    public HitImpactWeight impactWeight = HitImpactWeight.Light;

    [Header("Запуск")]
    [Tooltip("Если не ноль — цель улетает: X по направлению удара, Y вверх. Ноль — обычное отбрасывание")]
    public Vector2 launchVelocity;

    [Header("Визуальная обратная связь")]
    public Color hitFlashColor = Color.white;
    public float hitFlashDuration = 0.08f;

    [Header("Искры")]
    public Color sparkTint = Color.white;
    public float sparkScale = 1f;
    [Range(1, 5)] public int sparkCount = 1;

    [Header("Камера")]
    public float cameraShakeStrength = 0.08f;
    public float cameraShakeDuration = 0.08f;

    [Header("Реакция цели")]
    [Range(0f, 45f)] public float reactionTilt = 0f;
    public float reactionRecovery = 0.18f;

    [Header("Зона удара")]
    public Vector2 hitboxSize = new Vector2(1.4f, 1.1f);
    public Vector2 hitboxOffset = new Vector2(0.9f, 0f);
}

using UnityEngine;

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

    [Header("Зона удара")]
    public Vector2 hitboxSize = new Vector2(1.4f, 1.1f);
    public Vector2 hitboxOffset = new Vector2(0.9f, 0f);
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Область удара. Включается событием анимации на кадре замаха и выключается
/// после него. Каждая цель получает урон ровно один раз за активацию.
/// Работает через OverlapBox, поэтому не требует настройки слоёв физики.
/// </summary>
public class Hitbox : MonoBehaviour
{
    [Tooltip("Кого можно бить. Оставьте Everything, если не уверены.")]
    [SerializeField] private LayerMask targetMask = ~0;

    private readonly HashSet<Health> alreadyHit = new HashSet<Health>();
    private HitData data;
    private Vector2 offset;
    private int facing = 1;
    private bool active;

    public void Activate(HitData hitData, int facingDirection)
    {
        data = hitData;
        facing = facingDirection;
        offset = new Vector2(hitData.hitboxOffset.x * facing, hitData.hitboxOffset.y);
        alreadyHit.Clear();
        active = true;
    }

    public void Deactivate()
    {
        active = false;
        alreadyHit.Clear();
    }

    private void FixedUpdate()
    {
        if (!active || data == null)
            return;

        Vector2 center = (Vector2)transform.root.position + offset;
        var hits = Physics2D.OverlapBoxAll(center, data.hitboxSize, 0f, targetMask);

        foreach (var collider in hits)
        {
            var health = collider.GetComponentInParent<Health>();
            if (health == null || !health.IsAlive)
                continue;

            if (health.transform.root == transform.root)
                continue;

            if (!alreadyHit.Add(health))
                continue;

            health.TakeDamage(new DamageInfo
            {
                amount = data.damage,
                hitPoint = center,
                knockback = new Vector2(data.knockback * facing, 0f),
                hitStop = data.hitStop,
                source = transform.root.gameObject
            });

            HitStop.Play(data.hitStop);
            CameraFollow.Shake(0.12f, 0.12f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null)
            return;

        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.35f);
        Gizmos.DrawCube((Vector2)transform.root.position + data.hitboxOffset, data.hitboxSize);
    }
}

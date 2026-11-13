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
    [SerializeField] private bool centerOnRootCollider;

    private readonly HashSet<Health> alreadyHit = new HashSet<Health>();
    private HitData data;
    private Vector2 offset;
    private int facing = 1;
    private float damageMultiplier = 1f;
    private float geometrySizeMultiplier = 1f;
    private float geometryOffsetMultiplier = 1f;
    private bool active;

    public void Activate(HitData hitData, int facingDirection)
    {
        data = hitData;
        facing = facingDirection;
        offset = new Vector2(
            hitData.hitboxOffset.x * facing * geometryOffsetMultiplier,
            hitData.hitboxOffset.y * geometryOffsetMultiplier);
        alreadyHit.Clear();
        active = true;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(0f, multiplier);
    }

    /// <summary>Масштабирует зону удара вместе с визуальным размером героя.</summary>
    public void SetGeometryMultipliers(float sizeMultiplier, float offsetMultiplier)
    {
        geometrySizeMultiplier = Mathf.Max(0.01f, sizeMultiplier);
        geometryOffsetMultiplier = Mathf.Max(0.01f, offsetMultiplier);
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

        Vector2 origin = transform.root.position;
        if (centerOnRootCollider)
        {
            Collider2D ownerCollider = transform.root.GetComponent<Collider2D>();
            if (ownerCollider != null)
                origin = ownerCollider.bounds.center;
        }

        Vector2 center = origin + offset;
        Vector2 size = data.hitboxSize * geometrySizeMultiplier;
        var hits = Physics2D.OverlapBoxAll(center, size, 0f, targetMask);

        foreach (var collider in hits)
        {
            var health = collider.GetComponentInParent<Health>();
            if (health == null || !health.IsAlive)
                continue;

            // Улетевшего не добиваем: пока он в полёте, он вне боя.
            var reaction = health.GetComponent<HitReaction>();
            if (reaction != null && reaction.IsLaunched)
                continue;

            if (health.transform.root == transform.root)
                continue;

            if (!alreadyHit.Add(health))
                continue;

            Vector2 hitPoint = collider != null ? collider.ClosestPoint(center) : center;

            health.TakeDamage(new DamageInfo
            {
                amount = data.damage * damageMultiplier,
                hitPoint = hitPoint,
                knockback = new Vector2(data.knockback * facing, 0f),
                hitStop = data.hitStop,
                hitStun = data.hitStun,
                impactWeight = data.impactWeight,
                launchVelocity = new Vector2(data.launchVelocity.x * facing, data.launchVelocity.y),
                hitFlashColor = data.hitFlashColor,
                hitFlashDuration = data.hitFlashDuration,
                reactionTilt = data.reactionTilt,
                reactionRecovery = data.reactionRecovery,
                source = transform.root.gameObject
            });

            HitStop.Play(data.hitStop);

            if (data.cameraShakeStrength > 0f && data.cameraShakeDuration > 0f)
                CameraFollow.Shake(data.cameraShakeStrength, data.cameraShakeDuration);

            SpawnSparks(hitPoint);
        }
    }

    private static GameObject sparkPrefab;

    private void SpawnSparks(Vector2 position)
    {
        if (sparkPrefab == null)
            sparkPrefab = Resources.Load<GameObject>("HitSpark");

        if (sparkPrefab == null)
            return;

        int count = Mathf.Clamp(data.sparkCount, 1, 5);
        float baseScale = Mathf.Max(0.1f, data.sparkScale);
        Color tint = data.sparkTint.a > 0.01f ? data.sparkTint : Color.white;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = count > 1
                ? Random.insideUnitCircle * 0.18f
                : Vector2.zero;

            float angle = Random.Range(-25f, 25f);
            GameObject spark = Instantiate(
                sparkPrefab,
                position + offset,
                Quaternion.Euler(0f, 0f, angle)
            );

            spark.transform.localScale = Vector3.one * baseScale * Random.Range(0.88f, 1.12f);

            HitSpark sparkView = spark.GetComponent<HitSpark>();
            if (sparkView != null)
                sparkView.Play(baseScale, tint);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null)
            return;

        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.35f);
        Vector2 offset = new Vector2(
            data.hitboxOffset.x * geometryOffsetMultiplier,
            data.hitboxOffset.y * geometryOffsetMultiplier);
        Gizmos.DrawCube((Vector2)transform.root.position + offset, data.hitboxSize * geometrySizeMultiplier);
    }
}

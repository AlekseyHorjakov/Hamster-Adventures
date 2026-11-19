using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Щука-ловушка у переправы: сидит под водой, выпрыгивает, бьёт героя на 50% здоровья
/// и отбрасывает. Убить нельзя — Health на объекте нет, поэтому хитбоксы игроков её не берут.
/// Кадры прыжка идут по времени; смещения в кадрах уже нарисованы, объект стоит на месте.
/// </summary>
[DisallowMultipleComponent]
public sealed class PikeLeap : MonoBehaviour
{
    [Header("Анимация")]
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private bool flipX;

    [Header("Ритм")]
    [Tooltip("Пауза под водой между прыжками, сек.")]
    [SerializeField] private float restTime = 1.6f;
    [Tooltip("Длительность всего прыжка (все кадры), сек.")]
    [SerializeField] private float leapTime = 1.2f;
    [SerializeField] private float startDelay = 0.5f;

    [Header("Урон")]
    [Tooltip("Доля максимального здоровья героя, которую сносит щука.")]
    [SerializeField, Range(0.05f, 1f)] private float damageShare = 0.5f;
    [SerializeField] private Vector2 hitCenterOffset = new Vector2(0f, 1.7f);
    [SerializeField] private float hitRadius = 1.3f;
    [SerializeField] private float knockbackSpeed = 10f;
    [SerializeField] private float knockbackLift = 6f;
    [SerializeField] private float hitStop = 0.06f;
    [SerializeField] private HitImpactWeight impactWeight = HitImpactWeight.Heavy;

    private readonly HashSet<Health> hitThisLeap = new HashSet<Health>();    private float timer;
    private bool leaping;
    private float elapsed;
    private int lastFrame = -1;

    private void Awake()
    {
        if (visual == null)
            visual = GetComponent<SpriteRenderer>();
        if (visual != null)
            visual.flipX = flipX;
        SetVisible(false);
    }

    private void OnEnable()
    {
        timer = -Mathf.Max(0f, startDelay);
        leaping = false;
        elapsed = 0f;
        lastFrame = -1;
        SetVisible(false);
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0)
            return;

        if (!leaping)
        {
            timer += Time.deltaTime;
            if (timer >= restTime)
                StartLeap();
            return;
        }

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, leapTime));
        ShowFrame(t);

        if (t < 0.94f)
            TryHit();

        if (t >= 1f)
            EndLeap();
    }

    private void StartLeap()
    {
        leaping = true;
        elapsed = 0f;
        hitThisLeap.Clear();        lastFrame = -1;
        SetVisible(true);
    }

    private void EndLeap()
    {
        leaping = false;
        timer = 0f;
        SetVisible(false);
    }

    private void ShowFrame(float t)
    {
        int index = Mathf.Clamp(Mathf.FloorToInt(t * frames.Length), 0, frames.Length - 1);
        if (index == lastFrame)
            return;
        lastFrame = index;
        if (visual != null)
            visual.sprite = frames[index];
    }

    private void TryHit()
    {
        Vector2 center = (Vector2)transform.position + hitCenterOffset;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, hitRadius);
        foreach (Collider2D hit in hits)
        {
            Health health = hit.GetComponentInParent<Health>();
            if (health == null || !health.IsAlive)
                continue;
            if (health.GetComponentInParent<HamsterMovement>() == null)
                continue;
            if (!hitThisLeap.Add(health))
                continue;

            Vector2 direction = ((Vector2)health.transform.position - center).normalized;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector2.right;

            var info = new DamageInfo
            {
                amount = Mathf.Max(1f, health.Max * damageShare),
                hitPoint = center,
                knockback = new Vector2(direction.x * knockbackSpeed, knockbackLift),
                hitStop = hitStop,
                hitStun = 0.35f,
                impactWeight = impactWeight,
                hitFlashColor = Color.white,
                hitFlashDuration = 0.08f,
                reactionTilt = 12f,
                reactionRecovery = 0.35f,
                source = gameObject
            };
            health.TakeDamage(info);
        }
    }

    private void SetVisible(bool visible)
    {
        if (visual != null)
            visual.enabled = visible;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position + (Vector3)hitCenterOffset, hitRadius);
    }
}



using System.Collections;
using UnityEngine;

/// <summary>
/// Реакция на попадание: вспышка, хитстан и, если удар того заслуживает,
/// запуск — цель улетает по дуге, кувыркается, падает и встаёт.
/// </summary>
[RequireComponent(typeof(Health))]
public class HitReaction : MonoBehaviour
{
    [Header("Вспышка")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private float hitScale = 1.08f;

    [Header("Полёт")]
    [SerializeField] private float spinSpeed = 900f;
    [SerializeField] private float lieDownTime = 0.6f;
    [SerializeField] private float maxFlightTime = 1.5f;

    private Health health;
    private Rigidbody2D body;
    private Collider2D collider;
    private SpriteRenderer sprite;
    private Transform visual;

    private Color baseColor;
    private Vector3 baseScale;
    private Coroutine flash;
    private Coroutine launchRoutine;
    private bool launched;
    private float stunnedUntil;

    public bool IsStunned => Time.time < stunnedUntil;
    public bool IsLaunched => launched;

    private void Awake()
    {
        health = GetComponent<Health>();
        body = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        visual = sprite != null ? sprite.transform : transform;

        if (sprite != null)
        {
            baseColor = sprite.color;
            baseScale = sprite.transform.localScale;
        }
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void OnDied()
    {
        if (launchRoutine != null)
        {
            StopCoroutine(launchRoutine);
            launchRoutine = null;
        }

        launched = false;

        if (visual != null)
        {
            visual.rotation = Quaternion.identity;
            visual.localScale = baseScale;
        }

        if (sprite != null)
            sprite.color = baseColor;
    }

    private void OnDamaged(DamageInfo info)
    {
        stunnedUntil = Time.time + info.hitStun;

        if (flash != null)
            StopCoroutine(flash);

        flash = StartCoroutine(Flash());

        if (!launched && info.launchVelocity.sqrMagnitude > 0.01f)
            launchRoutine = StartCoroutine(ShortKnockback(info.launchVelocity));
    }

    private IEnumerator Flash()
    {
        if (sprite != null)
        {
            sprite.color = flashColor;
            visual.localScale = baseScale * hitScale;
        }

        yield return new WaitForSeconds(flashDuration);

        if (sprite != null)
        {
            sprite.color = baseColor;
            visual.localScale = baseScale;
        }

        flash = null;
    }

    /// <summary>
    /// Короткий отлёт без кувырка: враг смещается примерно на полкорпуса
    /// и остаётся в обычной ориентации.
    /// </summary>
    private IEnumerator ShortKnockback(Vector2 velocity)
    {
        launched = true;

        float direction = Mathf.Abs(velocity.x) > 0.01f ? Mathf.Sign(velocity.x) : 1f;
        float originalDamping = body != null ? body.linearDamping : 0f;

        if (body != null)
        {
            body.linearDamping = 0f;
            body.linearVelocity = new Vector2(direction * 5f, 0f);
        }

        yield return new WaitForSeconds(0.18f);

        if (body != null)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            body.linearDamping = originalDamping;
        }

        launched = false;
    }

    private bool IsGrounded()
    {
        if (collider == null)
            return true;

        Bounds bounds = collider.bounds;
        RaycastHit2D hit = Physics2D.Raycast(
            new Vector2(bounds.center.x, bounds.min.y + 0.02f),
            Vector2.down,
            0.12f);

        return hit.collider != null && hit.collider.transform.root != transform.root;
    }

    private void OnDrawGizmosSelected()
    {
        if (collider == null) return;

        Bounds bounds = collider.bounds;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(bounds.center.x, bounds.min.y, 0f),
                        new Vector3(bounds.center.x, bounds.min.y - 0.12f, 0f));
    }
}

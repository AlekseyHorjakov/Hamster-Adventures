using System.Collections;
using UnityEngine;

/// <summary>
/// Тренировочный манекен: получает урон, мигает и через несколько секунд
/// встаёт обратно. Нужен, чтобы проверять бой без врагов с ИИ.
/// </summary>
[RequireComponent(typeof(Health))]
public class Dummy : MonoBehaviour
{
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private float flashDuration = 0.14f;
    [Tooltip("Насколько манекен раздувается от удара")]
    [SerializeField] private float hitScale = 1.09f;

    private Health health;
    private SpriteRenderer sprite;
    private Collider2D body;
    private Color baseColor;
    private Vector3 baseScale;
    private Coroutine flash;

    private void Awake()
    {
        health = GetComponent<Health>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        body = GetComponent<Collider2D>();
        baseScale = transform.localScale;

        if (sprite != null)
            baseColor = sprite.color;
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

    private void OnDamaged(DamageInfo info)
    {
        if (sprite == null)
            return;

        if (flash != null)
            StopCoroutine(flash);

        flash = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        sprite.color = hitFlashColor;
        transform.localScale = baseScale * hitScale;

        yield return new WaitForSeconds(flashDuration);

        sprite.color = baseColor;
        transform.localScale = baseScale;
        flash = null;
    }

    private void OnDied()
    {
        if (body != null) body.enabled = false;
        if (sprite != null) sprite.enabled = false;
        Invoke(nameof(Respawn), respawnDelay);
    }

    private void Respawn()
    {
        health.ResetHealth();
        if (body != null) body.enabled = true;
        if (sprite != null)
        {
            sprite.color = baseColor;
            sprite.enabled = true;
        }
    }
}

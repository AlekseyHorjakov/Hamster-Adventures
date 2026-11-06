using UnityEngine;

/// <summary>
/// Тренировочный манекен: после смерти на время исчезает и встаёт обратно.
/// Вспышка и отлёт — задача HitReaction, здесь только жизненный цикл.
/// </summary>
[RequireComponent(typeof(Health))]
public class Dummy : MonoBehaviour
{
    [SerializeField] private float respawnDelay = 3f;

    private Health health;
    private SpriteRenderer sprite;
    private Collider2D body;
    private Rigidbody2D rigidbody2D;
    private Vector3 startPosition;

    private void Awake()
    {
        health = GetComponent<Health>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        body = GetComponent<Collider2D>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    private void OnEnable()
    {
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Died -= OnDied;
    }

    private void OnDied()
    {
        // Выключаем физику, иначе тело провалится сквозь землю.
        if (body != null) body.enabled = false;
        if (rigidbody2D != null) rigidbody2D.simulated = false;
        if (sprite != null) sprite.enabled = false;

        Invoke(nameof(Respawn), respawnDelay);
    }

    private void Respawn()
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;

        if (rigidbody2D != null)
        {
            rigidbody2D.simulated = true;
            rigidbody2D.linearVelocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
        }

        if (body != null) body.enabled = true;
        if (sprite != null) sprite.enabled = true;

        health.ResetHealth();
    }
}

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

    private static PhysicsMaterial2D frictionMaterial;

    /// <summary>
    /// Манекен не должен катиться по земле как по льду: тяжёлое тело
    /// с большим трением останавливается почти сразу после удара.
    /// </summary>
    private static PhysicsMaterial2D FrictionMaterial
    {
        get
        {
            if (frictionMaterial == null)
            {
                frictionMaterial = new PhysicsMaterial2D("DummyFriction")
                {
                    friction = 2f,
                    bounciness = 0f
                };
            }

            return frictionMaterial;
        }
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        body = GetComponent<Collider2D>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        startPosition = transform.position;

        if (body != null)
            body.sharedMaterial = FrictionMaterial;

        if (rigidbody2D != null)
        {
            rigidbody2D.mass = 6f;
            rigidbody2D.linearDamping = 0.5f;
        }
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

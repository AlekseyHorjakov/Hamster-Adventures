using UnityEngine;

/// <summary>
/// Разрушаемый ящик или бочка. Использует существующий Health,
/// поэтому корректно принимает удары через общий боевой конвейер.
/// </summary>
[RequireComponent(typeof(Health))]
public sealed class BreakableProp : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Sprite brokenSprite;
    [SerializeField] private GameObject pickupPrefab;

    private Health health;
    private Collider2D body;
    private bool broken;

    private void Awake()
    {
        health = GetComponent<Health>();
        body = GetComponent<Collider2D>();
        if (visual == null)
            visual = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.Died += OnDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= OnDied;
    }

    private void OnDied()
    {
        if (broken)
            return;

        broken = true;

        if (body != null)
            body.enabled = false;
        if (visual != null && brokenSprite != null)
            visual.sprite = brokenSprite;

        if (pickupPrefab != null)
            Instantiate(pickupPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
    }
}

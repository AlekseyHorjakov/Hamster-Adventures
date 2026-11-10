using UnityEngine;

/// <summary>Короткая вспышка и расходящееся кольцо в точке столкновения брошенного героя.</summary>
[DisallowMultipleComponent]
public class ThrowImpactVFX : MonoBehaviour
{
    [SerializeField] private SpriteRenderer ring;
    [SerializeField] private SpriteRenderer flash;
    [SerializeField] private float duration = 0.26f;
    [SerializeField] private float ringScale = 3.2f;
    [SerializeField] private float flashScale = 2.1f;

    private float elapsed;
    private int facing = 1;

    public void Play(int direction)
    {
        facing = direction >= 0 ? 1 : -1;
        transform.localScale = new Vector3(facing, 1f, 1f);
        elapsed = 0f;
        Apply(0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
        Apply(t);

        if (t >= 1f)
            Destroy(gameObject);
    }

    private void Apply(float t)
    {
        if (ring != null)
        {
            float scale = Mathf.Lerp(0.35f, ringScale, t);
            ring.transform.localScale = new Vector3(scale, scale, 1f);
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, 25f * t * facing);

            Color color = ring.color;
            color.a = 1f - t;
            ring.color = color;
        }

        if (flash != null)
        {
            float scale = Mathf.Lerp(0.4f, flashScale, Mathf.SmoothStep(0f, 1f, t));
            flash.transform.localScale = new Vector3(scale, scale, 1f);
            flash.transform.localRotation = Quaternion.Euler(0f, 0f, -18f * t * facing);

            Color color = flash.color;
            color.a = 1f - t * t;
            flash.color = color;
        }
    }
}

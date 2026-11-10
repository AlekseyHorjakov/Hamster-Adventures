using UnityEngine;

/// <summary>
/// Аркадный эффект превращения: тёплое свечение, энергетическое кольцо,
/// расходящиеся лучи, световой столб и вспышка. Все основные слои стоят
/// позади персонажа, чтобы силуэт халка оставался читаемым.
/// </summary>
[DisallowMultipleComponent]
public class HulkTransformVFXController : MonoBehaviour
{
    [Header("Слои")]
    [SerializeField] private SpriteRenderer aura;
    [SerializeField] private SpriteRenderer ring;
    [SerializeField] private SpriteRenderer pillar;
    [SerializeField] private SpriteRenderer flash;
    [SerializeField] private Transform rayRoot;
    [SerializeField] private SpriteRenderer[] rays;

    [Header("Длительность")]
    [SerializeField] private float duration = 0.7f;

    private Transform follow;
    private SpriteRenderer followSprite;
    private float elapsed;
    private float spin;
    private bool playing;

    public void Play(Transform target, SpriteRenderer targetSprite, float effectDuration)
    {
        follow = target;
        followSprite = targetSprite;
        duration = Mathf.Max(0.1f, effectDuration);
        elapsed = 0f;
        playing = true;
        Apply(0f);
    }

    private void LateUpdate()
    {
        if (!playing)
            return;

        elapsed += Time.deltaTime;

        if (follow != null)
        {
            Vector3 center = follow.position + Vector3.up * 1.2f;
            if (followSprite != null)
                center = followSprite.bounds.center;

            transform.position = new Vector3(center.x, center.y, follow.position.z - 0.15f);
        }

        float t = Mathf.Clamp01(elapsed / duration);
        Apply(t);

        if (t >= 1f)
        {
            playing = false;
            Destroy(gameObject, 0.1f);
        }
    }

    private void Apply(float t)
    {
        float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 2.5f));
        float fade = 1f - Mathf.SmoothStep(0.35f, 1f, t);
        float pulse = 0.82f + 0.18f * Mathf.Sin(t * 42f);

        if (aura != null)
        {
            float scale = Mathf.Lerp(0.8f, 4.6f, rise);
            aura.transform.localScale = new Vector3(scale, scale, 1f);
            Color color = aura.color;
            color.a = 0.9f * fade * pulse;
            aura.color = color;
        }

        if (ring != null)
        {
            float scale = Mathf.Lerp(0.45f, 5.2f, Mathf.SmoothStep(0f, 1f, t));
            ring.transform.localScale = new Vector3(scale, scale, 1f);
            Color color = ring.color;
            color.a = 0.85f * (1f - t);
            ring.color = color;
        }

        if (pillar != null)
        {
            float scaleY = Mathf.Lerp(0.8f, 7f, rise);
            pillar.transform.localScale = new Vector3(1.4f, scaleY, 1f);
            Color color = pillar.color;
            color.a = 0.75f * fade;
            pillar.color = color;
        }

        if (flash != null)
        {
            float flashPulse = Mathf.Sin(Mathf.Clamp01(t / 0.55f) * Mathf.PI);
            flash.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 3.6f, t);
            Color color = flash.color;
            color.a = Mathf.Clamp01(flashPulse) * 0.95f;
            flash.color = color;
        }

        if (rayRoot != null)
            rayRoot.localRotation *= Quaternion.Euler(0f, 0f, 85f * Time.deltaTime);

        spin += Time.deltaTime * 120f;

        if (rays != null)
        {
            foreach (SpriteRenderer ray in rays)
            {
                if (ray == null)
                    continue;

                float rayScale = Mathf.Lerp(0.6f, 2.4f, rise);
                ray.transform.localScale = new Vector3(rayScale, 0.8f + 0.4f * rise, 1f);
                Color color = ray.color;
                color.a = 0.85f * fade;
                ray.color = color;
            }
        }
    }
}

using UnityEngine;

/// <summary>
/// Мировые эффекты воздушного полёта: шлейфы, воздушные потоки, вихрь у кулака
/// и лёгкое искажение перед героем. Интенсивность задаётся из геймплея.
/// </summary>
[DisallowMultipleComponent]
public class PartnerThrowVFXController : MonoBehaviour
{
    [Header("Слои")]
    [SerializeField] private TrailRenderer[] speedTrails;
    [SerializeField] private SpriteRenderer[] airStreams;
    [SerializeField] private SpriteRenderer fistVortex;
    [SerializeField] private SpriteRenderer distortion;

    [Header("Настройки")]
    [SerializeField] private float maxTrailTime = 0.28f;
    [SerializeField] private float maxTrailWidth = 0.32f;
    [SerializeField] private float maxStreamAlpha = 0.72f;
    [SerializeField] private float maxVortexAlpha = 0.9f;
    [SerializeField] private float maxDistortionAlpha = 0.35f;
    [SerializeField] private float smoothing = 10f;

    private Transform follow;
    private SpriteRenderer followSprite;
    private int facing = 1;
    private float targetIntensity;
    private float intensity;
    private float phase;
    private bool stopping;

    private void Awake()
    {
        phase = Random.value * 10f;
    }

    public void Play(Transform target, SpriteRenderer targetSprite, int direction)
    {
        follow = target;
        followSprite = targetSprite;
        facing = direction >= 0 ? 1 : -1;
        transform.localScale = new Vector3(facing, 1f, 1f);
        targetIntensity = 0f;
        intensity = 0f;
        stopping = false;

        foreach (TrailRenderer trail in speedTrails)
        {
            if (trail == null) continue;
            trail.Clear();
            trail.emitting = true;
        }
    }

    public void SetIntensity(float value)
    {
        targetIntensity = Mathf.Clamp01(value);
    }

    private void LateUpdate()
    {
        if (follow != null)
        {
            Bounds bounds = followSprite != null
                ? followSprite.bounds
                : new Bounds(follow.position, Vector3.one);

            transform.position = new Vector3(bounds.center.x, bounds.center.y, follow.position.z - 0.02f);
        }

        intensity = Mathf.Lerp(intensity, targetIntensity, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

        UpdateTrails();
        UpdateStreams();
        UpdateVortex();

        if (stopping)
        {
            targetIntensity = 0f;
            if (intensity < 0.01f)
                Destroy(gameObject);
        }
    }

    private void UpdateTrails()
    {
        foreach (TrailRenderer trail in speedTrails)
        {
            if (trail == null) continue;
            trail.time = Mathf.Lerp(0.06f, maxTrailTime, intensity);
            trail.widthMultiplier = Mathf.Lerp(0.06f, maxTrailWidth, intensity);
        }
    }

    private void UpdateStreams()
    {
        phase += Time.deltaTime * (2f + intensity * 5f);

        for (int i = 0; i < airStreams.Length; i++)
        {
            SpriteRenderer stream = airStreams[i];
            if (stream == null) continue;

            float offset = Mathf.Sin(phase + i * 2.1f);
            stream.transform.localPosition = new Vector3(-0.15f + offset * 0.12f, (i == 0 ? 0.62f : -0.62f), 0f);
            stream.transform.localRotation = Quaternion.Euler(0f, 0f, offset * 12f);

            Color color = stream.color;
            color.a = Mathf.Lerp(0.08f, maxStreamAlpha, intensity) * (0.78f + 0.22f * Mathf.Abs(offset));
            stream.color = color;
        }
    }

    private void UpdateVortex()
    {
        if (fistVortex != null)
        {
            fistVortex.transform.localRotation *= Quaternion.Euler(0f, 0f, -(180f + intensity * 340f) * Time.deltaTime);
            float scale = 0.72f + intensity * 0.55f;
            fistVortex.transform.localScale = new Vector3(scale, scale, 1f);

            Color color = fistVortex.color;
            color.a = Mathf.Lerp(0.1f, maxVortexAlpha, intensity);
            fistVortex.color = color;
        }

        if (distortion != null)
        {
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 18f);
            distortion.transform.localScale = new Vector3(0.9f * pulse, 0.72f, 1f);

            Color color = distortion.color;
            color.a = Mathf.Lerp(0.04f, maxDistortionAlpha, intensity);
            distortion.color = color;
        }
    }

    public void StopAndDestroy()
    {
        if (stopping)
            return;

        stopping = true;
        follow = null;
        targetIntensity = 0f;

        foreach (TrailRenderer trail in speedTrails)
        {
            if (trail != null)
                trail.emitting = false;
        }

        Destroy(gameObject, 0.5f);
    }
}

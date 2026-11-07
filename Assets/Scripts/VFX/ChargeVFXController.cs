using UnityEngine;

/// <summary>
/// Один контроллер на весь огненный VFX усиленного удара.
/// Слои независимы, а наружу торчит только один параметр charge01 от 0 до 1:
///   GroundFire   — кольцо огня у ног (спрайтовый флипбук);
///   FlameParticles — языки пламени (частицы, анимация по листу);
///   FlyingEmbers — разлетающиеся искры (частицы);
///   FireAura     — большое пламя вокруг тела (спрайтовый флипбук);
///   GroundGlow   — трещины и свечение земли (кадр привязан к заряду);
///   Burst        — вспышка выпуска удара (одноразовый флипбук).
///
/// Сам персонаж не анимируется: эффект живёт на отдельных объектах
/// и может быть переиспользован для любой другой усиленной способности.
/// </summary>
[DisallowMultipleComponent]
public class ChargeVFXController : MonoBehaviour
{
    [Header("Спрайтовые слои")]
    [SerializeField] private SpriteRenderer groundFire;
    [SerializeField] private Sprite[] groundFireFrames;
    [SerializeField] private float groundFireFps = 11f;

    [SerializeField] private SpriteRenderer fireAura;
    [SerializeField] private Sprite[] fireAuraFrames;
    [SerializeField] private float fireAuraFps = 10f;

    [SerializeField] private SpriteRenderer groundGlow;
    [SerializeField] private Sprite[] groundGlowFrames;

    [SerializeField] private SpriteRenderer burstFlash;
    [SerializeField] private Sprite[] burstFlashFrames;
    [SerializeField] private float burstFlashDuration = 0.22f;

    [Header("Частицы")]
    [SerializeField] private ParticleSystem flames;
    [SerializeField] private ParticleSystem embers;

    [Header("Заряд 0..1")]
    [Range(0f, 1f)]
    [SerializeField] private float charge01;

    [Tooltip("Скорость сглаживания: интенсивность не должна дёргаться вслед за вводом")]
    [SerializeField] private float smoothSpeed = 12f;

    [Header("Максимальная интенсивность частиц")]
    [SerializeField] private float flamesRate = 30f;
    [SerializeField] private float embersRate = 12f;

    [Header("Масштаб слоёв")]
    [Tooltip("Кольцо-ореол: на старте заряда по груди, к концу охватывает весь силуэт")]
    [SerializeField] private float ringMinScale = 1.4f;
    [SerializeField] private float ringMaxScale = 2.2f;
    [SerializeField] private float auraMinScale = 1.4f;
    [SerializeField] private float auraMaxScale = 2f;
    [SerializeField] private float glowMinScale = 1f;
    [SerializeField] private float glowMaxScale = 2.4f;
    [SerializeField] private float flashScale = 2.2f;

    [Header("Выпуск удара")]
    [Tooltip("Сколько искр выбрасывается в момент удара")]
    [SerializeField] private int emberBurst = 18;

    private float current;
    private float flash;
    private float flickerSeed;
    private bool emitting;
    private float stopAt = -1f;
    private float ringTime;
    private float auraTime;
    private float flashTime = -1f;
    private int ringFrame = -1;
    private int auraFrame = -1;
    private int glowFrame = -1;

    public float Charge01 => charge01;

    private void Awake()
    {
        flickerSeed = Random.value * 100f;
        HideAll();
    }

    private void Update()
    {
        current = Mathf.Lerp(current, charge01, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        if (Mathf.Abs(current - charge01) < 0.002f)
            current = charge01;

        if (flash > 0f)
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime / Mathf.Max(0.01f, burstFlashDuration));

        // После удара заряд гаснет, а не обрывается: эмиссия плавно уходит в ноль.
        if (!emitting && stopAt > 0f && Time.time >= stopAt)
        {
            StopSystems(false);
            stopAt = -1f;
        }

        Apply(current);
        AnimateSprites();
    }

    /// <summary>Начать заряд: включает все слои и обнуляет интенсивность.</summary>
    public void Play()
    {
        emitting = true;
        stopAt = -1f;
        current = 0f;
        ringTime = 0f;
        auraTime = 0f;
        flashTime = -1f;
        flash = 0f;

        if (burstFlash != null)
        {
            Color color = burstFlash.color;
            color.a = 0f;
            burstFlash.color = color;
        }

        if (flames != null && !flames.isPlaying) flames.Play();
        if (embers != null && !embers.isPlaying) embers.Play();

        Apply(0f);
    }

    /// <summary>Текущий уровень заряда 0..1. Основной вход для геймплея.</summary>
    public void SetCharge(float value)
    {
        charge01 = Mathf.Clamp01(value);
    }

    /// <summary>Выпуск удара: вспышка, искры и быстрое рассеивание заряда.</summary>
    public void Burst()
    {
        flash = 1f;
        flashTime = 0f;
        charge01 = 0f;
        emitting = false;
        stopAt = Time.time + 0.35f;

        if (burstFlash != null && burstFlashFrames != null && burstFlashFrames.Length > 0)
        {
            burstFlash.sprite = burstFlashFrames[0];
            Color color = burstFlash.color;
            color.a = 1f;
            burstFlash.color = color;
            burstFlash.transform.localScale = new Vector3(flashScale * 0.5f, flashScale * 0.5f, 1f);
        }

        if (embers != null)
        {
            if (!embers.isPlaying)
                embers.Play();

            embers.Emit(emberBurst);
        }
    }

    /// <summary>Мгновенно убрать эффект, например при отмене заряда прыжком.</summary>
    public void Stop()
    {
        charge01 = 0f;
        current = 0f;
        flash = 0f;
        flashTime = -1f;
        emitting = false;
        stopAt = -1f;

        StopSystems(true);
        HideAll();
    }

    private void Apply(float value)
    {
        float t = Mathf.Clamp01(value);

        // Три ступени: 0–30% розжиг, 30–70% пламя, 70–100% аура и яркое свечение.
        float low = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.3f));
        float mid = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.25f) / 0.45f));
        float high = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.65f) / 0.35f));

        float fade = emitting ? 1f : (stopAt > 0f ? Mathf.Clamp01((stopAt - Time.time) / 0.35f) : 0f);

        SetEmission(flames, Mathf.Lerp(2f, flamesRate, mid) * fade);
        SetEmission(embers, Mathf.Lerp(0.5f, embersRate, mid) * fade);

        SetSize(flames, Mathf.Lerp(0.8f, 1.45f, mid));
        SetSize(embers, Mathf.Lerp(0.75f, 1.2f, mid));

        if (groundFire != null)
        {
            Color color = Color.white;
            color.a = Mathf.Lerp(0.25f, 1f, low) * fade;
            groundFire.color = color;

            float scale = Mathf.Lerp(ringMinScale, ringMaxScale, Mathf.Clamp01(low + flash * 0.3f));
            groundFire.transform.localScale = new Vector3(scale, scale, 1f);
        }

        if (fireAura != null)
        {
            Color color = Color.white;
            color.a = Mathf.Lerp(0f, 1f, high) * fade;
            fireAura.color = color;

            float scale = Mathf.Lerp(auraMinScale, auraMaxScale, Mathf.Clamp01(high + flash * 0.2f));
            fireAura.transform.localScale = new Vector3(scale, scale, 1f);
        }

        float glow = Mathf.Max(low * 0.35f, high);
        glow *= 0.88f + 0.12f * Mathf.PerlinNoise(Time.time * 5f, flickerSeed);
        glow = Mathf.Clamp01(glow + flash * 0.5f);

        if (groundGlow != null)
        {
            Color color = Color.white;
            color.a = glow * fade;
            groundGlow.color = color;

            float scale = Mathf.Lerp(glowMinScale, glowMaxScale, Mathf.Clamp01(Mathf.Max(low * 0.55f, high + flash * 0.3f)));
            groundGlow.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void AnimateSprites()
    {
        if (emitting && groundFire != null && groundFireFrames != null && groundFireFrames.Length > 1)
        {
            ringTime += Time.deltaTime * groundFireFps;
            int frame = Mathf.FloorToInt(ringTime) % groundFireFrames.Length;
            if (frame != ringFrame)
            {
                ringFrame = frame;
                groundFire.sprite = groundFireFrames[frame];
            }
        }

        if (emitting && fireAura != null && fireAuraFrames != null && fireAuraFrames.Length > 1)
        {
            auraTime += Time.deltaTime * fireAuraFps;
            int frame = Mathf.FloorToInt(auraTime) % fireAuraFrames.Length;
            if (frame != auraFrame)
            {
                auraFrame = frame;
                fireAura.sprite = fireAuraFrames[frame];
            }
        }

        // Трещины на земле раскрываются вместе с зарядом, а не крутятся по времени.
        if (groundGlow != null && groundGlowFrames != null && groundGlowFrames.Length > 0)
        {
            int frame = Mathf.Clamp(
                Mathf.RoundToInt(current * (groundGlowFrames.Length - 1)),
                0,
                groundGlowFrames.Length - 1);

            if (frame != glowFrame)
            {
                glowFrame = frame;
                groundGlow.sprite = groundGlowFrames[frame];
            }
        }

        if (flashTime >= 0f && burstFlash != null && burstFlashFrames != null && burstFlashFrames.Length > 0)
        {
            flashTime += Time.deltaTime;

            float progress = Mathf.Clamp01(flashTime / Mathf.Max(0.01f, burstFlashDuration));
            int frame = Mathf.Min(burstFlashFrames.Length - 1, Mathf.FloorToInt(progress * burstFlashFrames.Length));
            burstFlash.sprite = burstFlashFrames[frame];

            Color color = burstFlash.color;
            color.a = 1f - progress;
            burstFlash.color = color;

            float scale = Mathf.Lerp(flashScale * 0.5f, flashScale, progress);
            burstFlash.transform.localScale = new Vector3(scale, scale, 1f);

            if (progress >= 1f)
            {
                flashTime = -1f;
                color.a = 0f;
                burstFlash.color = color;
            }
        }
    }

    private void HideAll()
    {
        Hide(groundFire);
        Hide(fireAura);
        Hide(groundGlow);

        if (burstFlash != null)
        {
            Color color = burstFlash.color;
            color.a = 0f;
            burstFlash.color = color;
        }
    }

    private static void Hide(SpriteRenderer renderer)
    {
        if (renderer == null)
            return;

        Color color = renderer.color;
        color.a = 0f;
        renderer.color = color;
    }

    private static void SetEmission(ParticleSystem system, float rate)
    {
        if (system == null)
            return;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTimeMultiplier = rate;
    }

    private static void SetSize(ParticleSystem system, float size)
    {
        if (system == null)
            return;

        ParticleSystem.MainModule main = system.main;
        main.startSizeMultiplier = size;
    }

    private void StopSystems(bool clear)
    {
        StopSystem(flames, clear);
        StopSystem(embers, clear);
    }

    private static void StopSystem(ParticleSystem system, bool clear)
    {
        if (system == null)
            return;

        system.Stop(true, clear
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting);
    }

    /// <summary>Сборка префаба: разложить ссылки на слои и кадры.</summary>
    public void Configure(
        SpriteRenderer groundFireRenderer,
        Sprite[] groundFireAnimation,
        SpriteRenderer auraRenderer,
        Sprite[] auraAnimation,
        SpriteRenderer glowRenderer,
        Sprite[] glowAnimation,
        SpriteRenderer flashRenderer,
        Sprite[] flashAnimation,
        ParticleSystem flameSystem,
        ParticleSystem emberSystem)
    {
        groundFire = groundFireRenderer;
        groundFireFrames = groundFireAnimation;
        fireAura = auraRenderer;
        fireAuraFrames = auraAnimation;
        groundGlow = glowRenderer;
        groundGlowFrames = glowAnimation;
        burstFlash = flashRenderer;
        burstFlashFrames = flashAnimation;
        flames = flameSystem;
        embers = emberSystem;

        HideAll();
    }
}

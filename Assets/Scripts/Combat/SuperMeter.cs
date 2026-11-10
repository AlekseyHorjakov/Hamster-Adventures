using UnityEngine;

/// <summary>
/// Шкала суперудара. Заряжается за попадания по врагам и за полученный урон,
/// обновляет нижнюю полоску HUD и отдаёт заряд суперспособности.
/// </summary>
[DisallowMultipleComponent]
public class SuperMeter : MonoBehaviour
{
    [SerializeField] private float maxCharge = 100f;
    [Tooltip("Сколько заряда даёт одно успешное попадание по врагу")]
    [SerializeField] private float hitChargeGain = 20f;
    [Tooltip("Сколько заряда даёт полученный урон")]
    [SerializeField] private float damageChargeGain = 10f;
    [SerializeField] private float startingCharge;
    [SerializeField] private bool hamsterBar = true;
    [SerializeField] private Health health;

    private float charge;
    private bool gainLocked;
    private HealthHud hud;

    public float HitChargeGain => hitChargeGain;
    public float Normalized => maxCharge > 0f ? Mathf.Clamp01(charge / maxCharge) : 0f;
    public bool IsFull => maxCharge > 0f && charge >= maxCharge - 0.01f;

    private void Awake()
    {
        if (health == null) health = GetComponent<Health>();
        charge = Mathf.Clamp(startingCharge, 0f, maxCharge);
    }

    private void OnEnable()
    {
        if (health != null)
            health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Damaged -= OnDamaged;
    }

    private void Update()
    {
        if (hud == null)
            hud = FindFirstObjectByType<HealthHud>();

        if (hud == null)
            return;

        if (hamsterBar)
            hud.SetHamsterSuperCharge(Normalized);
        else
            hud.SetGirlSuperCharge(Normalized);
    }

    private void OnDamaged(DamageInfo info)
    {
        AddCharge(damageChargeGain);
    }

    public void AddCharge(float amount)
    {
        if (gainLocked || amount <= 0f)
            return;

        charge = Mathf.Clamp(charge + amount, 0f, maxCharge);
    }

    public bool ConsumeIfFull()
    {
        if (!IsFull)
            return false;

        charge = 0f;
        return true;
    }

    public void SetGainLocked(bool locked)
    {
        gainLocked = locked;
    }

    public void SetCharge(float value)
    {
        charge = Mathf.Clamp(value, 0f, maxCharge);
    }
}

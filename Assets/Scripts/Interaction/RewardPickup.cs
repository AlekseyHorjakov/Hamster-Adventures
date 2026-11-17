using UnityEngine;

/// <summary>Подбираемая награда: даёт заряд суперудара и исчезает.</summary>
[RequireComponent(typeof(Collider2D))]
public sealed class RewardPickup : MonoBehaviour
{
    [SerializeField] private float superCharge = 30f;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2.5f;

    private Vector3 basePosition;
    private float phase;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.isTrigger = true;

        basePosition = transform.position;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        transform.position = basePosition + Vector3.up * Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
            return;

        HamsterMovement player = other.GetComponentInParent<HamsterMovement>();
        if (player == null)
            return;

        SuperMeter meter = player.GetComponent<SuperMeter>();
        if (meter != null)
            meter.AddCharge(superCharge);

        LevelFlowController flow = FindFirstObjectByType<LevelFlowController>();
        if (flow != null)
            flow.ShowMessage("Награда: +" + Mathf.RoundToInt(superCharge) + " к суперудару");

        Destroy(gameObject);
    }
}

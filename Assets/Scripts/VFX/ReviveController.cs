using UnityEngine;

/// <summary>
/// Удержание кнопки взаимодействия рядом с поверженным напарником.
/// Спасатель нагибается и 3 секунды заливает зелье.
/// </summary>
[RequireComponent(typeof(PlayerCombatState))]
public class ReviveController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private HamsterMovement movement;
    [SerializeField] private AttackController attack;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private GameObject bubblePrefab;

    [Header("Оживление")]
    [SerializeField] private float reviveRange = 2.2f;
    [SerializeField] private float reviveDuration = 3f;
    [SerializeField] private float reviveHealthFraction = 0.3f;
    [Tooltip("За сколько секунд до конца оживления показать бабл")]
    [SerializeField] private float bubbleLeadTime = 2f;
    [SerializeField] private string reviveStateName = "Revive";
    [SerializeField] private string idleStateName = "Idle 0";
    [SerializeField] private string bubbleText = "УХУ ЕЛИ?";

    private PlayerCombatState[] players;
    private PlayerCombatState target;
    private GameObject bubble;
    private float progress;
    private bool active;

    private void Awake()
    {
        if (input == null) input = GetComponent<PlayerInputReader>();
        if (movement == null) movement = GetComponent<HamsterMovement>();
        if (attack == null) attack = GetComponent<AttackController>();
        if (animator == null) animator = GetComponent<Animator>();
        if (body == null) body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        PlayerCombatState self = GetComponent<PlayerCombatState>();
        if (self.IsDowned || self.IsDead)
        {
            Cancel();
            return;
        }

        PlayerCombatState candidate = FindCandidate();
        bool canRevive = input != null && input.InteractHeld && candidate != null &&
                         candidate.IsDowned && !candidate.IsDead &&
                         Vector2.Distance(transform.position, candidate.transform.position) <= reviveRange;

        if (!canRevive)
        {
            Cancel();
            return;
        }

        if (!active || target != candidate)
        {
            Cancel();
            Begin(candidate);
        }

        progress += Time.deltaTime;

        // Процесс длится 3 секунды: бабл появляется со второй секунды
        // и остаётся до завершения оживления.
        if (bubble == null && progress >= reviveDuration - bubbleLeadTime)
            ShowBubble();

        if (progress >= reviveDuration)
            Complete();
    }

    private PlayerCombatState FindCandidate()
    {
        if (players == null || players.Length == 0)
            players = FindObjectsByType<PlayerCombatState>(FindObjectsSortMode.None);

        PlayerCombatState best = null;
        float bestDistance = float.MaxValue;

        foreach (PlayerCombatState player in players)
        {
            if (player == null || player == GetComponent<PlayerCombatState>() || !player.IsDowned || player.IsDead)
                continue;

            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = player;
            }
        }

        return best;
    }

    private void Begin(PlayerCombatState newTarget)
    {
        active = true;
        target = newTarget;
        progress = 0f;

        if (movement != null)
        {
            movement.SetControlLocked(true);
            if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        if (attack != null) attack.enabled = false;
        if (animator != null) animator.Play(reviveStateName, 0, 0f);
    }

    private void Cancel()
    {
        if (!active) return;

        active = false;
        target = null;
        progress = 0f;
        HideBubble();

        if (attack != null) attack.enabled = true;
        if (movement != null) movement.SetControlLocked(false);
        if (animator != null) animator.Play(idleStateName, 0, 0f);
    }

    private void Complete()
    {
        if (target != null)
            target.Revive(reviveHealthFraction);
        Cancel();
    }

    private void ShowBubble()
    {
        if (bubblePrefab == null || bubble != null) return;

        bubble = Instantiate(bubblePrefab, transform);
        float parentScale = transform.lossyScale.y;
        if (parentScale > 0.0001f)
            bubble.transform.localScale = Vector3.one / parentScale;

        ReviveBubble text = bubble.GetComponent<ReviveBubble>();
        if (text != null) text.SetText(bubbleText);
        bubble.transform.position = transform.position + Vector3.up * 2.2f;
    }

    private void HideBubble()
    {
        if (bubble != null) Destroy(bubble);
        bubble = null;
    }
}

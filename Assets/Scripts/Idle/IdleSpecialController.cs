using UnityEngine;

/// <summary>
/// Включает особую анимацию бездействия (например, Хомячиха достаёт микрофон
/// и поёт), если игрок не давал ввода дольше заданного времени.
///
/// Схема из двух частей: вступление играется один раз (достаёт микрофон,
/// стучит по нему), затем зацикливается основная часть. Как только ввод
/// вернулся — сразу обычный покой, дальше Animator сам разберётся с бегом
/// или прыжком.
/// </summary>
[DisallowMultipleComponent]
public class IdleSpecialController : MonoBehaviour
{
    [SerializeField] private HamsterMovement movement;
    [SerializeField] private Animator animator;

    [Tooltip("Обычное состояние покоя, к которому возвращаемся")]
    [SerializeField] private string idleState = "Idle";

    [Tooltip("Вступление: играется один раз. Пусто — сразу к циклу")]
    [SerializeField] private string introState = "";

    [Tooltip("Зацикленное состояние особой анимации бездействия")]
    [SerializeField] private string specialState = "SingIdle";

    [Tooltip("Сколько секунд без ввода ждать перед особым бездействием")]
    [SerializeField] private float idleDelay = 5f;

    [Tooltip("Показывать особую анимацию только на земле")]
    [SerializeField] private bool requireGrounded = true;

    private enum Phase
    {
        None,
        Intro,
        Loop
    }

    private Phase phase;
    private float lastSeenInput;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<HamsterMovement>();

        if (animator == null)
            animator = GetComponent<Animator>();

        lastSeenInput = movement != null ? movement.LastInputTime : Time.time;
    }

    private void Update()
    {
        if (movement == null || animator == null)
            return;

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);

        if (movement.LastInputTime > lastSeenInput)
        {
            lastSeenInput = movement.LastInputTime;

            if (phase != Phase.None)
            {
                // Специальное состояние могло уже смениться ударом или прыжком —
                // тогда не перебиваем его покоем.
                if (current.IsName(introState) || current.IsName(specialState))
                    animator.Play(idleState, 0, 0f);

                phase = Phase.None;
            }

            return;
        }

        if (phase == Phase.Intro)
        {
            if (current.IsName(introState) && current.normalizedTime >= 1f)
            {
                animator.Play(specialState, 0, 0f);
                phase = Phase.Loop;
            }
            else if (!current.IsName(introState))
            {
                // Вступление прервали снаружи — сбрасываемся.
                phase = Phase.None;
            }

            return;
        }

        if (phase == Phase.Loop)
        {
            if (!current.IsName(specialState))
                phase = Phase.None;

            return;
        }

        if (Time.time - movement.LastInputTime >= idleDelay &&
            (!requireGrounded || movement.Grounded) &&
            current.IsName(idleState))
        {
            if (!string.IsNullOrEmpty(introState))
            {
                animator.Play(introState, 0, 0f);
                phase = Phase.Intro;
            }
            else
            {
                animator.Play(specialState, 0, 0f);
                phase = Phase.Loop;
            }
        }
    }
}

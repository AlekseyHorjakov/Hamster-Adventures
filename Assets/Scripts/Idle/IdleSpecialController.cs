using UnityEngine;

/// <summary>
/// Включает особую анимацию бездействия (например, Хомячиха достаёт микрофон
/// и поёт), если игрок не давал ввода дольше заданного времени.
/// Как только ввод вернулся — сразу обычный покой, дальше Animator сам
/// разберётся с бегом или прыжком.
/// </summary>
[DisallowMultipleComponent]
public class IdleSpecialController : MonoBehaviour
{
    [SerializeField] private HamsterMovement movement;
    [SerializeField] private Animator animator;

    [Tooltip("Обычное состояние покоя, к которому возвращаемся")]
    [SerializeField] private string idleState = "Idle";

    [Tooltip("Состояние особой анимации бездействия")]
    [SerializeField] private string specialState = "SingIdle";

    [Tooltip("Сколько секунд без ввода ждать перед особым бездействием")]
    [SerializeField] private float idleDelay = 5f;

    [Tooltip("Показывать особую анимацию только на земле")]
    [SerializeField] private bool requireGrounded = true;

    private bool playing;
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

        if (movement.LastInputTime > lastSeenInput)
        {
            lastSeenInput = movement.LastInputTime;

            if (playing)
            {
                // Специальное состояние могло уже смениться ударом или прыжком —
                // тогда не перебиваем его покоем.
                if (animator.GetCurrentAnimatorStateInfo(0).IsName(specialState))
                    animator.Play(idleState, 0, 0f);

                playing = false;
            }

            return;
        }

        // Если состояние прервали снаружи (попадание, смена сцены), сбрасываем флаг.
        if (playing && !animator.GetCurrentAnimatorStateInfo(0).IsName(specialState))
            playing = false;

        if (playing)
            return;

        if (Time.time - movement.LastInputTime >= idleDelay &&
            (!requireGrounded || movement.Grounded) &&
            animator.GetCurrentAnimatorStateInfo(0).IsName(idleState))
        {
            animator.Play(specialState, 0, 0f);
            playing = true;
        }
    }
}

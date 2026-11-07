
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HamsterMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Air Movement")]
    [SerializeField] private float airAcceleration = 60f;
    [SerializeField] private float airDeceleration = 70f;

    [Header("Controls")]
    [SerializeField] private Key leftKey = Key.A;
    [SerializeField] private Key rightKey = Key.D;
    [SerializeField] private Key jumpKey = Key.W;
    [SerializeField] private Key attackKey = Key.J;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private int maxJumps = 2;

    [Header("Partner Distance")]
    [SerializeField] private Transform partner;
    [SerializeField] private float maxCameraSize = 15f;
    [SerializeField] private float screenMargin = 4f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Camera mainCamera;

    private float moveInput;
    private int jumpsRemaining;
    private bool isGrounded;

    // Блокировка управления на время удара — её ставит AttackController.
    private bool controlLocked;

    // Нажатие атаки запоминаем здесь, а забирает AttackController.
    private bool attackPressed;

    // Пока true, горизонтальную скорость задаёт удар (рывок), а не ввод.
    private bool externalHorizontalControl;

    // Удержание и отпускание нужны заряженному удару: заряд копится, пока
    // кнопка нажата, а сам удар выполняется на отпускании.
    private bool attackHeld;
    private bool attackReleased;

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int IsGroundedHash =
        Animator.StringToHash("IsGrounded");

    private static readonly int IsJumpingHash =
        Animator.StringToHash("IsJumping");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Спрайт живёт на дочернем объекте Visual, чтобы анимации масштаба
        // не задевали коллайдер, поэтому ищем рендерер во вложенных объектах.
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        if (spriteRenderer == null)
            Debug.LogWarning("[HamsterMovement] SpriteRenderer не найден у " + name +
                             " — разворот персонажа работать не будет");

        jumpsRemaining = maxJumps;
        LastInputTime = Time.time;
    }

    private void Update()
    {
        ReadMovementInput();
        ReadJumpInput();
        ReadAttackInput();
        UpdateAnimation();

        // Для «особого бездействия»: любой ввод сбрасывает таймер.
        bool anyKey = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

        if (anyKey || Mathf.Abs(moveInput) > 0.01f || attackHeld)
            LastInputTime = Time.time;
    }

    private void ReadMovementInput()
    {
        moveInput = 0f;

        if (controlLocked)
            return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current[leftKey].isPressed)
                moveInput = -1f;
            else if (Keyboard.current[rightKey].isPressed)
                moveInput = 1f;
        }

        if (spriteRenderer != null)
        {
            if (moveInput < 0f)
                spriteRenderer.flipX = true;
            else if (moveInput > 0f)
                spriteRenderer.flipX = false;
        }
    }

    private void ReadJumpInput()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[jumpKey].wasPressedThisFrame &&
            jumpsRemaining > 0)
        {
            // Не разрешаем прыгать за верхнюю границу
            // области, доступной обоим игрокам.
            if (partner != null &&
                transform.position.y >= partner.position.y +
                GetMaxVerticalSeparation() * 0.5f)
            {
                return;
            }

            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

            jumpsRemaining--;
            isGrounded = false;
        }
    }

    private void ReadAttackInput()
    {
        if (Keyboard.current == null)
        {
            attackHeld = false;
            return;
        }

        var attackControl = Keyboard.current[attackKey];

        attackHeld = attackControl.isPressed;

        if (attackControl.wasPressedThisFrame)
            attackPressed = true;

        if (attackControl.wasReleasedThisFrame)
            attackReleased = true;
    }

    /// <summary>Куда смотрит герой: 1 — вправо, -1 — влево.</summary>
    public int Facing => spriteRenderer != null && spriteRenderer.flipX ? -1 : 1;

    /// <summary>Текущий ввод по горизонтали. Только для диагностики.</summary>
    public float MoveInput => moveInput;

    /// <summary>Заблокировано ли управление ударом. Только для диагностики.</summary>
    public bool ControlLocked => controlLocked;

    /// <summary>Стоит ли герой на земле. Только для диагностики.</summary>
    public bool Grounded => isGrounded;

    /// <summary>Когда последний раз был хоть какой-то ввод. Нужно особому бездействию.</summary>
    public float LastInputTime { get; private set; }

    /// <summary>Физическое тело — нужно удару для шага вперёд.</summary>
    public Rigidbody2D Body => rb;

    /// <summary>Запрещает или разрешает движение (используется во время удара).</summary>
    public void SetControlLocked(bool locked)
    {
        controlLocked = locked;

        if (locked)
            moveInput = 0f;
    }

    /// <summary>Возвращает true ровно один раз на каждое нажатие атаки.</summary>
    public bool ConsumeAttackPress()
    {
        if (!attackPressed)
            return false;

        attackPressed = false;
        return true;
    }

    /// <summary>Держит ли игрок кнопку атаки прямо сейчас.</summary>
    public bool AttackHeld => attackHeld;

    /// <summary>
    /// Передаёт управление горизонтальной скоростью удару. Пока включено,
    /// скрипт движения не переписывает скорость и не тянет героя к напарнику.
    /// </summary>
    public void SetExternalHorizontalControl(bool enabled)
    {
        externalHorizontalControl = enabled;

        if (enabled)
            moveInput = 0f;
    }

    /// <summary>Возвращает true ровно один раз на каждое отпускание атаки.</summary>
    public bool ConsumeAttackRelease()
    {
        if (!attackReleased)
            return false;

        attackReleased = false;
        return true;
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool(
            IsMovingHash,
            Mathf.Abs(moveInput) > 0.01f
        );

        animator.SetBool(
            IsGroundedHash,
            isGrounded
        );

        animator.SetBool(
            IsJumpingHash,
            !isGrounded
        );
    }

    private void FixedUpdate()
    {
        // Во время рывка горизонтальную скорость задаёт удар: если трогать её
        // здесь, движение первым же кадром обнулит рывок.
        if (externalHorizontalControl)
            return;

        float targetSpeed = moveInput * moveSpeed;

        if (partner != null)
        {
            Vector2 position = rb.position;
            Vector2 partnerPosition = partner.position;

            float maxHorizontalSeparation =
                GetMaxHorizontalSeparation();

            float maxVerticalSeparation =
                GetMaxVerticalSeparation();

            float distanceX = position.x - partnerPosition.x;
            float distanceY = position.y - partnerPosition.y;

            // Ограничиваем движение по горизонтали.
            bool movingAwayHorizontally =
                (distanceX > 0f && moveInput > 0f) ||
                (distanceX < 0f && moveInput < 0f);

            if (Mathf.Abs(distanceX) >=
                maxHorizontalSeparation * 0.5f &&
                movingAwayHorizontally)
            {
                targetSpeed = 0f;

                if ((distanceX > 0f && rb.linearVelocity.x > 0f) ||
                    (distanceX < 0f && rb.linearVelocity.x < 0f))
                {
                    rb.linearVelocity = new Vector2(
                        0f,
                        rb.linearVelocity.y
                    );
                }
            }

            // Ограничиваем подъём одного героя относительно другого.
            bool movingAwayVertically =
                (distanceY > 0f && rb.linearVelocity.y > 0f) ||
                (distanceY < 0f && rb.linearVelocity.y < 0f);

            if (Mathf.Abs(distanceY) >=
                maxVerticalSeparation * 0.5f &&
                movingAwayVertically)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    0f
                );
            }
        }

        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(
                targetSpeed,
                rb.linearVelocity.y
            );
        }
        else
        {
            float acceleration = moveInput == 0f
                ? airDeceleration
                : airAcceleration;

            float newSpeed = Mathf.MoveTowards(
                rb.linearVelocity.x,
                targetSpeed,
                acceleration * Time.fixedDeltaTime
            );

            rb.linearVelocity = new Vector2(
                newSpeed,
                rb.linearVelocity.y
            );
        }
    }

    private float GetMaxHorizontalSeparation()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null || !mainCamera.orthographic)
            return 20f;

        return 2f * Mathf.Max(
            1f,
            maxCameraSize * mainCamera.aspect - screenMargin
        );
    }

    private float GetMaxVerticalSeparation()
    {
        return 2f * Mathf.Max(
            1f,
            maxCameraSize - screenMargin
        );
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckGround(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckGround(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isGrounded = false;
    }

    private void CheckGround(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);

            if (contact.normal.y > 0.5f &&
                rb.linearVelocity.y <= 0.1f)
            {
                if (!isGrounded)
                    jumpsRemaining = maxJumps;

                isGrounded = true;
                return;
            }
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HamsterMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Ground Feel")]
    [Tooltip("Ускорение разгона на земле")]
    [SerializeField] private float groundAcceleration = 75f;
    [Tooltip("Торможение при отпускании ввода")]
    [SerializeField] private float groundDeceleration = 100f;
    [Tooltip("Ускорение при попытке развернуться против текущей скорости")]
    [SerializeField] private float turnAcceleration = 130f;

    [Header("Air Movement")]
    [SerializeField] private float airAcceleration = 60f;
    [SerializeField] private float airDeceleration = 70f;
    [Tooltip("Множитель торможения в воздухе без ввода")]
    [SerializeField] private float airStopMultiplier = 0.65f;

    [Header("Controls")]
    [SerializeField] private Key leftKey = Key.A;
    [SerializeField] private Key rightKey = Key.D;
    [SerializeField] private Key jumpKey = Key.W;
    [SerializeField] private Key attackKey = Key.J;

    [Tooltip("Новый ввод из InputActionAsset; если пусто — старые клавиши выше")]
    [SerializeField] private PlayerInputReader input;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private int maxJumps = 2;
    [Tooltip("Сколько секунд после схода с земли разрешён прыжок")]
    [SerializeField] private float coyoteTime = 0.10f;
    [Tooltip("Сколько секунд нажатие прыжка ждёт возможности сработать")]
    [SerializeField] private float jumpBufferTime = 0.12f;
    [Tooltip("Множитель вертикальной скорости при коротком нажатии")]
    [SerializeField] private float jumpCutMultiplier = 0.52f;
    [Tooltip("Окно, в котором отпускание кнопки обрезает прыжок")]
    [SerializeField] private float jumpCutWindow = 0.14f;

    [Header("Fall Feel")]
    [SerializeField] private float riseGravityMultiplier = 1f;
    [SerializeField] private float fallGravityMultiplier = 1.55f;
    [SerializeField] private float maxFallSpeed = 26f;

    [Header("Landing")]
    [Tooltip("Минимальная скорость падения, после которой сообщаем о приземлении")]
    [SerializeField] private float landingFeedbackThreshold = 2f;

    [Header("Hit Reaction Physics")]
    [Tooltip("Торможение горизонтального отброса на земле")]
    [SerializeField] private float knockbackGroundDeceleration = 35f;
    [Tooltip("Торможение горизонтального отброса в воздухе")]
    [SerializeField] private float knockbackAirDeceleration = 2f;

    [Header("Partner Distance")]
    [SerializeField] private Transform partner;
    [SerializeField] private float maxCameraSize = 15f;
    [SerializeField] private float screenMargin = 4f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private HitReaction hitReaction;
    private Camera mainCamera;

    private float moveInput;
    private float runtimeSpeedMultiplier = 1f;
    private float runtimeJumpMultiplier = 1f;
    private int jumpsRemaining;
    private bool isGrounded;
    private bool jumpLocked;
    private float baseGravityScale = 2.5f;

    private float coyoteUntil;
    private float jumpBufferedUntil;
    private float jumpStartedAt;
    private bool jumpCutArmed;
    private bool jumpHeld;
    private bool jumpReleasedThisFrame;
    private float previousVerticalVelocity;
    private int lastDirection = 1;
    private bool wasMoving;

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

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");

    public event Action Jumped;
    public event Action<float> Landed;
    public event Action StartedMoving;
    public event Action StoppedMoving;
    public event Action<int> DirectionChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<Animator>();
        hitReaction = GetComponent<HitReaction>();
        mainCamera = Camera.main;

        if (input == null)
            input = GetComponent<PlayerInputReader>();

        if (spriteRenderer == null)
            Debug.LogWarning("[HamsterMovement] SpriteRenderer не найден у " + name +
                             " — разворот персонажа работать не будет");

        baseGravityScale = rb != null && rb.gravityScale > 0.001f ? rb.gravityScale : 2.5f;
        jumpsRemaining = maxJumps;
        LastInputTime = Time.time;
    }

    private void Update()
    {
        ReadMovementInput();
        ReadJumpInput();
        TryBufferedJump();
        HandleJumpCut();
        ReadAttackInput();
        UpdateAnimation();
        UpdateMotionEvents();

        // Для «особого бездействия»: любой ввод сбрасывает таймер.
        bool anyKey = input != null
            ? input.AnyPressed
            : Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

        if (anyKey || Mathf.Abs(moveInput) > 0.01f || attackHeld)
            LastInputTime = Time.time;
    }

    private void ReadMovementInput()
    {
        moveInput = 0f;

        if (controlLocked)
            return;

        if (input != null)
        {
            moveInput = Mathf.Clamp(input.Move, -1f, 1f);
        }
        else if (Keyboard.current != null)
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
        bool jumpPressed = input != null
            ? input.JumpPressed
            : Keyboard.current != null && Keyboard.current[jumpKey].wasPressedThisFrame;

        jumpHeld = input != null
            ? input.JumpHeld
            : Keyboard.current != null && Keyboard.current[jumpKey].isPressed;

        jumpReleasedThisFrame = input != null
            ? input.JumpReleased
            : Keyboard.current != null && Keyboard.current[jumpKey].wasReleasedThisFrame;

        if (jumpPressed && !jumpLocked)
            jumpBufferedUntil = Time.time + jumpBufferTime;
    }

    private void TryBufferedJump()
    {
        if (jumpLocked || Time.time > jumpBufferedUntil || jumpsRemaining <= 0)
            return;

        // Не разрешаем прыгать за верхнюю границу области, доступной обоим игрокам.
        if (partner != null &&
            transform.position.y >= partner.position.y + GetMaxVerticalSeparation() * 0.5f)
        {
            return;
        }

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce * runtimeJumpMultiplier
        );

        jumpsRemaining--;
        isGrounded = false;
        coyoteUntil = 0f;
        jumpBufferedUntil = 0f;
        jumpStartedAt = Time.time;
        jumpCutArmed = true;
        Jumped?.Invoke();
    }

    private void HandleJumpCut()
    {
        if (rb == null)
            return;

        if (rb.linearVelocity.y <= 0.05f)
        {
            jumpCutArmed = false;
            return;
        }

        if (!jumpCutArmed || !jumpReleasedThisFrame)
            return;

        if (Time.time - jumpStartedAt > jumpCutWindow)
        {
            jumpCutArmed = false;
            return;
        }

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            rb.linearVelocity.y * jumpCutMultiplier
        );
        jumpCutArmed = false;
    }

    private void ReadAttackInput()
    {
        if (input != null)
        {
            attackHeld = input.AttackHeld;

            if (input.AttackPressed)
                attackPressed = true;

            if (input.AttackReleased)
                attackReleased = true;

            return;
        }

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

    public bool IsRising => rb != null && rb.linearVelocity.y > 0.05f;
    public bool IsFalling => rb != null && rb.linearVelocity.y < -0.05f;
    public float HorizontalSpeed => rb != null ? rb.linearVelocity.x : 0f;
    public float VerticalSpeed => rb != null ? rb.linearVelocity.y : 0f;

    /// <summary>Когда последний раз был хоть какой-то ввод. Нужно особому бездействию.</summary>
    public float LastInputTime { get; private set; }

    /// <summary>Физическое тело — нужно удару для шага вперёд.</summary>
    public Rigidbody2D Body => rb;

    /// <summary>Временные множители скорости и прыжка для суперформы.</summary>
    public void SetRuntimeMovementMultipliers(float speedMultiplier, float jumpMultiplier)
    {
        runtimeSpeedMultiplier = Mathf.Max(0.01f, speedMultiplier);
        runtimeJumpMultiplier = Mathf.Max(0.01f, jumpMultiplier);
    }

    /// <summary>Назначает или сбрасывает напарника для ограничения дистанции.</summary>
    public void SetPartner(Transform partnerTransform)
    {
        partner = partnerTransform;
    }

    public void SetControlLocked(bool locked)
    {
        controlLocked = locked;

        if (locked)
            moveInput = 0f;
    }

    /// <summary>Отдельная блокировка прыжка на время hurt/downed/death.</summary>
    public void SetJumpLocked(bool locked)
    {
        jumpLocked = locked;

        if (locked)
            jumpBufferedUntil = 0f;
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

        float horizontalSpeed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
        bool moving = horizontalSpeed > 0.15f || Mathf.Abs(moveInput) > 0.01f;

        animator.SetBool(IsMovingHash, moving);
        animator.SetBool(IsGroundedHash, isGrounded);
        animator.SetBool(IsJumpingHash, !isGrounded);
    }

    private void UpdateMotionEvents()
    {
        if (rb == null)
            return;

        float horizontal = rb.linearVelocity.x;
        bool moving = Mathf.Abs(horizontal) > 0.2f;

        if (moving && !wasMoving)
            StartedMoving?.Invoke();
        else if (!moving && wasMoving)
            StoppedMoving?.Invoke();

        wasMoving = moving;

        if (Mathf.Abs(horizontal) > 0.2f)
        {
            int direction = horizontal > 0f ? 1 : -1;
            if (direction != lastDirection)
            {
                lastDirection = direction;
                DirectionChanged?.Invoke(direction);
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        previousVerticalVelocity = rb.linearVelocity.y;

        // Во время рывка горизонтальную скорость задаёт удар: если трогать её
        // здесь, движение первым же кадром обнулит рывок.
        if (!externalHorizontalControl)
            UpdateHorizontalMovement();

        ApplyGravityFeel();
    }

    private void UpdateHorizontalMovement()
    {
        // Во время hit reaction физикой отброса управляет HitReaction.
        // Не обнуляем её мгновенно, а даём телу погасить скорость.
        if (hitReaction != null && hitReaction.IsReacting)
        {
            float deceleration = isGrounded
                ? knockbackGroundDeceleration
                : knockbackAirDeceleration;
            float slowedSpeed = Mathf.MoveTowards(
                rb.linearVelocity.x,
                0f,
                deceleration * Time.fixedDeltaTime
            );
            rb.linearVelocity = new Vector2(slowedSpeed, rb.linearVelocity.y);
            return;
        }

        float targetSpeed = moveInput * moveSpeed * runtimeSpeedMultiplier;

        if (partner != null && partner.gameObject.activeInHierarchy)
        {
            Vector2 position = rb.position;
            Vector2 partnerPosition = partner.position;

            float maxHorizontalSeparation = GetMaxHorizontalSeparation();
            float maxVerticalSeparation = GetMaxVerticalSeparation();

            float distanceX = position.x - partnerPosition.x;
            float distanceY = position.y - partnerPosition.y;

            bool movingAwayHorizontally =
                (distanceX > 0f && moveInput > 0f) ||
                (distanceX < 0f && moveInput < 0f);

            if (Mathf.Abs(distanceX) >= maxHorizontalSeparation * 0.5f &&
                movingAwayHorizontally)
            {
                targetSpeed = 0f;

                if ((distanceX > 0f && rb.linearVelocity.x > 0f) ||
                    (distanceX < 0f && rb.linearVelocity.x < 0f))
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
            }

            bool movingAwayVertically =
                (distanceY > 0f && rb.linearVelocity.y > 0f) ||
                (distanceY < 0f && rb.linearVelocity.y < 0f);

            if (Mathf.Abs(distanceY) >= maxVerticalSeparation * 0.5f &&
                movingAwayVertically)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }
        }

        float currentSpeed = rb.linearVelocity.x;
        float acceleration;

        if (isGrounded)
        {
            if (Mathf.Abs(moveInput) < 0.01f)
                acceleration = groundDeceleration;
            else if (Mathf.Abs(currentSpeed) > 0.15f &&
                     Mathf.Sign(currentSpeed) != Mathf.Sign(moveInput))
                acceleration = turnAcceleration;
            else
                acceleration = groundAcceleration;
        }
        else
        {
            acceleration = Mathf.Abs(moveInput) < 0.01f
                ? airDeceleration * airStopMultiplier
                : airAcceleration;
        }

        float newSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
    }

    private void ApplyGravityFeel()
    {
        float gravityMultiplier = isGrounded
            ? 1f
            : rb.linearVelocity.y > 0.05f
                ? riseGravityMultiplier
                : fallGravityMultiplier;

        rb.gravityScale = baseGravityScale * Mathf.Max(0.01f, gravityMultiplier);

        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                -maxFallSpeed
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
        return 2f * Mathf.Max(1f, maxCameraSize - screenMargin);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IgnoreEnemyContact(collision))
            return;

        CheckGround(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IgnoreEnemyContact(collision))
            return;

        CheckGround(collision);
    }

    /// <summary>
    /// Враги, манекены и предметы с Health не должны быть физической опорой:
    /// иначе герой цепляется за их боковые грани в прыжке. Атаки работают
    /// через OverlapBox хитбокса и от этого игнорирования не страдают.
    /// </summary>
    private bool IgnoreEnemyContact(Collision2D collision)
    {
        if (collision.collider == null)
            return false;

        Health otherHealth = collision.collider.GetComponentInParent<Health>();
        if (otherHealth == null)
            return false;

        // Между двумя игроками физические столкновения сохраняем.
        if (collision.collider.GetComponentInParent<HamsterMovement>() != null)
            return false;

        Collider2D ownCollider = GetComponent<Collider2D>();
        if (ownCollider != null)
            Physics2D.IgnoreCollision(ownCollider, collision.collider, true);

        return true;
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
                {
                    jumpsRemaining = maxJumps;
                    coyoteUntil = Time.time + coyoteTime;
                    jumpCutArmed = false;

                    float impactSpeed = Mathf.Abs(previousVerticalVelocity);
                    if (impactSpeed >= landingFeedbackThreshold)
                        Landed?.Invoke(impactSpeed);
                }

                isGrounded = true;
                return;
            }
        }
    }
}

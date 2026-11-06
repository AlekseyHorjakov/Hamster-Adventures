
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

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int IsGroundedHash =
        Animator.StringToHash("IsGrounded");

    private static readonly int IsJumpingHash =
        Animator.StringToHash("IsJumping");

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        jumpsRemaining = maxJumps;
    }

    private void Update()
    {
        ReadMovementInput();
        ReadJumpInput();
        ReadAttackInput();
        UpdateAnimation();
    }

    private void ReadMovementInput()
    {
        moveInput = 0f;

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
        if (Keyboard.current == null || animator == null)
            return;

        if (Keyboard.current[attackKey].wasPressedThisFrame)
        {
            animator.SetTrigger(AttackHash);
        }
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

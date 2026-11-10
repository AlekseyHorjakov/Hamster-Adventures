using UnityEngine;

/// <summary>
/// Показывает отдельный статичный спрайт героя с поднятыми руками, пока он
/// держит напарника и стоит на месте. При движении возвращает обычную
/// анимацию Animator, поэтому бег с напарником остаётся стандартным.
/// </summary>
[DisallowMultipleComponent]
public class CarryPoseController : MonoBehaviour
{
    [SerializeField] private PartnerThrowController throwController;
    [SerializeField] private HamsterMovement movement;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private SpriteRenderer mainRenderer;
    [SerializeField] private SpriteRenderer carryRenderer;
    [SerializeField] private float horizontalMoveThreshold = 0.08f;

    private bool poseVisible;

    private void Awake()
    {
        if (throwController == null) throwController = GetComponent<PartnerThrowController>();
        if (movement == null) movement = GetComponent<HamsterMovement>();
        if (body == null) body = GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        bool shouldShow = throwController != null &&
                          throwController.IsCarryingPartner &&
                          !throwController.IsBeingCarried &&
                          !throwController.IsFlying &&
                          movement != null &&
                          movement.Grounded &&
                          Mathf.Abs(movement.MoveInput) < 0.01f &&
                          (body == null || Mathf.Abs(body.linearVelocity.x) < horizontalMoveThreshold);

        SetPoseVisible(shouldShow);

        if (poseVisible && carryRenderer != null && mainRenderer != null)
            carryRenderer.flipX = mainRenderer.flipX;
    }

    private void OnDisable()
    {
        SetPoseVisible(false);
    }

    private void SetPoseVisible(bool visible)
    {
        if (poseVisible == visible)
            return;

        poseVisible = visible;

        if (carryRenderer != null)
            carryRenderer.enabled = visible;

        if (mainRenderer != null)
            mainRenderer.enabled = !visible;
    }
}


using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform hamster;
    [SerializeField] private Transform hamsterGirl;

    [Header("Camera Zoom")]
    [SerializeField] private float normalSize = 10f;
    [SerializeField] private float maxSize = 15f;
    [SerializeField] private float zoomPadding = 3f;
    [SerializeField] private float zoomSmoothTime = 0.3f;

    [Header("Camera Movement")]
    [SerializeField] private float moveSmoothTime = 0.15f;

    private Camera cam;
    private float zoomVelocity;
    private Vector3 moveVelocity;

    public float MaxSize => maxSize;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (hamster == null || hamsterGirl == null)
            return;

        Vector3 midpoint = (hamster.position + hamsterGirl.position) * 0.5f;

        Vector3 targetPosition = new Vector3(
            midpoint.x,
            midpoint.y,
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref moveVelocity,
            moveSmoothTime
        );

        float distanceX = Mathf.Abs(hamster.position.x - hamsterGirl.position.x);
        float distanceY = Mathf.Abs(hamster.position.y - hamsterGirl.position.y);

        float requiredHeight = distanceY * 0.5f + zoomPadding;
        float requiredWidth = (distanceX * 0.5f + zoomPadding) / cam.aspect;

        float targetSize = Mathf.Clamp(
            Mathf.Max(normalSize, requiredHeight, requiredWidth),
            normalSize,
            maxSize
        );

        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize,
            targetSize,
            ref zoomVelocity,
            zoomSmoothTime
        );
    }
}

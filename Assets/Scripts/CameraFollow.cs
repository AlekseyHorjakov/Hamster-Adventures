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

    [Header("Level Bounds")]
    [SerializeField] private bool useBounds;
    [SerializeField] private Vector2 boundsMin = new Vector2(-100f, -20f);
    [SerializeField] private Vector2 boundsMax = new Vector2(100f, 40f);
    [Header("Camera Movement")]
    [SerializeField] private float moveSmoothTime = 0.15f;

    [Header("Shake")]
    [Tooltip("Максимальное смещение камеры при тряске, в юнитах")]
    [SerializeField] private float maxShakeOffset = 0.3f;

    private Camera cam;
    private float zoomVelocity;
    private Vector3 moveVelocity;

    // Сглаженная позиция без тряски: тряска добавляется поверх,
    // чтобы не сбивать следование за игроками.
    private Vector3 smoothedPosition;

    private float shakeTime;
    private float shakeDuration;
    private float shakeStrength;

    public float MaxSize => maxSize;

    private static CameraFollow instance;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        instance = this;
        smoothedPosition = transform.position;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    /// <summary>Тряска камеры. strength — 0..1, duration — секунды.</summary>
    public static void Shake(float strength, float duration)
    {
        if (instance == null || duration <= 0f)
            return;

        instance.shakeStrength = Mathf.Max(instance.shakeStrength, Mathf.Clamp01(strength));
        instance.shakeDuration = Mathf.Max(instance.shakeDuration, duration);
        instance.shakeTime = Mathf.Max(instance.shakeTime, duration);
    }

    private void LateUpdate()
    {
        PlayerCombatState hamsterState = hamster != null ? hamster.GetComponent<PlayerCombatState>() : null;
        PlayerCombatState girlState = hamsterGirl != null ? hamsterGirl.GetComponent<PlayerCombatState>() : null;

        bool hasHamster = hamster != null && hamster.gameObject.activeInHierarchy
            && (hamsterState == null || !hamsterState.IsDead);
        bool hasGirl = hamsterGirl != null && hamsterGirl.gameObject.activeInHierarchy
            && (girlState == null || !girlState.IsDead);

        if (!hasHamster && !hasGirl)
            return;

        Vector3 midpoint;
        float distanceX = 0f;
        float distanceY = 0f;

        if (hasHamster && hasGirl)
        {
            midpoint = (hamster.position + hamsterGirl.position) * 0.5f;
            distanceX = Mathf.Abs(hamster.position.x - hamsterGirl.position.x);
            distanceY = Mathf.Abs(hamster.position.y - hamsterGirl.position.y);
        }
        else
        {
            midpoint = hasHamster ? hamster.position : hamsterGirl.position;
        }

        Vector3 targetPosition = new Vector3(
            midpoint.x,
            midpoint.y,
            transform.position.z
        );

        smoothedPosition = Vector3.SmoothDamp(
            smoothedPosition,
            targetPosition,
            ref moveVelocity,
            moveSmoothTime
        );

        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, boundsMin.x, boundsMax.x);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, boundsMin.y, boundsMax.y);
        }

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

        Vector3 offset = Vector3.zero;

        if (shakeTime > 0f)
        {
            // Тряска живёт на неигровом времени, поэтому видна и во время hitstop.
            shakeTime -= Time.unscaledDeltaTime;
            float progress = shakeDuration > 0f ? Mathf.Clamp01(shakeTime / shakeDuration) : 0f;
            float amplitude = maxShakeOffset * shakeStrength * progress;

            offset = new Vector3(
                Random.Range(-amplitude, amplitude),
                Random.Range(-amplitude, amplitude),
                0f
            );

            if (shakeTime <= 0f)
                shakeStrength = 0f;
        }

        transform.position = smoothedPosition + offset;
    }
}

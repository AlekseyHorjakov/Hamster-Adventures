using UnityEngine;

/// <summary>Показывает зональный фон только рядом с нужным участком камеры.</summary>
[DefaultExecutionOrder(110)]
public class ZoneFadeLayer : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private float startX;
    [SerializeField] private float endX;
    [SerializeField] private float fadeDistance = 1f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(Transform targetCamera, float zoneStartX, float zoneEndX,
        float fade, Color tint)
    {
        cameraTransform = targetCamera;
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = tint;
        startX = zoneStartX;
        endX = zoneEndX;
        fadeDistance = Mathf.Max(0.01f, fade);
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || spriteRenderer == null)
            return;

        float x = cameraTransform.position.x;
        float alpha = 1f;

        if (x < startX)
            alpha = 1f - (startX - x) / fadeDistance;
        else if (x > endX)
            alpha = 1f - (x - endX) / fadeDistance;

        alpha = Mathf.Clamp01(alpha);
        spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b,
            baseColor.a * alpha);
        spriteRenderer.enabled = alpha > 0.01f;
    }
}

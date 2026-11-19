using UnityEngine;

/// <summary>
/// Держит BoxCollider2D ровно по визуалу платформы. Повесь на корень камня/плиты:
/// двигаешь спрайт (даже дочерний) — коллайдер сам подтягивается по верху и ширине.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PlatformColliderSync : MonoBehaviour
{
    [Tooltip("Доля высоты спрайта от верха до линии стойки (трава).")]
    [SerializeField] private float topFraction = 0.05f;
    [Tooltip("Толщина коллайдера в юнитах.")]
    [SerializeField] private float thickness = 0.5f;
    [Tooltip("Ширина коллайдера относительно ширины спрайта.")]
    [Range(0.2f, 1f)]
    [SerializeField] private float widthFactor = 0.96f;
    [SerializeField] private bool oneWay;

    private BoxCollider2D box;

    private void OnEnable()
    {
        box = GetComponent<BoxCollider2D>();
        Apply();
    }

    private void OnValidate()
    {
        box = GetComponent<BoxCollider2D>();
        Apply();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            Apply();
    }

    private void Apply()
    {
        if (box == null)
            return;

        SpriteRenderer visual = GetComponentInChildren<SpriteRenderer>(true);
        if (visual == null || visual.sprite == null)
            return;

        Bounds b = visual.bounds;
        if (b.size.x < 0.01f)
            return;

        float flatY = b.max.y - topFraction * b.size.y;
        Vector3 scale = transform.lossyScale;
        float sx = Mathf.Max(0.01f, scale.x);
        float sy = Mathf.Max(0.01f, scale.y);

        box.size = new Vector2(b.size.x * widthFactor / sx, thickness / sy);
        box.offset = new Vector2((b.center.x - transform.position.x) / sx,
                                 (flatY - transform.position.y) / sy - thickness / sy * 0.5f);

        box.usedByEffector = oneWay;
        if (oneWay)
        {
            PlatformEffector2D effector = GetComponent<PlatformEffector2D>();
            if (effector == null)
                effector = gameObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            effector.surfaceArc = 170f;
        }
    }
}

using UnityEngine;

/// <summary>
/// Костёр-чекпоинт. Сохраняет текущие позиции обоих героев и включает огонь.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class RoomCheckpoint : MonoBehaviour
{
    [SerializeField] private LevelFlowController level;
    [SerializeField] private RoomDefinition room;
    [SerializeField] private int checkpointIndex;
    [SerializeField] private string checkpointLabel = "Чекпоинт";
    [SerializeField] private SpriteRenderer flameRenderer;
    [SerializeField] private Sprite[] flameFrames;
    [SerializeField] private float frameRate = 10f;

    private bool saved;
    private bool lit;
    private float frameTimer;
    private int frameIndex;

    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
            box.isTrigger = true;
    }

    private void Start()
    {
        SetLit(false);
    }

    private void Update()
    {
        if (!lit || flameRenderer == null || flameFrames == null || flameFrames.Length == 0)
            return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, frameRate);
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % flameFrames.Length;
            flameRenderer.sprite = flameFrames[frameIndex];
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (saved || other == null || other.GetComponentInParent<HamsterMovement>() == null)
            return;

        if (level == null)
            level = FindFirstObjectByType<LevelFlowController>();
        if (level == null)
            return;

        saved = true;
        SetLit(true);
        level.SaveCheckpointFromPlayers(checkpointIndex, room, checkpointLabel);
        level.ShowMessage(checkpointLabel + " активирован");
    }

    private void SetLit(bool value)
    {
        lit = value;

        if (flameRenderer != null)
            flameRenderer.enabled = value;

        if (!value)
        {
            frameTimer = 0f;
            frameIndex = 0;
            if (flameRenderer != null && flameFrames != null && flameFrames.Length > 0)
                flameRenderer.sprite = flameFrames[0];
        }
    }
}

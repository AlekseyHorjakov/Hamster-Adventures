using UnityEngine;

/// <summary>
/// Горящий след после рывка: 6 кадров возгорания и 6 кадров затухания.
/// Префаб живёт на земле, растягивается по длине рывка и уничтожается
/// самим AttackController по trailLifetime.
/// </summary>
[DisallowMultipleComponent]
public class FireTrail : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameSeconds = 0.1f;

    private float timer;
    private int index;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
            return;

        if (index >= frames.Length - 1)
            return;

        timer += Time.deltaTime;
        int next = Mathf.FloorToInt(timer / Mathf.Max(0.01f, frameSeconds));

        if (next >= frames.Length)
            next = frames.Length - 1;

        if (next != index)
        {
            index = next;
            spriteRenderer.sprite = frames[index];
        }
    }
}

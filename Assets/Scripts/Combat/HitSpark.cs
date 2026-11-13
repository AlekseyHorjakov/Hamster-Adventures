using UnityEngine;

/// <summary>
/// Вспышка в точке попадания. Проигрывает кадры вручную и сама себя удаляет,
/// поэтому Animator ей не нужен.
/// </summary>
public class HitSpark : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameTime = 0.035f;

    private SpriteRenderer sprite;
    private int index;
    private float timer;
    private float playbackScale = 1f;

    public void Configure(Sprite[] sprites, float time)
    {
        frames = sprites;
        frameTime = time;
    }

    /// <summary>
    /// Настраивает цвет и скорость проигрывания под вес удара.
    /// Масштаб задаёт вызывающая система через Transform.
    /// </summary>
    public void Play(float intensity, Color tint)
    {
        playbackScale = Mathf.Lerp(1.25f, 0.75f, Mathf.Clamp01(intensity));
        if (sprite != null)
            sprite.color = tint;
    }

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        timer += Time.unscaledDeltaTime;

        while (timer >= frameTime * playbackScale && index < frames.Length - 1)
        {
            timer -= frameTime * playbackScale;
            index++;
            sprite.sprite = frames[index];
        }

        if (index >= frames.Length - 1 && timer >= frameTime * playbackScale)
            Destroy(gameObject);
    }
}

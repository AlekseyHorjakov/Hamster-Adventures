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

    public void Configure(Sprite[] sprites, float time)
    {
        frames = sprites;
        frameTime = time;
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

        while (timer >= frameTime && index < frames.Length - 1)
        {
            timer -= frameTime;
            index++;
            sprite.sprite = frames[index];
        }

        if (index >= frames.Length - 1 && timer >= frameTime)
            Destroy(gameObject);
    }
}

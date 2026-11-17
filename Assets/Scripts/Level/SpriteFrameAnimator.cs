using UnityEngine;

/// <summary>Простая покадровая анимация SpriteRenderer без Animator Controller.</summary>
public sealed class SpriteFrameAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 10f;
    [SerializeField] private bool loop = true;

    private float timer;
    private int index;

    private void Reset()
    {
        target = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (target == null || frames == null || frames.Length < 2)
            return;

        timer += Time.deltaTime;
        float step = 1f / Mathf.Max(1f, framesPerSecond);
        while (timer >= step)
        {
            timer -= step;
            index++;

            if (index >= frames.Length)
                index = loop ? 0 : frames.Length - 1;

            target.sprite = frames[index];
        }
    }
}

using UnityEngine;

/// <summary>
/// Короткая заморозка игры на попадании. Именно она делает удар «тяжёлым»:
/// физика и анимации останавливаются на 40–80 мс.
/// </summary>
public class HitStop : MonoBehaviour
{
    private static HitStop instance;

    private float remaining;

    public static void Play(float duration)
    {
        if (duration <= 0f)
            return;

        if (instance == null)
        {
            var go = new GameObject("HitStop");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<HitStop>();
        }

        instance.remaining = Mathf.Max(instance.remaining, duration);
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (remaining <= 0f)
            return;

        remaining -= Time.unscaledDeltaTime;

        if (remaining <= 0f)
        {
            remaining = 0f;
            Time.timeScale = 1f;
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}

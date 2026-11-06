using UnityEngine;

/// <summary>
/// Короткая заморозка игры на попадании. Именно она делает удар «тяжёлым»:
/// физика и анимации останавливаются на 40–80 мс.
/// </summary>
public class HitStop : MonoBehaviour
{
    private static HitStop instance;

    private float remaining;

    /// <summary>
    /// В проекте отключена перезагрузка домена при входе в Play Mode,
    /// поэтому Time.timeScale может пережить выход из игры и остаться нулём.
    /// Сбрасываем его принудительно на каждом запуске.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetTimeOnStart()
    {
        Time.timeScale = 1f;
        instance = null;
    }

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

    private void OnDisable()
    {
        // Если игру остановили во время заморозки — вернуть время,
        // иначе следующий запуск начнётся в замершем состоянии.
        remaining = 0f;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        Time.timeScale = 1f;
    }
}

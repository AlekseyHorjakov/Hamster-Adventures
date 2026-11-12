using System;
using UnityEngine;

/// <summary>
/// Единое состояние паузы. Отдельный флаг нужен, чтобы HitStop и меню
/// не включали друг другу игровое время.
/// </summary>
public static class GamePause
{
    public static bool IsPaused { get; private set; }
    public static event Action<bool> Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsPaused = false;
        Changed = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetTime()
    {
        Time.timeScale = 1f;
    }

    public static void SetPaused(bool paused)
    {
        if (IsPaused == paused)
            return;

        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        Changed?.Invoke(paused);
    }
}

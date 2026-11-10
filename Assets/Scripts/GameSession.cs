using System;
using UnityEngine;

/// <summary>Выбранный режим игры, общий для всех сцен в текущем запуске.</summary>
public enum GameMode
{
    SoloHamster,
    SoloHamsterGirl,
    Coop
}

public static class GameSession
{
    public static GameMode Mode { get; private set; }
    public static bool HasSelection { get; private set; }
    public static event Action<GameMode> ModeChanged;

    public static bool IsHamsterActive => Mode != GameMode.SoloHamsterGirl;
    public static bool IsGirlActive => Mode != GameMode.SoloHamster;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForNewPlaySession()
    {
        HasSelection = false;
    }

    public static void SetMode(GameMode mode)
    {
        Mode = mode;
        HasSelection = true;
        ModeChanged?.Invoke(mode);
    }
}

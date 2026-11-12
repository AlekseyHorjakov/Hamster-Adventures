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

    /// <summary>Сбрасывает выбранный режим перед возвратом в меню.</summary>
    public static void Clear()
    {
        HasSelection = false;
    }

    public static void SetMode(GameMode mode)
    {
        Mode = mode;
        HasSelection = true;
        ModeChanged?.Invoke(mode);
    }

    /// <summary>Применяет выбранный режим к героям текущей сцены.</summary>
    public static void ApplyModeToPlayers()
    {
        HamsterMovement[] players = UnityEngine.Object.FindObjectsByType<HamsterMovement>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        HamsterMovement hamster = null;
        HamsterMovement girl = null;

        foreach (HamsterMovement player in players)
        {
            if (player == null)
                continue;

            if (player.name.Contains("HamsterGirl"))
                girl = player;
            else if (player.name.Contains("Hamster"))
                hamster = player;
        }

        bool hamsterActive = Mode != GameMode.SoloHamsterGirl;
        bool girlActive = Mode != GameMode.SoloHamster;

        if (hamster != null)
        {
            hamster.SetPartner(Mode == GameMode.Coop && girl != null ? girl.transform : null);
            hamster.gameObject.SetActive(hamsterActive);
        }

        if (girl != null)
        {
            girl.SetPartner(Mode == GameMode.Coop && hamster != null ? hamster.transform : null);
            girl.gameObject.SetActive(girlActive);
        }
    }
}

using System;
using UnityEngine;

[Serializable]
public class CheckpointData
{
    public bool valid;
    public int mode;
    public int checkpoint;
    public bool completed;
    public bool hasHamster;
    public float hamsterX;
    public float hamsterY;
    public bool hasGirl;
    public float girlX;
    public float girlY;
}

/// <summary>Последняя контрольная точка и режим текущего прохождения.</summary>
public static class GameProgress
{
    private const string SaveKey = "hamster.progress.v1";

    private static CheckpointData current;
    private static bool resumeRequested;
    private static bool newGamePending;

    public static bool HasCheckpoint { get; private set; }
    public static bool CanContinue => HasCheckpoint && current != null && !current.completed;
    public static bool IsCompleted => current != null && current.completed;
    public static GameMode Mode => current != null ? (GameMode)current.mode : GameMode.SoloHamster;
    public static int Checkpoint => current != null ? current.checkpoint : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlaySession()
    {
        resumeRequested = false;
        newGamePending = false;
        Load();
    }

    public static void StartNew(GameMode mode)
    {
        current = new CheckpointData
        {
            valid = true,
            mode = (int)mode,
            checkpoint = 0,
            completed = false
        };
        HasCheckpoint = true;
        newGamePending = true;
        resumeRequested = false;
        Save();
    }

    public static bool TryBeginContinue(out GameMode mode)
    {
        Load();
        if (!HasCheckpoint || current == null || current.completed)
        {
            mode = default;
            return false;
        }

        resumeRequested = true;
        newGamePending = false;
        mode = (GameMode)current.mode;
        return true;
    }

    public static void MarkLevelCompleted()
    {
        if (current == null)
            current = new CheckpointData();

        current.valid = true;
        current.completed = true;
        current.checkpoint = 4;
        HasCheckpoint = true;
        resumeRequested = false;
        newGamePending = false;
        Save();
    }
    public static bool ConsumeResume(out CheckpointData data)
    {
        if (!resumeRequested || current == null)
        {
            data = null;
            return false;
        }

        resumeRequested = false;
        data = current;
        return true;
    }

    public static bool ConsumeNewGame()
    {
        if (!newGamePending)
            return false;

        newGamePending = false;
        return true;
    }

    public static void SaveCheckpoint(int checkpoint, Vector2? hamsterPosition, Vector2? girlPosition)
    {
        if (current == null)
            current = new CheckpointData();

        current.valid = true;
        current.mode = (int)GameSession.Mode;
        current.checkpoint = checkpoint;
        current.hasHamster = hamsterPosition.HasValue;
        current.hasGirl = girlPosition.HasValue;

        if (hamsterPosition.HasValue)
        {
            current.hamsterX = hamsterPosition.Value.x;
            current.hamsterY = hamsterPosition.Value.y;
        }

        if (girlPosition.HasValue)
        {
            current.girlX = girlPosition.Value.x;
            current.girlY = girlPosition.Value.y;
        }

        HasCheckpoint = true;
        Save();
    }

    private static void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            current = null;
            HasCheckpoint = false;
            return;
        }

        try
        {
            current = JsonUtility.FromJson<CheckpointData>(json);
        }
        catch (Exception)
        {
            current = null;
        }

        HasCheckpoint = current != null && current.valid;
        if (!HasCheckpoint)
            current = null;
    }

    private static void Save()
    {
        if (current == null)
            return;

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(current));
        PlayerPrefs.Save();
    }
}

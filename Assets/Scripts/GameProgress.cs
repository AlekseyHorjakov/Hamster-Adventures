using System;
using UnityEngine;

[Serializable]
public class CheckpointData
{
    public bool valid;
    public int version = 2;
    public int mode;
    public int checkpoint;
    public int roomId = -1;
    public int zoneId;
    public string roomName = "";
    public string checkpointName = "";
    public bool completed;
    public bool hasHamster;
    public float hamsterX;
    public float hamsterY;
    public bool hasGirl;
    public float girlX;
    public float girlY;
}

/// <summary>
/// Последняя контрольная точка и режим текущего прохождения.
/// Формат v2 хранит комнату и зону, но по-прежнему читает старые сохранения v1.
/// </summary>
public static class GameProgress
{
    private const string SaveKeyV2 = "hamster.progress.v2";
    private const string SaveKeyV1 = "hamster.progress.v1";

    private static CheckpointData current;
    private static bool resumeRequested;
    private static bool newGamePending;

    public static bool HasCheckpoint { get; private set; }
    public static bool CanContinue => HasCheckpoint && current != null && !current.completed;
    public static bool IsCompleted => current != null && current.completed;
    public static GameMode Mode => current != null ? (GameMode)current.mode : GameMode.SoloHamster;
    public static int Checkpoint => current != null ? current.checkpoint : 0;
    public static int RoomId => current != null ? current.roomId : 0;
    public static int ZoneId => current != null ? current.zoneId : 0;
    public static string RoomName => current != null ? current.roomName : string.Empty;
    public static string CheckpointName => current != null ? current.checkpointName : string.Empty;

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
            version = 2,
            mode = (int)mode,
            checkpoint = 0,
            roomId = 1,
            zoneId = 1,
            roomName = "Лагерь",
            checkpointName = "Старт",
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
        current.version = 2;
        current.completed = true;
        current.checkpoint = 5;
        current.roomId = 18;
        current.zoneId = 4;
        current.roomName = "Выход";
        current.checkpointName = "Уровень пройден";
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

    /// <summary>Совместимость со старым вертикальным срезом.</summary>
    public static void SaveCheckpoint(int checkpoint, Vector2? hamsterPosition, Vector2? girlPosition)
    {
        SaveCheckpointInternal(checkpoint, current != null ? current.roomId : -1,
            current != null ? current.zoneId : 0,
            current != null ? current.roomName : string.Empty,
            current != null ? current.checkpointName : string.Empty,
            hamsterPosition, girlPosition);
    }

    /// <summary>Сохраняет чекпоинт с привязкой к комнате полного уровня.</summary>
    public static void SaveRoomCheckpoint(int checkpoint, int roomId, int zoneId,
        string roomName, string checkpointName, Vector2? hamsterPosition, Vector2? girlPosition)
    {
        SaveCheckpointInternal(checkpoint, roomId, zoneId, roomName, checkpointName,
            hamsterPosition, girlPosition);
    }

    private static void SaveCheckpointInternal(int checkpoint, int roomId, int zoneId,
        string roomName, string checkpointName, Vector2? hamsterPosition, Vector2? girlPosition)
    {
        if (current == null)
            current = new CheckpointData();

        current.valid = true;
        current.version = 2;
        current.mode = (int)GameSession.Mode;
        current.checkpoint = checkpoint;
        current.roomId = roomId;
        current.zoneId = zoneId;
        current.roomName = roomName ?? string.Empty;
        current.checkpointName = checkpointName ?? string.Empty;
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
        current = null;

        string jsonV2 = PlayerPrefs.GetString(SaveKeyV2, string.Empty);
        if (!string.IsNullOrEmpty(jsonV2))
        {
            current = Deserialize(jsonV2);
        }

        if (current == null)
        {
            string jsonV1 = PlayerPrefs.GetString(SaveKeyV1, string.Empty);
            if (!string.IsNullOrEmpty(jsonV1))
            {
                current = Deserialize(jsonV1);
                if (current != null)
                {
                    current.version = 2;
                    if (current.roomId <= 0)
                        current.roomId = 1;
                    if (current.zoneId <= 0)
                        current.zoneId = 1;
                    if (string.IsNullOrEmpty(current.roomName))
                        current.roomName = "Лагерь";
                    Save();
                }
            }
        }

        HasCheckpoint = current != null && current.valid;
        if (!HasCheckpoint)
            current = null;
    }

    private static CheckpointData Deserialize(string json)
    {
        try
        {
            CheckpointData data = JsonUtility.FromJson<CheckpointData>(json);
            return data != null && data.valid ? data : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void Save()
    {
        if (current == null)
            return;

        current.version = 2;
        PlayerPrefs.SetString(SaveKeyV2, JsonUtility.ToJson(current));
        PlayerPrefs.Save();
    }
}

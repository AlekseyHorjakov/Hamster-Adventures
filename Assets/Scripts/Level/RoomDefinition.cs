using UnityEngine;

/// <summary>
/// Данные комнаты полного уровня: границы камеры, зона и подсказка цели.
/// Не хранит состояние боя — этим занимается EncounterController.
/// </summary>
[CreateAssetMenu(menuName = "Hamster/Level/Room Definition", fileName = "RoomDefinition")]
public sealed class RoomDefinition : ScriptableObject
{
    [SerializeField] private int roomId;
    [SerializeField] private string displayName = "Комната";
    [SerializeField] private int zoneId = 1;
    [SerializeField] private string zoneName = "Лагерь и лес";
    [SerializeField, TextArea] private string objective = "";
    [SerializeField] private Vector2 cameraBoundsMin = new Vector2(-100f, -20f);
    [SerializeField] private Vector2 cameraBoundsMax = new Vector2(100f, 40f);
    [SerializeField] private float surfaceY;
    [SerializeField] private float worldMinX = -100f;
    [SerializeField] private float worldMaxX = 100f;

    public int RoomId => roomId;
    public string DisplayName => displayName;
    public int ZoneId => zoneId;
    public string ZoneName => zoneName;
    public string Objective => objective;
    public Vector2 CameraBoundsMin => cameraBoundsMin;
    public Vector2 CameraBoundsMax => cameraBoundsMax;
    public float SurfaceY => surfaceY;
    public float WorldMinX => worldMinX;
    public float WorldMaxX => worldMaxX;
}



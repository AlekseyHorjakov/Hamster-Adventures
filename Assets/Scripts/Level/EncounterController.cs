using UnityEngine;

/// <summary>
/// Управляет одной боевой встречей: активирует заранее расставленных врагов,
/// поднимает ворота и открывает проход после зачистки.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class EncounterController : MonoBehaviour
{
    [SerializeField] private LevelFlowController level;
    [SerializeField] private RoomDefinition room;
    [SerializeField] private GameObject[] enemies;
    [SerializeField] private GameObject[] gates;
    [SerializeField] private string encounterName = "Бой";
    [SerializeField] private bool activateOnStart;

    private bool begun;
    private bool cleared;

    public bool IsCleared => cleared;

    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
            box.isTrigger = true;
    }

    private void Start()
    {
        SetGates(false);
        if (activateOnStart)
            BeginEncounter();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || other.GetComponentInParent<HamsterMovement>() == null)
            return;

        BeginEncounter();
    }

    private void Update()
    {
        if (!begun || cleared)
            return;

        if (AllEnemiesDefeated())
            ClearEncounter();
    }

    public void BeginEncounter()
    {
        if (begun || cleared)
            return;

        begun = true;

        if (enemies != null)
        {
            foreach (GameObject enemy in enemies)
            {
                if (enemy != null)
                    enemy.SetActive(true);
            }
        }

        SetGates(true);

        if (level != null && room != null)
            level.EnterRoom(room);

        if (level != null)
            level.ShowMessage(encounterName + " — победи всех противников!");
    }

    private void ClearEncounter()
    {
        cleared = true;
        SetGates(false);

        if (level != null)
            level.NotifyEncounterCleared(room, encounterName);
    }

    private bool AllEnemiesDefeated()
    {
        if (enemies == null || enemies.Length == 0)
            return true;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy)
                continue;

            Health health = enemy.GetComponent<Health>();
            if (health != null && health.IsAlive)
                return false;
        }

        return true;
    }

    private void SetGates(bool open)
    {
        if (gates == null)
            return;

        foreach (GameObject gate in gates)
        {
            if (gate != null)
                gate.SetActive(open);
        }
    }
}

using UnityEngine;

/// <summary>Финальный триггер контрольной точки зоны I.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class ZoneEndTrigger : MonoBehaviour
{
    [SerializeField] private LevelFlowController level;
    [SerializeField] private int completedRooms = 5;

    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
            box.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || other.GetComponentInParent<HamsterMovement>() == null)
            return;

        if (level != null)
            level.NotifyZoneCheckpoint(completedRooms);
    }
}

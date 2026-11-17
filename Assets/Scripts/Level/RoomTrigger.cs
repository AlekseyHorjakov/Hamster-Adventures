using UnityEngine;

/// <summary>Мягкий триггер входа в комнату: обновляет название и границы камеры.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class RoomTrigger : MonoBehaviour
{
    [SerializeField] private LevelFlowController level;
    [SerializeField] private RoomDefinition room;

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

        if (level != null && room != null)
            level.EnterRoom(room);
    }
}

using UnityEngine;

public enum LevelTriggerAction
{
    StartArena,
    Finish
}

/// <summary>Простой триггер этапа: запускает арену или завершает уровень.</summary>
[RequireComponent(typeof(Collider2D))]
public class LevelTrigger : MonoBehaviour
{
    [SerializeField] private LevelFlowController level;
    [SerializeField] private LevelTriggerAction action;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || other.GetComponentInParent<HamsterMovement>() == null)
            return;

        if (level == null)
            return;

        if (action == LevelTriggerAction.StartArena)
            level.NotifyArenaEntered();
        else
            level.NotifyFinishEntered();
    }
}

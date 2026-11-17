using System.Collections;
using UnityEngine;

/// <summary>
/// Безопасное возвращение упавшего героя к последнему чекпоинту.
/// Падение не считается окончательным поражением, пока идёт respawn.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class FallRespawnZone : MonoBehaviour
{
    [SerializeField] private LevelFlowController level;
    [SerializeField] private float respawnDelay = 0.65f;

    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
            box.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
            return;

        PlayerCombatState state = other.GetComponentInParent<PlayerCombatState>();
        HamsterMovement movement = other.GetComponentInParent<HamsterMovement>();
        if (state == null || movement == null || state.IsDead || state.IsRespawning)
            return;

        if (level == null)
            level = FindFirstObjectByType<LevelFlowController>();
        if (level == null)
            return;

        state.BeginRespawn();
        StartCoroutine(RespawnRoutine(state));
    }

    private IEnumerator RespawnRoutine(PlayerCombatState state)
    {
        yield return new WaitForSecondsRealtime(respawnDelay);

        if (state == null)
            yield break;

        level.RespawnPlayer(state);
        level.ShowMessage("Возврат к последнему чекпоинту");
    }
}


using UnityEngine;

public class FallZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем, что в зону упал именно персонаж с управлением.
        HamsterMovement hamster =
            other.GetComponentInParent<HamsterMovement>();

        if (hamster == null)
            return;

        Debug.Log("Хомяк упал за пределы уровня!");
    }
}

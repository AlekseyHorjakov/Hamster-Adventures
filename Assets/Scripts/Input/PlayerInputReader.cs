using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Читает ввод игрока из InputActionAsset. На префабе выбирается карта:
/// "Hamster" или "HamsterGirl". Пока asset не назначен, HamsterMovement
/// использует старые клавиши как fallback.
/// </summary>
[DisallowMultipleComponent]
public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionAsset actions;
    [SerializeField] private string mapName = "Hamster";

    private InputAction move;
    private InputAction jump;
    private InputAction attack;
    private InputAction interact;
    private InputAction any;
    private InputActionMap map;

    public float Move => move != null ? move.ReadValue<float>() : 0f;
    public bool JumpPressed => jump != null && jump.WasPressedThisFrame();
    public bool AttackPressed => attack != null && attack.WasPressedThisFrame();
    public bool AttackHeld => attack != null && attack.IsPressed();
    public bool AttackReleased => attack != null && attack.WasReleasedThisFrame();
    public bool AnyPressed => any != null && any.WasPressedThisFrame();
    public bool InteractHeld => interact != null && interact.IsPressed();

    private void Awake()
    {
        if (actions == null)
        {
            Debug.LogWarning("[PlayerInputReader] Не назначен InputActionAsset у " + name, this);
            return;
        }

        map = actions.FindActionMap(mapName, false);
        if (map == null)
        {
            Debug.LogWarning("[PlayerInputReader] Нет карты " + mapName + " у " + name, this);
            return;
        }

        move = map.FindAction("Move", false);
        jump = map.FindAction("Jump", false);
        attack = map.FindAction("Attack", false);
        interact = map.FindAction("Interact", false);
        any = map.FindAction("Any", false);
        map.Enable();
    }

    private void OnEnable()
    {
        map?.Enable();
    }

    private void OnDisable()
    {
        map?.Disable();
    }
}

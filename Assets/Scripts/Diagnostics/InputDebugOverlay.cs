using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Временный диагностический оверлей: показывает состояние ввода, времени
/// и управления. F1 — скрыть/показать. Удаляется после отладки.
/// </summary>
public class InputDebugOverlay : MonoBehaviour
{
    private static bool visible = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("InputDebugOverlay");
        DontDestroyOnLoad(go);
        go.AddComponent<InputDebugOverlay>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[Key.F1].wasPressedThisFrame)
            visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible)
            return;

        var sb = new StringBuilder();
        var keyboard = Keyboard.current;

        sb.AppendLine("timeScale: " + Time.timeScale.ToString("0.00") +
                      "   scene: " + SceneManager.GetActiveScene().name);
        sb.AppendLine("keyboard: " + (keyboard == null ? "НЕТ УСТРОЙСТВА" : "есть"));

        if (keyboard != null)
        {
            sb.AppendLine("A=" + keyboard[Key.A].isPressed + " D=" + keyboard[Key.D].isPressed +
                          " W=" + keyboard[Key.W].isPressed + " F=" + keyboard[Key.F].isPressed);
            sb.AppendLine("H=" + keyboard[Key.H].isPressed + " K=" + keyboard[Key.K].isPressed +
                          " U=" + keyboard[Key.U].isPressed + " L=" + keyboard[Key.L].isPressed);
        }

        foreach (var body in Object.FindObjectsByType<HamsterMovement>(FindObjectsSortMode.None))
        {
            sb.AppendLine(body.name + ": input=" + body.MoveInput.ToString("0.0") +
                          " locked=" + body.ControlLocked +
                          " grounded=" + body.Grounded +
                          " vx=" + (body.Body != null ? body.Body.linearVelocity.x.ToString("0.0") : "?"));
        }

        var style = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.UpperLeft };
        GUI.Box(new Rect(8, 8, 470, 170), "");
        GUI.Label(new Rect(18, 14, 455, 160), sb.ToString(), style);
    }
}

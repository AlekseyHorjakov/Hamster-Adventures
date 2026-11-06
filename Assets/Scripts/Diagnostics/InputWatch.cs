using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>
/// Диагностический оверлей: показывает, доходит ли ввод до игры и что
/// происходит с управлением. F1 — скрыть/показать.
/// </summary>
public class InputWatch : MonoBehaviour
{
    private static bool visible = true;
    private static int inputEvents;
    private static string lastKey = "нет";
    private static float lastKeyTime = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("InputWatch");
        DontDestroyOnLoad(go);
        go.AddComponent<InputWatch>();
    }

    private void OnEnable()
    {
        InputSystem.onEvent += OnAnyInput;
    }

    private void OnDisable()
    {
        InputSystem.onEvent -= OnAnyInput;
    }

    private static void OnAnyInput(InputEventPtr eventPtr, InputDevice device)
    {
        inputEvents++;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        foreach (var key in keyboard.allKeys)
        {
            if (!key.wasPressedThisFrame)
                continue;

            lastKey = key.displayName;
            lastKeyTime = Time.unscaledTime;
            break;
        }

        if (keyboard[Key.F1].wasPressedThisFrame)
            visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible)
            return;

        var sb = new StringBuilder();
        var keyboard = Keyboard.current;
        bool noInput = inputEvents == 0;

        sb.AppendLine("timeScale: " + Time.timeScale.ToString("0.00") +
                      "   scene: " + SceneManager.GetActiveScene().name);
        sb.AppendLine("событий ввода: " + inputEvents + (noInput ? "   <<< ВВОД НЕ ПОСТУПАЕТ" : ""));
        sb.AppendLine("клавиатура: " + (keyboard == null ? "НЕТ" : "есть"));

        if (keyboard != null)
            sb.AppendLine("A=" + keyboard[Key.A].isPressed + " D=" + keyboard[Key.D].isPressed +
                          " W=" + keyboard[Key.W].isPressed + " F=" + keyboard[Key.F].isPressed);

        string when = lastKeyTime < 0f ? "" : " (" + (Time.unscaledTime - lastKeyTime).ToString("0.0") + " с назад)";
        sb.AppendLine("последняя клавиша: " + lastKey + when);

        foreach (var body in Object.FindObjectsByType<HamsterMovement>(FindObjectsSortMode.None))
        {
            sb.AppendLine(body.name + ": input=" + body.MoveInput.ToString("0.0") +
                          " locked=" + body.ControlLocked +
                          " grounded=" + body.Grounded +
                          " vx=" + (body.Body != null ? body.Body.linearVelocity.x.ToString("0.0") : "?"));
        }

        var style = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.UpperLeft };
        GUI.Box(new Rect(8, 8, 560, 185), "");
        GUI.Label(new Rect(18, 14, 545, 175), sb.ToString(), style);
    }
}

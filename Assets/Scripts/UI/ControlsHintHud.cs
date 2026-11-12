using UnityEngine;
using UnityEngine.UI;

/// <summary>Короткие подсказки управления под HUD.</summary>
[DisallowMultipleComponent]
public class ControlsHintHud : MonoBehaviour
{
    private GameObject panel;
    private Text label;
    private LevelFlowController levelFlow;
    private GameMode lastMode;
    private bool hasLastMode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Object.FindFirstObjectByType<ControlsHintHud>() != null)
            return;

        new GameObject("ControlsHintHUD").AddComponent<ControlsHintHud>();
    }

    private void Awake()
    {
        Build();
    }

    private void Update()
    {
        levelFlow = levelFlow != null
            ? levelFlow
            : FindFirstObjectByType<LevelFlowController>();

        bool visible = GameSession.HasSelection
            && !GamePause.IsPaused
            && (levelFlow == null || !levelFlow.IsEndScreen);

        if (panel != null && panel.activeSelf != visible)
            panel.SetActive(visible);

        if (visible && (!hasLastMode || lastMode != GameSession.Mode))
            RefreshText();
    }

    private void Build()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        panel = new GameObject("ControlsPanel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(canvasGo.transform, false);
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -240f);
        panelRect.sizeDelta = new Vector2(1200f, 94f);

        Image background = panel.GetComponent<Image>();
        background.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f), 1f);
        background.color = new Color(0.02f, 0.035f, 0.065f, 0.78f);
        background.raycastTarget = false;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.SetParent(panel.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 9f);
        textRect.offsetMax = new Vector2(-14f, -9f);

        label = textGo.GetComponent<Text>();
        label.font = LoadFont();
        label.fontSize = 17;
        label.alignment = TextAnchor.UpperLeft;
        label.color = new Color(0.9f, 0.95f, 1f, 1f);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false;

        panel.SetActive(false);
    }

    private void RefreshText()
    {
        lastMode = GameSession.Mode;
        hasLastMode = true;

        string hint = "";
        if (GameSession.IsHamsterActive)
            hint += "Хомяк: A/D — идти · W — прыжок · F — удар/заряд · Q — супер · E — оживить · R — бросить\n";
        if (GameSession.IsGirlActive)
            hint += "Хомячиха: H/K — идти · U — прыжок · L — удар/заряд · I — супер · O — оживить · P — бросить\n";
        hint += "Esc / Start — пауза";

        if (label != null)
            label.text = hint;
    }

    private static Font LoadFont()
    {
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Tahoma" }, 19);
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font;
    }
}

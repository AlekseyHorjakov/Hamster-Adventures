using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Плашки здоровья героев: золотые рамки, красная заливка здоровья
/// и нижняя полоска под суперудар. Плашки берутся из Resources, поэтому
/// HUD создаётся автоматически в любой сцене, где есть герои.
/// </summary>
[DisallowMultipleComponent]
public class HealthHud : MonoBehaviour
{
    private const float PlateHeight = 200f;
    private const float SideMargin = 24f;
    private const float TopMargin = 20f;

    // Окна в плашках, нормализованные по размеру картинки (origin снизу слева).
    private static readonly Rect HamsterHealthRect = new Rect(0.332258f, 0.346207f, 0.617051f, 0.222069f);
    private static readonly Rect HamsterSuperRect = new Rect(0.312903f, 0.124138f, 0.482488f, 0.034483f);
    private static readonly Rect GirlHealthRect = new Rect(0.335175f, 0.349448f, 0.615561f, 0.219613f);
    private static readonly Rect GirlSuperRect = new Rect(0.315838f, 0.128453f, 0.482965f, 0.038674f);

    private static readonly Color Red = new Color(0.82f, 0.07f, 0.06f, 1f);
    private static readonly Color Orange = new Color(1f, 0.55f, 0.12f, 1f);
    private static readonly Color Backing = new Color(0.16f, 0.06f, 0.04f, 0.95f);

    private Image hamsterHealthFill;
    private Image girlHealthFill;
    private Image hamsterSuperFill;
    private Image girlSuperFill;

    private Health hamsterHealth;
    private Health girlHealth;
    private GameObject hamsterPlate;
    private GameObject girlPlate;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Object.FindFirstObjectByType<HealthHud>() != null)
            return;

        Health[] all = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
        if (!all.Any(h => h.name.Contains("Hamster")))
            return;

        new GameObject("HealthHUD").AddComponent<HealthHud>();
    }

    private void Awake()
    {
        Initialize();
    }

    /// <summary>Строит HUD. Публичный, чтобы проверять его в редакторе.</summary>
    public void Initialize()
    {
        if (transform.childCount > 0)
            return;

        BuildPlates();
        BindHealth();
    }

    private void Update()
    {
        UpdateHealthPlate(hamsterHealth, hamsterHealthFill, hamsterPlate);
        UpdateHealthPlate(girlHealth, girlHealthFill, girlPlate);
    }

    /// <summary>Заполнение нижней полоски суперудара у Хомяка, 0..1.</summary>
    public void SetHamsterSuperCharge(float value)
    {
        if (hamsterSuperFill != null)
            hamsterSuperFill.fillAmount = Mathf.Clamp01(value);
    }

    /// <summary>Заполнение нижней полоски суперудара у Хомячихи, 0..1.</summary>
    public void SetGirlSuperCharge(float value)
    {
        if (girlSuperFill != null)
            girlSuperFill.fillAmount = Mathf.Clamp01(value);
    }

    private void BuildPlates()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        hamsterHealthFill = BuildPlate(canvasGo.transform, "Hamster", false, out hamsterSuperFill);
        girlHealthFill = BuildPlate(canvasGo.transform, "Girl", true, out girlSuperFill);

        hamsterPlate = canvasGo.transform.Find("HamsterPlate")?.gameObject;
        girlPlate = canvasGo.transform.Find("GirlPlate")?.gameObject;
    }

    private static void UpdateHealthPlate(Health health, Image fill, GameObject plate)
    {
        bool active = health != null && health.gameObject.activeInHierarchy;

        if (plate != null && plate.activeSelf != active)
            plate.SetActive(active);

        if (active)
            UpdateFill(health, fill);
    }

    private Image BuildPlate(Transform canvas, string prefix, bool right, out Image superFill)
    {
        superFill = null;

        Sprite plate = LoadSprite("UI/HealthPlates/" + prefix + "_HealthPlate");
        Sprite health = LoadSprite("UI/HealthPlates/" + prefix + "_HealthFill");
        Sprite super = LoadSprite("UI/HealthPlates/" + prefix + "_SuperFill");

        if (plate == null || health == null || super == null)
            return null;

        float width = PlateHeight * (plate.rect.width / plate.rect.height);

        GameObject containerGo = new GameObject(prefix + "Plate", typeof(RectTransform));
        RectTransform container = containerGo.GetComponent<RectTransform>();
        container.SetParent(canvas, false);
        container.anchorMin = container.anchorMax = right ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        container.pivot = right ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        container.anchoredPosition = new Vector2(right ? -SideMargin : SideMargin, -TopMargin);
        container.sizeDelta = new Vector2(width, PlateHeight);

        Rect healthRect = prefix == "Hamster" ? HamsterHealthRect : GirlHealthRect;
        Rect superRect = prefix == "Hamster" ? HamsterSuperRect : GirlSuperRect;

        CreateFill(container, "HealthBack", health, healthRect, Backing, false, 1f);
        Image healthFill = CreateFill(container, "HealthFill", health, healthRect, Red, true, 1f);
        CreateFill(container, "SuperBack", super, superRect, Backing, false, 1f);
        superFill = CreateFill(container, "SuperFill", super, superRect, Orange, true, 0f);

        GameObject plateGo = new GameObject("Plate", typeof(RectTransform), typeof(Image));
        RectTransform plateRect = plateGo.GetComponent<RectTransform>();
        plateRect.SetParent(container, false);
        plateRect.anchorMin = Vector2.zero;
        plateRect.anchorMax = Vector2.one;
        plateRect.offsetMin = Vector2.zero;
        plateRect.offsetMax = Vector2.zero;

        Image plateImage = plateGo.GetComponent<Image>();
        plateImage.sprite = plate;
        plateImage.preserveAspect = true;
        plateImage.raycastTarget = false;

        return healthFill;
    }

    private static Image CreateFill(Transform parent, string name, Sprite sprite, Rect rect,
        Color color, bool filled, float amount)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(rect.xMin, rect.yMin);
        rt.anchorMax = new Vector2(rect.xMax, rect.yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;

        if (filled)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = amount;
        }

        return image;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite imported = Resources.Load<Sprite>(path);
        if (imported != null)
            return imported;

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture != null)
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);

        Debug.LogWarning("[HealthHud] Не нашёл " + path + " в Resources");
        return null;
    }

    private void BindHealth()
    {
        Health[] all = FindObjectsByType<Health>(FindObjectsSortMode.None);
        girlHealth = all.FirstOrDefault(h => h.name.Contains("HamsterGirl"));
        hamsterHealth = all.FirstOrDefault(h => h != girlHealth && h.name.Contains("Hamster"));
    }

    private static void UpdateFill(Health health, Image fill)
    {
        if (fill == null)
            return;

        fill.fillAmount = health != null && health.Max > 0f
            ? Mathf.Clamp01(health.Current / health.Max)
            : 0f;
    }
}

using System.Linq;
using UnityEngine;

/// <summary>
/// Фиолетовые молнии вокруг Хомячихи на всё время суперудара.
/// Используются только кадры из присланного листа: несколько отдельных
/// разрядов по бокам и центральная электрическая клетка.
/// </summary>
[DisallowMultipleComponent]
public class GirlSuperAuraVFXController : MonoBehaviour
{
    [Header("Молнии")]
    [SerializeField] private SpriteRenderer cage;
    [SerializeField] private Sprite[] cageFrames;
    [SerializeField] private SpriteRenderer[] bolts;
    [SerializeField] private Sprite[] boltFrames;

    [Header("Тайминг")]
    [SerializeField] private float duration = 10f;
    [SerializeField] private float cageFrameRate = 11f;
    [SerializeField] private float boltFrameRate = 15f;

    private Transform follow;
    private SpriteRenderer followSprite;
    private float elapsed;
    private float phase;
    private bool active;
    private Vector3[] boltBasePositions;
    private Quaternion[] boltBaseRotations;
    private Vector3[] boltBaseScales;

    private void Awake()
    {
        phase = Random.value * 20f;

        if (bolts != null && bolts.Length > 0)
            CacheBoltBaseTransforms();
    }

    private void CacheBoltBaseTransforms()
    {
        if (bolts == null)
            return;

        boltBasePositions = new Vector3[bolts.Length];
        boltBaseRotations = new Quaternion[bolts.Length];
        boltBaseScales = new Vector3[bolts.Length];
        for (int i = 0; i < bolts.Length; i++)
        {
            if (bolts[i] == null)
                continue;
            boltBasePositions[i] = bolts[i].transform.localPosition;
            boltBaseRotations[i] = bolts[i].transform.localRotation;
            boltBaseScales[i] = bolts[i].transform.localScale;
        }
    }

    public void Play(Transform target, SpriteRenderer targetSprite, float effectDuration)
    {
        follow = target;
        followSprite = targetSprite;
        duration = Mathf.Max(0.1f, effectDuration);
        elapsed = 0f;
        active = true;

        // Старый префаб мог содержать другие слои. Если в нём ещё нет
        // новых ссылок на молнии, собираем их из Resources во время игры.
        if (bolts == null || bolts.Length == 0)
            BuildRuntimeLightning();
    }

    public void StopAndDestroy()
    {
        active = false;
        Destroy(gameObject, 0.2f);
    }

    private void LateUpdate()
    {
        if (!active)
            return;

        elapsed += Time.deltaTime;

        if (follow != null)
        {
            Vector3 center = follow.position + Vector3.up * 1.2f;
            if (followSprite != null)
                center = followSprite.bounds.center;
            transform.position = new Vector3(center.x, center.y, follow.position.z - 0.15f);
        }

        AnimateCage();
        AnimateBolts();

        if (elapsed >= duration)
            StopAndDestroy();
    }

    private void BuildRuntimeLightning()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        Sprite[] boltFramesBuilt = new Sprite[8];
        for (int i = 0; i < boltFramesBuilt.Length; i++)
            boltFramesBuilt[i] = LoadRuntimeSprite("lightning_" + (i + 1).ToString("00"));

        Sprite[] cageFramesBuilt = new Sprite[4];
        for (int i = 0; i < cageFramesBuilt.Length; i++)
            cageFramesBuilt[i] = LoadRuntimeSprite("lightning_" + (12 + i).ToString("00"));

        if (boltFramesBuilt.Any(frame => frame == null) || cageFramesBuilt.Any(frame => frame == null))
        {
            Debug.LogWarning("[GirlSuperAura] Не удалось загрузить кадры молний из Resources.");
            return;
        }

        GameObject cageObject = new GameObject("LightningCage");
        cageObject.transform.SetParent(transform, false);
        cageObject.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        cageObject.transform.localScale = new Vector3(1.55f, 1.75f, 1f);
        cage = cageObject.AddComponent<SpriteRenderer>();
        cage.sprite = cageFramesBuilt[0];
        cage.sortingLayerName = "Characters";
        cage.sortingOrder = -16;
        cageFrames = cageFramesBuilt;

        Vector3[] positions =
        {
            new Vector3(-0.92f, 1.22f, 0f),
            new Vector3(-1.10f, 0.48f, 0f),
            new Vector3(-0.68f, 1.78f, 0f),
            new Vector3(-0.38f, -0.12f, 0f),
            new Vector3(0.92f, 1.22f, 0f),
            new Vector3(1.10f, 0.48f, 0f),
            new Vector3(0.68f, 1.78f, 0f),
            new Vector3(0.38f, -0.12f, 0f)
        };
        float[] angles = { 24f, -28f, 52f, -62f, 156f, 208f, 128f, 242f };
        float[] scales = { 0.58f, 0.48f, 0.46f, 0.42f, 0.58f, 0.48f, 0.46f, 0.42f };

        bolts = new SpriteRenderer[positions.Length];
        for (int i = 0; i < bolts.Length; i++)
        {
            GameObject boltObject = new GameObject("LightningBolt" + (i + 1));
            boltObject.transform.SetParent(transform, false);
            boltObject.transform.localPosition = positions[i];
            boltObject.transform.localRotation = Quaternion.Euler(0f, 0f, angles[i]);
            Vector3 scale = new Vector3(scales[i], scales[i], 1f);
            if (i >= bolts.Length / 2)
                scale.x = -scale.x;
            boltObject.transform.localScale = scale;

            SpriteRenderer bolt = boltObject.AddComponent<SpriteRenderer>();
            bolt.sprite = boltFramesBuilt[i % boltFramesBuilt.Length];
            bolt.sortingLayerName = "Characters";
            bolt.sortingOrder = -15;
            bolts[i] = bolt;
        }

        boltFrames = boltFramesBuilt;
        CacheBoltBaseTransforms();
    }

    private static Sprite LoadRuntimeSprite(string fileName)
    {
        Texture2D texture = Resources.Load<Texture2D>("VFX/GirlLightning/" + fileName);
        if (texture == null)
            return null;

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), 100f);
    }

    private void AnimateCage()
    {
        if (cage == null || cageFrames == null || cageFrames.Length == 0)
            return;

        int index = Mathf.FloorToInt((Time.time + phase) * cageFrameRate) % cageFrames.Length;
        cage.sprite = cageFrames[index];

        float pulse = 0.94f + 0.08f * Mathf.Sin(Time.time * 17f + phase);
        cage.transform.localScale = new Vector3(1.55f * pulse, 1.75f * pulse, 1f);
        cage.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 3f) * 5f);

        Color color = cage.color;
        color.a = 0.82f + 0.18f * Mathf.Abs(Mathf.Sin(Time.time * 19f + phase));
        cage.color = color;
    }

    private void AnimateBolts()
    {
        if (bolts == null || boltFrames == null || boltFrames.Length == 0)
            return;

        for (int i = 0; i < bolts.Length; i++)
        {
            SpriteRenderer bolt = bolts[i];
            if (bolt == null)
                continue;

            int frame = Mathf.FloorToInt((Time.time + phase + i * 1.7f) * boltFrameRate) % boltFrames.Length;
            bolt.sprite = boltFrames[frame];

            float sway = Mathf.Sin(Time.time * (6f + i * 0.6f) + i * 1.9f);
            bolt.transform.localPosition = boltBasePositions[i] + new Vector3(sway * 0.05f, Mathf.Cos(sway * 2f) * 0.04f, 0f);
            bolt.transform.localRotation = boltBaseRotations[i] * Quaternion.Euler(0f, 0f, sway * 9f);

            float pulse = 0.92f + 0.10f * Mathf.Abs(Mathf.Sin(Time.time * (13f + i) + i));
            bolt.transform.localScale = boltBaseScales[i] * pulse;

            Color color = bolt.color;
            color.a = 0.72f + 0.28f * Mathf.Abs(Mathf.Sin(Time.time * (16f + i) + phase));
            bolt.color = color;
        }
    }
}

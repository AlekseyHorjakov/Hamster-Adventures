using UnityEngine;

/// <summary>Постоянная фиолетовая молния вокруг Хомячихи во время суперудара.</summary>
[DisallowMultipleComponent]
public class GirlSuperAuraVFXController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer aura;
    [SerializeField] private Transform rayRoot;
    [SerializeField] private SpriteRenderer[] rays;
    [SerializeField] private float duration = 10f;

    private Transform follow;
    private SpriteRenderer followSprite;
    private float elapsed;
    private bool active;

    public void Play(Transform target, SpriteRenderer targetSprite, float effectDuration)
    {
        follow = target;
        followSprite = targetSprite;
        duration = Mathf.Max(0.1f, effectDuration);
        elapsed = 0f;
        active = true;
    }

    public void StopAndDestroy()
    {
        active = false;
        Destroy(gameObject, 0.25f);
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
            transform.position = new Vector3(center.x, center.y, follow.position.z - 0.12f);
        }

        if (rayRoot != null)
            rayRoot.localRotation *= Quaternion.Euler(0f, 0f, 130f * Time.deltaTime);

        float pulse = 0.72f + 0.28f * Mathf.Sin(Time.time * 18f);
        if (aura != null)
        {
            float scale = 1.8f + 0.22f * Mathf.Sin(Time.time * 7f);
            aura.transform.localScale = new Vector3(scale, scale, 1f);
            Color color = aura.color;
            color.a = 0.58f * pulse;
            aura.color = color;
        }

        if (rays != null)
        {
            for (int i = 0; i < rays.Length; i++)
            {
                if (rays[i] == null) continue;
                float flicker = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * (11f + i) + i));
                Color color = rays[i].color;
                color.a = flicker;
                rays[i].color = color;
                float rayScale = 1.25f + 0.25f * Mathf.Sin(Time.time * 9f + i * 1.7f);
                rays[i].transform.localScale = new Vector3(rayScale, 0.72f, 1f);
            }
        }

        if (elapsed >= duration)
            StopAndDestroy();
    }
}

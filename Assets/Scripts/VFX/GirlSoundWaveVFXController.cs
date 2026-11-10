using UnityEngine;

/// <summary>Расходящиеся во все стороны звуковые кольца от атаки Хомячихи.</summary>
[DisallowMultipleComponent]
public class GirlSoundWaveVFXController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] rings;
    [SerializeField] private SpriteRenderer flash;
    [SerializeField] private float duration = 0.52f;
    [SerializeField] private float maxScale = 7f;

    private float elapsed;

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));

        if (rings != null)
        {
            for (int i = 0; i < rings.Length; i++)
            {
                SpriteRenderer ring = rings[i];
                if (ring == null) continue;

                float delay = i * 0.09f;
                float local = Mathf.Clamp01((elapsed - delay) / Mathf.Max(0.01f, duration - delay));
                float scale = Mathf.Lerp(0.25f, maxScale + i * 0.7f, local);
                ring.transform.localScale = new Vector3(scale, scale, 1f);

                Color color = ring.color;
                color.a = Mathf.Sin(local * Mathf.PI) * 0.9f;
                ring.color = color;
            }
        }

        if (flash != null)
        {
            float flashT = Mathf.Clamp01(t / 0.35f);
            flash.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 3.2f, flashT);
            Color color = flash.color;
            color.a = 1f - flashT;
            flash.color = color;
        }

        if (t >= 1f)
            Destroy(gameObject);
    }
}

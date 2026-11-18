using System.Collections;
using UnityEngine;

/// <summary>
/// Тонущая доска: постоял 1 секунду — доска уходит под воду на 1.5 секунды, затем всплывает.
/// Пока доска под водой, коллайдер выключен и игрок падает.
/// </summary>
public sealed class SinkingPlatform : MonoBehaviour
{
    [SerializeField] private float sinkDelay = 1f;
    [SerializeField] private float hiddenTime = 1.5f;
    [SerializeField] private float warnShake = 0.12f;

    private Collider2D platformCollider;
    private SpriteRenderer[] renderers;
    private bool busy;

    private void Awake()
    {
        platformCollider = GetComponent<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (busy || collision.collider == null)
            return;
        if (collision.collider.GetComponentInParent<HamsterMovement>() == null)
            return;

        StartCoroutine(SinkCycle());
    }

    private IEnumerator SinkCycle()
    {
        busy = true;
        Vector3 start = transform.localPosition;

        // Лёгкая тряска перед погружением.
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            transform.localPosition = start + new Vector3(Mathf.Sin(t * 60f) * warnShake, 0f, 0f);
            yield return null;
        }
        transform.localPosition = start;

        yield return new WaitForSeconds(Mathf.Max(0f, sinkDelay - 0.3f));
        SetActive(false);
        yield return new WaitForSeconds(hiddenTime);
        SetActive(true);
        busy = false;
    }

    private void SetActive(bool visible)
    {
        if (platformCollider != null)
            platformCollider.enabled = visible;
        if (renderers != null)
            foreach (SpriteRenderer renderer in renderers)
                renderer.enabled = visible;
    }
}

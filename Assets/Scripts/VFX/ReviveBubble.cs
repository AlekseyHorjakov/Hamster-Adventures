using UnityEngine;
using UnityEngine.UI;

/// <summary>Текст внутри облачка оживления.</summary>
public class ReviveBubble : MonoBehaviour
{
    [SerializeField] private Text label;

    public void SetText(string value)
    {
        if (label != null) label.text = value;
    }
}

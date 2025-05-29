using System;
using UnityEngine;
using UnityEngine.UI;

internal class StatLine : MonoBehaviour
{
    [SerializeField] Image image; 
    [SerializeField] TMPro.TextMeshProUGUI ui; 
    internal void SetLine(Sprite sprite, string label, float value)
    {
        image.sprite = sprite;
        ui.text = $"{(value > 0f ? "+" : string.Empty)}{value} {label}" ; 
    }
}
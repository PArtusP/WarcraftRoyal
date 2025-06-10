using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

internal class StatLine : MonoBehaviour
{
    [SerializeField] Image image; 
    [SerializeField] TMPro.TextMeshProUGUI ui; 
    internal void SetLineAdd(Sprite sprite, string label, float value)
    {
        image.sprite = sprite;
        ui.text = $"{FormatSigned(value)} {label}" ; 
    }    
    internal void SetLineMult(Sprite sprite, string label, float value)
    {
        image.sprite = sprite;
        ui.text = $"{FormatPercent(value)} {label}" ; 
    }
    internal void SetLineSmall(string label, float value)
    { 
        ui.text = $"{label} {FormatSigned(value).Substring(1)}"; 
    }
    internal void SetLineSmall(string label, float value, float buffs)
    { 
        ui.text = $"{label}: {FormatSigned(value).Substring(1)}{(buffs != 0 ? $" ({FormatSigned(buffs)})" : string.Empty)}";
    }
    internal void SetLine(Sprite sprite, string label)
    {
        image.sprite = sprite;
        ui.text = label;
    } 
    internal void SetStatLineWithBuff(Sprite sprite, string label, float value, float buffs) =>
        ui.text = $"{label}: {FormatSigned(value).Substring(1)}{(buffs != 0 ? $" ({FormatSigned(buffs)})" : string.Empty)}"; 

    static internal string FormatSigned(float value) => value > 0 ? $"+{value:0.#}" : $"{value:0.#}"; 

    static internal string FormatPercent(float multiplier)
    {
        float percent = (multiplier - 1f) * 100f;
        return percent > 0 ? $"+{percent:0.#}%" : $"{percent:0.#}%";
    }
}
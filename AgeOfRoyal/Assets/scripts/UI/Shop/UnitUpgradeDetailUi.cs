using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class UnitUpgradeDetailUi : MonoBehaviour
{
    [SerializeField] new TMPro.TextMeshProUGUI name;
    [SerializeField] Image image;
    [SerializeField] StatLine statelinePrefab;
    [SerializeField] RectTransform statsContainer;

    private string Name { get => name.text; set => name.text = value; }
    private Sprite Image { get => image.sprite; set => image.sprite = value; }
    private MinionCombatStats Stats
    { 
        set
        {
            ClearChildren();
            if (value.health != 0) AddStatLine("health", value.health);
            if (value.damage != 0) AddStatLine("damage", value.damage);
            if (value.speed != 0) AddStatLine("speed", value.speed);
            if (value.cooldown != 0) AddStatLine("cooldown", value.cooldown);
            if (value.hitRadius != 0) AddStatLine("range", value.hitRadius); 
        }
    }

    internal void Display(UnitUpgradeButton button)
    {
        Name = button.Name;
        Image = button.Image;
        Stats = button.PowerUp;
    }

    private void AddStatLine(string label, float value) => Instantiate(statelinePrefab, statsContainer).SetLine(null, label, value); 
    
    public void ClearChildren()
    {
        for (int i = statsContainer.childCount - 1; i >= 0; i--)
        {
            GameObject.Destroy(statsContainer.GetChild(i).gameObject);
        }
    }
}

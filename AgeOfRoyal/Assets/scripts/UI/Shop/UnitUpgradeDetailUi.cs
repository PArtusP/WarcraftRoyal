using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class UnitUpgradeDetailUi : MonoBehaviour
{
    static UnitUpgradeDetailUi instance = null;
    [SerializeField] new TMPro.TextMeshProUGUI name;
    [SerializeField] Image image;
    [SerializeField] StatLine statelinePrefab;
    [SerializeField] StatLine statelineSmallPrefab;
    [SerializeField] StatLineDouble statelineDoublePrefab;
    [SerializeField] RectTransform statsContainer;
    public static UnitUpgradeDetailUi Instance
    {
        get
        {
            if (instance == null && NetworkManager.Singleton.IsConnectedClient)
            {
                var players = FindObjectsByType<Player>(FindObjectsSortMode.None);

                var self = players.FirstOrDefault(p => p.IsOwner || NetworkManager.Singleton.IsServer);
                if (self) instance = self.ShopUi.DetailUi;
                if (instance == null)
                {
                    /*throw new NullReferenceException("UnitUpgradeDetailUi instance not found in the scene. Please ensure it is present.");*/
                }
                else
                {
                    instance.OnUpdateEvent.AddListener(instance.Display);  // Register the Display method to the OnUpdateEvent}
                }
            }
            return instance;
        }
    }
    private string Name { get => name.text; set => name.text = value; }
    private Sprite Image { get => image.sprite; set => image.sprite = value; }
    private List<UnitModule> Modules
    {
        set => value.ForEach(v => AddModuleLine(v.Description, v.Icon));
    }
    private List<UpgradeDescription> Descriptions
    {
        set => value.ForEach(v => AddModuleLine(v.description, v.icon));
    }
    internal void Display(UnitUpgradeButton button)
    {
        gameObject.SetActive(true);
        ClearChildren();
        Name = button.Name;
        Image = button.Image;
        PowerUp = button.Buff.PowerUp;
        Triggers = button.Buff.Triggers;
        Descriptions = button.Descriptions;
        Modules = button.Modules;
    }
    internal void Display(UnitWithoutState unit) => Display(unit, null);
    internal void Display(UnitWithoutState unit, UnitPowerUp buffs, bool inGame = false)
    {
        buffs = buffs != null ? buffs : inGame ? unit.TotalBuff : unit.TotalBuffNoFilter;
        gameObject.SetActive(true);
        ClearChildren();
        Name = unit.Name;
        Image = unit.icon;


        AddDoubleStatLine("HP", unit.Stats.health,
            buffs != null ? buffs.addStats.health : 0,
            buffs != null ? buffs.multStats.health : 1,
            "damage", unit.Stats.damage,
            buffs != null ? buffs.addStats.damage : 0,
            buffs != null ? buffs.multStats.damage : 1);

        AddDoubleStatLine("speed", unit.Stats.speed,
            buffs != null ? buffs.addStats.speed : 0,
            buffs != null ? buffs.multStats.speed : 1,
            "rate", 1f / unit.Stats.cooldown,
            buffs != null && buffs.addStats.cooldown != 0 ? 1f / buffs.addStats.cooldown : 0, // @TODO hmm
            buffs != null && buffs.multStats.cooldown != 1f ? 2f - buffs.multStats.cooldown : 1); // @TODO hmm

        AddDoubleStatLine("range", unit.Stats.hitRadius,
            buffs != null ? buffs.addStats.hitRadius : 0,
            buffs != null ? buffs.multStats.hitRadius : 1,
            "heal MOD", unit.Stats.healModifier,
            buffs != null ? buffs.addStats.healModifier : 0,
            buffs != null ? buffs.multStats.healModifier : 1);

        AddDoubleStatLine("melee DEF", unit.Stats.armorMelee,
            buffs != null ? buffs.addStats.armorMelee : 0,
            buffs != null ? buffs.multStats.armorMelee : 1,
            "range DEF", unit.Stats.armorRange,
            buffs != null ? buffs.addStats.armorRange : 0,
            buffs != null ? buffs.multStats.armorRange : 1);

        Modules = unit.Modules;
    }
    public UnityEvent<UnitWithoutState> OnUpdateEvent { get; internal set; } = new UnityEvent<UnitWithoutState>();

    virtual protected UnitPowerUp PowerUp
    {
        set
        {
            if (value.addStats.health != 0) AddStatLineAdd("HP", value.addStats.health);
            if (value.addStats.damage != 0) AddStatLineAdd("damage", value.addStats.damage);
            if (value.addStats.speed != 0) AddStatLineAdd("speed", value.addStats.speed);
            if (value.addStats.cooldown != 0) AddStatLineAdd("rate", 1f / value.addStats.cooldown); // @TODO hmm
            if (value.addStats.hitRadius != 0) AddStatLineAdd("range", value.addStats.hitRadius);
            if (value.addStats.armorRange != 0) AddStatLineAdd("range DEF", value.addStats.armorRange);
            if (value.addStats.armorMelee != 0) AddStatLineAdd("melee DEF", value.addStats.armorMelee);
            if (value.addStats.armorMelee != 0) AddStatLineAdd("heal MOD", value.addStats.healModifier);

            if (value.multStats.health != 1) AddStatLineMult("HP", value.multStats.health);
            if (value.multStats.damage != 1) AddStatLineMult("damage", value.multStats.damage);
            if (value.multStats.speed != 1) AddStatLineMult("speed", value.multStats.speed);
            if (value.multStats.cooldown != 1) AddStatLineMult("rate", 2f - value.multStats.cooldown); // @TODO hmm
            if (value.multStats.hitRadius != 1) AddStatLineMult("range", value.multStats.hitRadius);
            if (value.multStats.armorRange != 1) AddStatLineMult("range DEF", value.multStats.armorRange);
            if (value.multStats.armorMelee != 1) AddStatLineMult("melee DEF", value.multStats.armorMelee);
            if (value.multStats.armorMelee != 1) AddStatLineMult("heal MOD", value.multStats.healModifier);
        }
    }

    public List<BuffApplyTrigger> Triggers
    {
        set
        {
            value.ForEach(v =>
            {
                if (v.ActionBuff.PowerUp.addStats.health != 0) AddStatLineAdd($"On {v.Type}: HP", v.ActionBuff.PowerUp.addStats.health);
                if (v.ActionBuff.PowerUp.addStats.damage != 0) AddStatLineAdd($"On {v.Type}: damage", v.ActionBuff.PowerUp.addStats.damage);
                if (v.ActionBuff.PowerUp.addStats.speed != 0) AddStatLineAdd($"On {v.Type}: speed", v.ActionBuff.PowerUp.addStats.speed);
                if (v.ActionBuff.PowerUp.addStats.cooldown != 0) AddStatLineAdd($"On {v.Type}: rate", 1f / v.ActionBuff.PowerUp.addStats.cooldown); // @TODO hmm
                if (v.ActionBuff.PowerUp.addStats.hitRadius != 0) AddStatLineAdd($"On {v.Type}: range", v.ActionBuff.PowerUp.addStats.hitRadius);
                if (v.ActionBuff.PowerUp.addStats.armorRange != 0) AddStatLineAdd($"On {v.Type}: range DEF", v.ActionBuff.PowerUp.addStats.armorRange);
                if (v.ActionBuff.PowerUp.addStats.armorMelee != 0) AddStatLineAdd($"On {v.Type}: melee DEF", v.ActionBuff.PowerUp.addStats.armorMelee);
                if (v.ActionBuff.PowerUp.addStats.armorMelee != 0) AddStatLineAdd($"On {v.Type}: heal MOD", v.ActionBuff.PowerUp.addStats.healModifier);

                if (v.ActionBuff.PowerUp.multStats.health != 1) AddStatLineMult($"On {v.Type}: HP", v.ActionBuff.PowerUp.multStats.health);
                if (v.ActionBuff.PowerUp.multStats.damage != 1) AddStatLineMult($"On {v.Type}: damage", v.ActionBuff.PowerUp.multStats.damage);
                if (v.ActionBuff.PowerUp.multStats.speed != 1) AddStatLineMult($"On {v.Type}: speed", v.ActionBuff.PowerUp.multStats.speed);
                if (v.ActionBuff.PowerUp.multStats.cooldown != 1) AddStatLineMult($"On {v.Type}: rate", 2f - v.ActionBuff.PowerUp.multStats.cooldown); // @TODO hmm
                if (v.ActionBuff.PowerUp.multStats.hitRadius != 1) AddStatLineMult($"On {v.Type}: range", v.ActionBuff.PowerUp.multStats.hitRadius);
                if (v.ActionBuff.PowerUp.multStats.armorRange != 1) AddStatLineMult($"On {v.Type}: range DEF", v.ActionBuff.PowerUp.multStats.armorRange);
                if (v.ActionBuff.PowerUp.multStats.armorMelee != 1) AddStatLineMult($"On {v.Type}: melee DEF", v.ActionBuff.PowerUp.multStats.armorMelee);
                if (v.ActionBuff.PowerUp.multStats.armorMelee != 1) AddStatLineMult($"On {v.Type}: heal MOD", v.ActionBuff.PowerUp.multStats.healModifier);
            });
        }
    }

    private void AddStatLineAdd(string label, float value) => Instantiate(statelinePrefab, statsContainer).SetLineAdd(null, label, value);
    private void AddStatLineMult(string label, float value) => Instantiate(statelinePrefab, statsContainer).SetLineMult(null, label, value);
    private void AddModuleLine(string label, Sprite image = null) => Instantiate(statelinePrefab, statsContainer).SetLine(image, label);
    private void AddStatLine(string label, float stats, float addBuff, float multBuff) => Instantiate(statelineSmallPrefab, statsContainer).SetStatLineWithBuff(null, label, stats, (stats + addBuff) * multBuff - stats);
    private void AddDoubleStatLine(string label1, float stats1, float addBuff1, float multBuff1,
        string label2, float stats2, float addBuff2, float multBuff2) =>
        Instantiate(statelineDoublePrefab, statsContainer)
        .SetLines(label1, stats1, (stats1 + addBuff1) * multBuff1 - stats1,
            label2, stats2, (stats2 + addBuff2) * multBuff2 - stats2);

    public void ClearChildren()
    {
        for (int i = statsContainer.childCount - 1; i >= 0; i--)
        {
            GameObject.Destroy(statsContainer.GetChild(i).gameObject);
        }
    }

    internal void Close() => gameObject.SetActive(false);

}

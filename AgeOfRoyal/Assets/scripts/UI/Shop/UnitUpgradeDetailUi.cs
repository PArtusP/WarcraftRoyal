using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
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

                var self = players.FirstOrDefault(p =>  p.IsOwner || 
            NetworkManager.Singleton.IsServer);
                if (self) instance = self.ShopUi.DetailUi;
                if (instance == null)
                {
/*                    throw new NullReferenceException("UnitUpgradeDetailUi instance not found in the scene. Please ensure it is present.");*/
                }
            }
            return instance;
        }
    }
    private string Name { get => name.text; set => name.text = value; }
    private Sprite Image { get => image.sprite; set => image.sprite = value; }
    private List<UnitModule> Modules
    {
        set => value.ForEach(v => AddModuleLine(v.Description));
    }
    internal void Display(UnitUpgradeButton button)
    {
        gameObject.SetActive(true);
        ClearChildren();
        Name = button.Name;
        Image = button.Image;
        PowerUp = button.Buff.PowerUp;
        Modules = button.Modules;
    }
    internal void Display(UnitWithoutState Minion, UnitPowerUp buffs)
    {
        gameObject.SetActive(true);
        ClearChildren();
        Name = Minion.Name;
        Image = Minion.icon;


        AddDoubleStatLine("HP", Minion.Stats.health,
            buffs != null ? buffs.addStats.health : 0,
            buffs != null ? buffs.multStats.health : 1,
            "damage", Minion.Stats.damage,
            buffs != null ? buffs.addStats.damage : 0,
            buffs != null ? buffs.multStats.damage : 1);

        AddDoubleStatLine("speed", Minion.Stats.speed,
            buffs != null ? buffs.addStats.speed : 0,
            buffs != null ? buffs.multStats.speed : 1,
            "rate", 1f / Minion.Stats.cooldown,
            buffs != null && buffs.addStats.cooldown != 0 ? 1f / buffs.addStats.cooldown : 0, // @TODO hmm
            buffs != null && buffs.multStats.cooldown != 1f ? 2f - buffs.multStats.cooldown : 1); // @TODO hmm

        AddDoubleStatLine("range", Minion.Stats.hitRadius,
            buffs != null ? buffs.addStats.hitRadius : 0,
            buffs != null ? buffs.multStats.hitRadius : 1,
            "range DEF", Minion.Stats.armorRange,
            buffs != null ? buffs.addStats.armorRange : 0,
            buffs != null ? buffs.multStats.armorRange : 1);

        AddStatLine("melee DEF", Minion.Stats.armorMelee,
            buffs != null ? buffs.addStats.armorMelee : 0,
            buffs != null ? buffs.multStats.armorMelee : 1);

        Modules = Minion.Modules;
    }

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

            if (value.multStats.health != 1) AddStatLineMult("HP", value.multStats.health);
            if (value.multStats.damage != 1) AddStatLineMult("damage", value.multStats.damage);
            if (value.multStats.speed != 1) AddStatLineMult("speed", value.multStats.speed);
            if (value.multStats.cooldown != 1) AddStatLineMult("rate", 2f - value.multStats.cooldown); // @TODO hmm
            if (value.multStats.hitRadius != 1) AddStatLineMult("range", value.multStats.hitRadius);
            if (value.multStats.armorRange != 1) AddStatLineMult("range DEF", value.multStats.armorRange);
            if (value.multStats.armorMelee != 1) AddStatLineMult("melee DEF", value.multStats.armorMelee);
        }
    } 


    private void AddStatLineAdd(string label, float value) => Instantiate(statelinePrefab, statsContainer).SetLineAdd(null, label, value);
    private void AddStatLineMult(string label, float value) => Instantiate(statelinePrefab, statsContainer).SetLineMult(null, label, value);
    private void AddModuleLine(string label) => Instantiate(statelinePrefab, statsContainer).SetLine(null, label);
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

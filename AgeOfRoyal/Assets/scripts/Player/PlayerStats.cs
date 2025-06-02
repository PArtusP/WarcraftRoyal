using System;
using UnityEngine;

[Serializable]
public class PlayerStats
{
    [SerializeField] public float moneyToExperienceMultiplier = 1f;
    [SerializeField] public int extraMoney = 0;
    [SerializeField] public float unityRegeneration = 0f;

    public static PlayerStats Zero => new PlayerStats()
    {
        moneyToExperienceMultiplier = 0f,
        extraMoney = 0,
        unityRegeneration = 0f
    };

    public void Add(PlayerStats other)
    {
        moneyToExperienceMultiplier += other.moneyToExperienceMultiplier;
        extraMoney += other.extraMoney;
        unityRegeneration += other.unityRegeneration;
    }

    public static PlayerStats operator +(PlayerStats a, PlayerStats b) => new PlayerStats()
    {
        moneyToExperienceMultiplier = a.moneyToExperienceMultiplier + b.moneyToExperienceMultiplier,
        extraMoney = a.extraMoney + b.extraMoney,
        unityRegeneration = a.unityRegeneration + b.unityRegeneration
    };

    public static PlayerStats operator -(PlayerStats a) => new PlayerStats()
    {
        moneyToExperienceMultiplier = -a.moneyToExperienceMultiplier,
        extraMoney = -a.extraMoney,
        unityRegeneration = -a.unityRegeneration
    };

    public override string ToString()
    {
        return $"Money→XP Multiplier: {moneyToExperienceMultiplier}, Extra Money: {extraMoney}, Unity Regen: {unityRegeneration}";
    }
}

using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Android;

[Serializable]
public class UnitPowerUp : INetworkSerializable
{
    [SerializeField] public UnitStats addStats = UnitStats.Zero;
    [SerializeField] public UnitStats multStats = UnitStats.One;

    static public UnitPowerUp Identity => new UnitPowerUp();

    public override bool Equals(object obj)
    {
        if (obj is UnitPowerUp o && o != null)
        {
            return addStats.Equals(o.addStats) && multStats.Equals(o.multStats);
        }

        return false;
    }

    public bool IsBuff
    {
        get
        { 
            return addStats.health >= 0 && addStats.damage >= 0 && addStats.speed >= 0 &&
                   addStats.cooldown <= 0 && addStats.sightRadius >= 0 && addStats.hitRadius >= 0 &&
                   addStats.armorRange >= 0 && addStats.armorMelee >= 0 &&
                   multStats.health >= 1 && multStats.damage >= 1 && multStats.speed >= 1 &&
                   multStats.cooldown <= 1 && multStats.sightRadius >= 1 && multStats.hitRadius >= 1 &&
                   multStats.armorRange >= 1 && multStats.armorMelee >= 1;
        }
    }
    public string Short
    {
        get
        {
            var parts = new List<string>();

            if (addStats.health != 0) parts.Add($"{StatLine.FormatSigned(addStats.health)} HP");
            if (addStats.damage != 0) parts.Add($"{StatLine.FormatSigned(addStats.damage)} damag");
            if (addStats.speed != 0) parts.Add($"{StatLine.FormatSigned(addStats.speed)} speed");
            if (addStats.cooldown != 0) parts.Add($"{StatLine.FormatSigned(1f / addStats.cooldown)} rate"); // @TODO hmm
            if (addStats.sightRadius != 0) parts.Add($"{StatLine.FormatSigned(addStats.sightRadius)} vision");
            if (addStats.hitRadius != 0) parts.Add($"{StatLine.FormatSigned(addStats.hitRadius)} range");
            if (addStats.armorRange != 0) parts.Add($"{StatLine.FormatSigned(addStats.armorRange)} range DEF");
            if (addStats.armorMelee != 0) parts.Add($"{StatLine.FormatSigned(addStats.armorMelee)} melee DEF");

            if (multStats.health != 1) parts.Add($"{StatLine.FormatPercent(multStats.health)} HP");
            if (multStats.damage != 1) parts.Add($"{StatLine.FormatPercent(multStats.damage)} dmg");
            if (multStats.speed != 1) parts.Add($"{StatLine.FormatPercent(multStats.speed)} speed");
            if (multStats.cooldown != 1) parts.Add($"{StatLine.FormatPercent(2f - multStats.cooldown)} rate"); // @TODO hmm
            if (multStats.sightRadius != 1) parts.Add($"{StatLine.FormatPercent(multStats.sightRadius)} vision");
            if (multStats.hitRadius != 1) parts.Add($"{StatLine.FormatPercent(multStats.hitRadius)} range");
            if (multStats.armorRange != 1) parts.Add($"{StatLine.FormatPercent(multStats.armorRange)} range DEF");
            if (multStats.armorMelee != 1) parts.Add($"{StatLine.FormatPercent(multStats.armorMelee)} melee DEF");

            string result = string.Join(", ", parts);
            int lastComma = result.LastIndexOf(", ");
            if (lastComma >= 0)
                result = result.Substring(0, lastComma) + " &" + result.Substring(lastComma + 1);
            return result;
        }
    }

    /// <summary>
    /// Applies the power-up to UnitStats `a`, and returns the difference as a new UnitStats instance.
    /// </summary>
    public static UnitStats operator +(UnitStats a, UnitPowerUp b)
    {
        UnitStats res = new UnitStats()
        {
            health = a.health,
            damage = a.damage,
            speed = a.speed,
            cooldown = a.cooldown,
            sightRadius = a.sightRadius,
            hitRadius = a.hitRadius,
            armorRange = a.armorRange,
            armorMelee = a.armorMelee,
        };

        res += b.addStats;
        res *= b.multStats;

        // Calculate difference
        return res;
    }

    /// <summary>
    /// Combines two UnitPowerUps into a new one and returns the difference from the first.
    /// </summary>
    public static UnitPowerUp operator +(UnitPowerUp a, UnitPowerUp b)
    {
        UnitStats newAdd = a.addStats + b.addStats;

        UnitStats newMult = new UnitStats()
        {
            health = a.multStats.health * b.multStats.health,
            damage = a.multStats.damage * b.multStats.damage,
            speed = a.multStats.speed * b.multStats.speed,
            cooldown = a.multStats.cooldown * b.multStats.cooldown,
            sightRadius = a.multStats.sightRadius * b.multStats.sightRadius,
            hitRadius = a.multStats.hitRadius * b.multStats.hitRadius,
            armorRange = a.multStats.armorRange * b.multStats.armorRange,
            armorMelee = a.multStats.armorMelee * b.multStats.armorMelee,
        };

        // Calculate difference from `a`
        return new UnitPowerUp()
        {
            addStats = newAdd,
            multStats = newMult,
        };
    }
    public static UnitPowerUp operator -(UnitPowerUp p)
    {
        return new UnitPowerUp()
        {
            addStats = -p.addStats,
            multStats = 1f / p.multStats
        };
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref addStats);
        serializer.SerializeValue(ref multStats);
    }
}

public static class UnitPowerUpExtensions
{
    public static UnitPowerUp SumPowerUps(this IEnumerable<UnitPowerUp> buffs)
    {
        var result = UnitPowerUp.Identity;

        foreach (var buff in buffs)
            result += buff;

        return result;
    }
}
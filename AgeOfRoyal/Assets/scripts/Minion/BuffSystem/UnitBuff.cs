using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public enum UnitBuffType
{
    OneShot,    // Instant effect, no duration (e.g., +50 HP heal now)
    Temporary,  // Has duration and can expire (most buffs/debuffs)
    Permanent,  // Never expires, not dispellable unless removed manually
    Refreshable, // Like Temporary, but refreshes/extends on re-application
    Aura,       // Active as long as the source is nearby/active
    Stackable, // Like Temporary, but can be stacked from the same source hitable
}

[Serializable]
public class UnitBuff : INetworkSerializable
{
    [Header("Buff settings")]
    [SerializeField] private UnitBuffType buffType = UnitBuffType.Temporary;
    [SerializeField] private float duration = 1f;
    [SerializeField] private int maxStack = 1;
    [SerializeField] private bool canBeDispelled = true;

    [Header("Power up effect")]
    [SerializeField] private UnitPowerUp powerup = new UnitPowerUp();
    [SerializeField] private float heal = 0f;
    [SerializeField] private bool dispel = false;

    [NonSerialized] private Guid sourceId = Guid.NewGuid();
    [NonSerialized] private float appliedTime;


    [NonSerialized] private Hitable source;

    public float Duration => duration;
    //public UnitPowerUp PowerUp => buff;
    public UnitPowerUp PowerUp { get => powerup; set => powerup = value; }
    public int MaxStack => maxStack;
    public bool CanBeDispelled => canBeDispelled;
    public float Heal => heal;
    public Guid SourceId => sourceId;
    public float AppliedTime => appliedTime;
    public float EndTime => appliedTime + duration;

    public UnitBuffType BuffType { get => buffType; set => buffType = value; }
    public Hitable Source { get => source; set => source = value; }
    public bool Dispel { get => dispel; set => dispel = value; }

    public void Apply() => appliedTime = Time.time;

    public bool IsExpired()
    { 
        Debug.Log("Checking if buff is expired: " + buffType + " applied at: " + appliedTime + " with duration: " + duration + " current time: " + Time.time);
        return (buffType == UnitBuffType.Temporary || buffType == UnitBuffType.Refreshable || buffType == UnitBuffType.Stackable) && Time.time - appliedTime >= duration;
    }

    public void Refresh() => appliedTime = Time.time;

    public bool CanStackWith(UnitBuff other) => !(sourceId == other.sourceId && GetStackCount(other) >= maxStack);

    public int GetStackCount(UnitBuff other) => sourceId == other.sourceId ? 1 : 0;

    public UnitBuff Clone()
    {
        return new UnitBuff
        {
            buffType = this.buffType,
            duration = this.duration,
            powerup = this.powerup,
            maxStack = this.maxStack,
            canBeDispelled = this.canBeDispelled,
            heal = this.heal,
            sourceId = this.sourceId,
            dispel = this.dispel
        };
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref buffType);
        serializer.SerializeValue(ref duration);
        serializer.SerializeValue(ref maxStack);
        serializer.SerializeValue(ref canBeDispelled);
        serializer.SerializeValue(ref powerup); // assuming UnitPowerUp implements INetworkSerializable
        serializer.SerializeValue(ref heal);
        serializer.SerializeValue(ref dispel);

        // Serialize Guid manually as bytes
        if (serializer.IsWriter)
        {
            byte[] guidBytes = sourceId.ToByteArray();
            for (int i = 0; i < guidBytes.Length; i++)
            {
                byte b = guidBytes[i];
                serializer.SerializeValue(ref b);
            }
        }
        else
        {
            byte[] guidBytes = new byte[16];
            for (int i = 0; i < guidBytes.Length; i++)
            {
                byte b = 0;
                serializer.SerializeValue(ref b);
                guidBytes[i] = b;
            }
            sourceId = new Guid(guidBytes);
        }

        // You can optionally sync appliedTime if needed, but it's often client-local
    }
    public override bool Equals(object obj)
    {
        if (obj is UnitBuff other)
        {
            return sourceId == other.sourceId && powerup.Equals(other.powerup);
        }
        return false;
    }

    public override int GetHashCode() => sourceId.GetHashCode() ^ powerup.GetHashCode();

    public override string ToString()
    {
        List<string> parts = new List<string>();

        parts.Add(buffType.ToString());

        if (buffType == UnitBuffType.Stackable && maxStack > 1)
            parts.Add($"x{maxStack}");

        if (duration > 0 && buffType != UnitBuffType.OneShot && buffType != UnitBuffType.Permanent && buffType != UnitBuffType.Aura)
            parts.Add($"{duration:0.##}s");

        if (dispel)
            parts.Add("apply dispel");

        if (!UnitPowerUp.Identity.Equals(powerup))
            parts.Add($"{(powerup.IsBuff ? "Buff" : "Debuff")}: '{powerup.Short}'");

        if (heal != 0f)
            parts.Add(buffType == UnitBuffType.OneShot ? $"+{heal:0.#} HP" : $"+{heal:0.#} HP/s");

        if (buffType != UnitBuffType.OneShot && buffType != UnitBuffType.Permanent && buffType != UnitBuffType.Aura && !canBeDispelled)
            parts.Add("undispellable");

        return string.Join(", ", parts);
    }

}
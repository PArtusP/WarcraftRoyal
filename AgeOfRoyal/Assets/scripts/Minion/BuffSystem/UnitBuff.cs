using System;
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
public class UnitBuff
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

    public void Apply()
    {
        appliedTime = Time.time;
    }

    public bool IsExpired()
    {
        return (buffType == UnitBuffType.Temporary || buffType == UnitBuffType.Refreshable) && Time.time - appliedTime >= duration;
    }

    public void Refresh()
    {
        appliedTime = Time.time;
    }

    public bool CanStackWith(UnitBuff other)
    {
        return !(sourceId == other.sourceId && GetStackCount(other) >= maxStack);
    }

    public int GetStackCount(UnitBuff other)
    {
        return sourceId == other.sourceId ? 1 : 0;
    }

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
            sourceId = this.sourceId
        };
    }

    public override bool Equals(object obj)
    {
        if (obj is UnitBuff other)
        {
            return sourceId == other.sourceId && powerup.Equals(other.powerup);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return sourceId.GetHashCode() ^ powerup.GetHashCode();
    }

    public override string ToString()
    {
        string status = duration == 0 ? "Permanent" : $"{duration}s";
        return $"{(powerup.IsBuff ? "Buff" : "Debuff")} [{powerup.Short}] ({status}, Stack:{maxStack}, Heal:{heal}/s)";
    }
}

[Serializable]
public class UnitBuffWithTargeting : UnitBuff
{
    [SerializeField] private TargetPicking pickiong;

    public TargetPicking Pickiong => pickiong;  
}
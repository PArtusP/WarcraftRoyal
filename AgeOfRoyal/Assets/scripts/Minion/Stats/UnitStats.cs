using System;
using UnityEngine;

[Serializable]
public class UnitStats
{
    [SerializeField] public float health = 100f;
    [SerializeField] public float damage = 10f;
    [SerializeField] public float speed = 2f;
    [SerializeField] public float cooldown = 1.5f;
    [SerializeField] public float sightRadius = 8f;
    [SerializeField] public float hitRadius = .4f;
    [SerializeField] public float armorRange = 0f;
    [SerializeField] public float armorMelee = 0f;
    public override bool Equals(object obj)
    {
        if (obj is UnitStats s)
        {
            return health == s.health &&
                damage == s.damage && 
                speed == s.speed &&
                cooldown == s.cooldown &&
                sightRadius == s.sightRadius &&
                hitRadius == s.hitRadius &&
                armorRange == s.armorRange &&
                armorMelee == s.armorMelee;
        }

        return false;
    }

    public static UnitStats Zero => new UnitStats()
    {
        health = 0f,
        damage = 0f,
        speed = 0f,
        cooldown = 0f,
        sightRadius = 0f,
        hitRadius = 0f,
        armorRange = 0f,
        armorMelee = 0f,
    };
    public static UnitStats One => new UnitStats()
    {
        health = 1f,
        damage = 1f,
        speed = 1f,
        cooldown = 1f,
        sightRadius = 1f,
        hitRadius = 1f,
        armorRange = 1f,
        armorMelee = 1f,
    };  
    public static UnitStats operator +(UnitStats a, UnitStats b) => new UnitStats()
    {
        health = a.health + b.health,
        damage = a.damage + b.damage,
        speed = a.speed + b.speed,
        cooldown = a.cooldown + b.cooldown,
        sightRadius = a.sightRadius + b.sightRadius,
        hitRadius = a.hitRadius + b.hitRadius,
        armorRange = a.armorRange + b.armorRange,
        armorMelee = a.armorMelee + b.armorMelee,
    };

    public static UnitStats operator -(UnitStats a) => new UnitStats()
    {
        health = -a.health,
        damage = -a.damage,
        speed = -a.speed,
        cooldown = -a.cooldown,
        sightRadius = -a.sightRadius,
        hitRadius = -a.hitRadius,
        armorRange = -a.armorRange,
        armorMelee = -a.armorMelee,
    };
    public static UnitStats operator -(UnitStats a, UnitStats b) => a + (-b);
    public static UnitStats operator *(UnitStats a, UnitStats b) => new UnitStats()
    {
        health = a.health * b.health,
        damage = a.damage * b.damage,
        speed = a.speed * b.speed,
        cooldown = a.cooldown * b.cooldown,
        sightRadius = a.sightRadius * b.sightRadius,
        hitRadius = a.hitRadius * b.hitRadius,
        armorRange = a.armorRange * b.armorRange,
        armorMelee = a.armorMelee * b.armorMelee,
    };
    public static UnitStats operator /(UnitStats a, UnitStats b) => new UnitStats()
    {
        health = a.health / b.health,
        damage = a.damage / b.damage,
        speed = a.speed / b.speed,
        cooldown = a.cooldown / b.cooldown,
        sightRadius = a.sightRadius / b.sightRadius,
        hitRadius = a.hitRadius / b.hitRadius,
        armorRange = a.armorRange / b.armorRange,
        armorMelee = a.armorMelee / b.armorMelee,
    };
    public static UnitStats operator *(float a, UnitStats b) => One * b;
    public static UnitStats operator /(float a, UnitStats b) => a * (One / b);

    public override string ToString()
    {
        return $"Health: {health}, Damage: {damage}, Speed: {speed}, Cooldown: {cooldown}, SightRadius: {sightRadius}, HitRadius: {hitRadius}, ArmorRange: {armorRange}, ArmorMelee: {armorMelee}";
    }
}

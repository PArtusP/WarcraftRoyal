using System;
using UnityEngine;

[Serializable]
public class MinionCombatStats
{
    [SerializeField] public float health = 100f;
    [SerializeField] public float damage = 10f;
    [SerializeField] public float speed = 2f;
    [SerializeField] public float cooldown = 1.5f;
    [SerializeField] public float sightRadius = 8f;
    [SerializeField] public float hitRadius = .4f;
    [SerializeField] public float armorRange = 1f;
    [SerializeField] public float armorMelee = 1f;

    public static MinionCombatStats Zero => new MinionCombatStats()
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

    public void Add(MinionCombatStats a)
    {
        health += a.health;
        damage += a.damage;
        speed += a.speed;
        cooldown += a.cooldown;
        sightRadius += a.sightRadius;
        hitRadius += a.hitRadius;
        armorRange += a.armorRange;
        armorMelee += a.armorMelee;
    }

    public static MinionCombatStats operator +(MinionCombatStats a, MinionCombatStats b) => new MinionCombatStats()
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

    public static MinionCombatStats operator -(MinionCombatStats a) => new MinionCombatStats()
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

    public override string ToString()
    {
        return $"Health: {health}, Damage: {damage}, Speed: {speed}, Cooldown: {cooldown}, SightRadius: {sightRadius}, HitRadius: {hitRadius}, ArmorRange: {armorRange}, ArmorMelee: {armorMelee}";
    }
}

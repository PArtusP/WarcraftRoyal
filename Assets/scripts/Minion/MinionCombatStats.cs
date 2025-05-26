using System;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class MinionCombatStats
{
    [SerializeField] public float damage = 10f;
    [SerializeField] public float cooldown = 1.5f;
    [SerializeField] public float sightRadius = 8f;
    [SerializeField] public float hitRadius = .4f;

    static public MinionCombatStats Zero => new MinionCombatStats()
    {
        damage = 0f,
        cooldown = 0f,
        sightRadius = 0f,
        hitRadius = 0f,
    };

    public void Add(MinionCombatStats a)
    {
        damage += a.damage;
        cooldown += a.cooldown;
        sightRadius += a.sightRadius;
        hitRadius += a.hitRadius;
    }
    public static MinionCombatStats operator +(MinionCombatStats a, MinionCombatStats b) => new MinionCombatStats()
    {
        damage = a.damage + b.damage,
        cooldown = a.cooldown + b.cooldown,
        sightRadius = a.sightRadius + b.sightRadius,
        hitRadius = a.hitRadius + b.hitRadius,
    };
    public static MinionCombatStats operator -(MinionCombatStats a) => new MinionCombatStats()
    {
        damage = -a.damage,
        cooldown = -a.cooldown,
        sightRadius = -a.sightRadius,
        hitRadius = -a.hitRadius
    };
    public override string ToString()
    {
        return $"Damage: {damage}, Cooldown: {cooldown}, SightRadius: {sightRadius}, HitRadius: {hitRadius}";
    }


}
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
abstract public class UnitAction : ScriptableObject
{
    public int ID = -1;
    abstract public string AnimationTrigger { get; }
    abstract public float MaxRadius { get; }
    abstract public float MinRadius { get; }
    abstract public float Cooldown { get; }

    [SerializeField] TriggerSVFX vfx;
    public TriggerSVFX Vfx => vfx;  

    abstract public bool Use(UnitWithoutState owner);

    public virtual UnitAction Clone()
    {
        return Instantiate(this);
    }
}



[System.Serializable]
abstract public class UnitAttack : UnitAction
{
    [SerializeField] internal float radius = 5f;   // @TODO : bof
    [SerializeField] internal float cooldown = 0f;   // @TODO : bof
    [SerializeField] protected float bonusMultiplier = 2f;
    [SerializeField] protected Class bonusAgainst;

    public override float MaxRadius => radius;
    public override float MinRadius => radius;
    public override float Cooldown => cooldown;
    override public string AnimationTrigger => "Attack"; 
    public override UnitAction Clone()
    {
        var res = Instantiate(this);
        res.radius = this.radius;
        res.bonusMultiplier = this.bonusMultiplier;
        res.bonusAgainst = this.bonusAgainst;
        return res;
        
    }
}


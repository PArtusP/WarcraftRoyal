using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "___ - MOD AOE Buff", menuName = "Unit Modules/AOE Buff", order = 1)]
public class AoeBuffModule : AoeUnitModule
{  
    [SerializeField] private UnitBuff buff =  new UnitBuff();
    [SerializeField] private List<Minion> targets = new List<Minion>(); 


    public override string Description => 
        $"{(picking.SameTeam == Target.Friends ? "Grants" : "Inflicts")} " +
        $"{buff.PowerUp.Short}" +
        $"{(buff.Heal > 0 ? $"{(buff.PowerUp.Short != string.Empty ? " & " : string.Empty)}{buff.Heal}HP/s" : string.Empty)}" +
        $"{(buff.Dispel ? $"{(buff.PowerUp.Short != string.Empty || buff.Heal > 0 ? " & " : string.Empty)}dispel" : string.Empty)} " +
        $"to {picking.MaxTarget} {(picking.SameTeam == Target.Friends ? "allies" : picking.SameTeam == Target.Foes ? "foes" : "unit")} " +
        $"within {radius}m";
    override protected void ApplyEffect(Hitable target, MinionCombat owner)
    {
        if(target is Minion m && !targets.Contains(m))
        {
            targets.Add(m); 
            owner.StartCoroutine(RemoveBlacklist(m));
            m.AddPowerUp(buff, buff.Duration, owner.Owner); 
        }
    } 

    public IEnumerator RemoveBlacklist(Minion victim)
    {
        yield return  new WaitForSeconds(buff.Duration); //maybe not for OneShot and permanent ?
        if (targets.Contains(victim))
            targets.Remove(victim);
    } 
}

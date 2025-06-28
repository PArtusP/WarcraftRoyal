using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "___ - MOD spawn units", menuName = "Unit Modules/Spawn units", order = 4)]
public class SpawnUnitModule : UnitModule
{
    [SerializeField] protected string description = string.Empty;
    [SerializeField] protected float cooldown = 5f;
    [SerializeField] protected float delay = 10f;
    [SerializeField] protected List<UnitWithoutState> spawnList = new List<UnitWithoutState>();

    float lastUsed = 0f;

    public override float Cooldown => cooldown;

    public override float Radius => Mathf.Infinity;

    public override string Description => description != string.Empty ? description : $"Spawn " +
        $"{(spawnList.Count() == 1 ? "a unit" : $"{spawnList.Count()} units")}" +
        $"{(cooldown != 0 ? $" every {cooldown}s" : string.Empty)}" +
        $"{(delay != 0 ? $" after {delay}s" : string.Empty)}";

    public override float Delay => delay;

    public override int Use(MinionCombat owner, int maxTargetOverride = -1)
    {
        if (!owner.IsServer) return 0;
        return UseOnTarget(owner, spawnList);
    }

    public override int UseOnTarget(MinionCombat owner, List<UnitWithoutState> targets)
    {
        var nbSpawned = 0;
        foreach (var h in targets)
        {
            var inst = owner.Owner.Home.SpawnUnit(h, owner.transform.position, owner.transform.rotation, owner.transform.parent, new List<UnitUpgrade>());
            if (OnTargetVfx != null)
            {
                inst.PlayVfx(OnTargetVfx);
                inst.PlayModuleOnTargetVfxClientRpc(ID, owner.NetworkObjectId, OnTargetVfx.id.ToString());
            }
            nbSpawned++;
        }
        if (nbSpawned > 0)
        {
            NextUse = Time.time + cooldown;
            if (OnSelfVfx != null)
            {
                owner.Owner.PlayVfx(OnSelfVfx);
                owner.Owner.PlayModuleOnSelfVfxClientRpc(ID, owner.NetworkObjectId, OnSelfVfx.id.ToString());
            };
        }
        return nbSpawned;
    }
    public override UnitModule Clone()
    {
        var clone = Instantiate(this);
        clone.ID = this.ID;

        // Optional deep clone if TargetPicking is mutable
        clone.spawnList = new List<UnitWithoutState>(spawnList);

        return clone;
    }

    public override void Init(MinionCombat owner)
    {
    }

    public override List<UnitWithoutState> FindTargets(MinionCombat owner)
    {
        // This module does not find targets, it spawns units from a predefined list.
        return spawnList;
    }
}

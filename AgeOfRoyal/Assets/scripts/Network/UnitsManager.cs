using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine; 
 
public class UnitsManager : NetworkBehaviour
{
    List<UnitWithoutState> units = new List<UnitWithoutState>();
    List<UnitWithoutState> deads = new List<UnitWithoutState>();

    public List<UnitWithoutState> Deads => deads;

    public void AddRange(IEnumerable<UnitWithoutState> units)
    { 
      this.units.AddRange(units);
        this.units.ForEach(u => u.OnDieEvent.AddListener(delegate () { StoreAsDead(u); }));
    }
    public void Clean(bool deleteAlive) 
    {
        deads.Where(u => u != null).ToList().ForEach(u => { if (u.IsSpawned) u.NetworkObject.Despawn(); });
        if(deleteAlive) units.Where(u => u != null).ToList().ForEach(u => { if (u.IsSpawned) u.NetworkObject.Despawn(); });
        units.Clear();
    }
    void StoreAsDead(UnitWithoutState unit) 
    {
        if (deads.Contains(unit)) return;
        unit.SetAlive(false);
        unit.SetAliveClientRpc(false);
        units.Remove(unit);
        deads.Add(unit);
    }    
    public void Resurect(UnitWithoutState unit, float healthPercent = 1f, float healthCeiling = 0, float healthFloor = 0)
    {
        UnitWithoutState target = null;
        try
        {
            target = deads.First(u => u == unit);
        }
        catch (Exception e)
        {
            Debug.LogError($"Couldn't find dead unity '{unit.Name} ({unit.NetworkObjectId})' in dead list : [{string.Join(", ", deads.Select(u => $"{unit.Name} ({unit.NetworkObjectId})"))}].");
            throw;
        }
        var res = healthCeiling != 0 ? Mathf.Min(healthCeiling, target.MaxHealth * healthPercent) : target.MaxHealth * healthPercent;
        res = healthFloor != 0 ? Mathf.Max(res, healthFloor) : res;
        target.Health = res;
        Reactivate(target);
        deads.Remove(target);
        units.Add(target);
        ResurectClientRpc(target.NetworkObjectId);
    }
    [ClientRpc]
    private void ResurectClientRpc(ulong networkObjId) => Reactivate(GetNetworkObject(networkObjId).GetComponent< UnitWithoutState>());
    private void Reactivate(UnitWithoutState target)
    {
        target.SetAlive(true);
        target.SetAliveClientRpc(true);
        target.PlayResurectAnimation();
    }
}

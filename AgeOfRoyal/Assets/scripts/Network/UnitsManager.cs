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
        unit.SetDead();
        unit.SetDeadClientRpc();
        units.Remove(unit);
        deads.Add(unit);
    }    
    public void Resurect(UnitWithoutState unit, float healthPercent = 1f, float healthCeiling = 0) 
    { 
        UnitWithoutState target = null;
        try
        {
            target = deads.First(u => u == unit);
        }
        catch(Exception e)
        {
            Debug.LogError($"Couldn't find dead unity '{unit.Name} ({unit.NetworkObjectId})' in dead list : [{string.Join(", ", deads.Select(u => $"{unit.Name} ({unit.NetworkObjectId})"))}].");
            throw;
        }
        target.Health = healthCeiling != 0 ? Mathf.Min(healthCeiling, target.MaxHealth * healthPercent) : target.MaxHealth * healthPercent;
        target.gameObject.SetActive(true);
        deads.Remove(target);
        units.Add(target);
        target.PlayResurectAnimation();
    }    
}

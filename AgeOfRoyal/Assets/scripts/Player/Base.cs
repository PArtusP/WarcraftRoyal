using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Base : Hitable
{
    [SerializeField] List<UnitWithoutState> spawnList;
    [SerializeField] Material material;
    private List<UnitWithoutState> spawnedUnits = new List<UnitWithoutState>();

    public Vector3 direction { get; private set; }
    public UnityEvent EndOfRoundEvent { get; private set; } = new UnityEvent();
    public List<UnitWithoutState> SpawnList { get => spawnList; set => spawnList = value; }
    public override float MaxHealth { get; set; } = 1000;
    public List<UnitWithoutState> SpawnedUnits  => spawnedUnits; 

    // Start is called before the first frame update
    override protected void AwakeInternal()
    {
        home = this;
        var bases = FindObjectsByType<Base>(FindObjectsSortMode.None).ToList();
        bases.Remove(this);

        direction = (bases[0].transform.position - this.transform.position).normalized;
        healthbar.SetMaxHealth(MaxHealth);
        Health = MaxHealth;
    }
    #region Spawn minions
    public void SpawnMinion(List<UnitUpgrade> unitUpgrades)
    {
        float laneSpacing = 1.5f;
        float rowSpacing = 1.5f;

        List<UnitWithoutState> melees = new List<UnitWithoutState>();
        List<UnitWithoutState> mages = new List<UnitWithoutState>();
        List<UnitWithoutState> archers = new List<UnitWithoutState>();

        foreach (var prefab in spawnList)
        {
            if (prefab.Type == Class.Melee) melees.Add(prefab);
            else if (prefab.Type == Class.Mage) mages.Add(prefab);
            else if (prefab.Type == Class.Range) archers.Add(prefab);
        }

        Vector3 basePos = transform.position;

        float zOffset = Mathf.Sign(direction.z) * 3f;
        zOffset += SpawnLine(melees, basePos, zOffset, laneSpacing, unitUpgrades) * -direction.z * rowSpacing;
        zOffset += SpawnLine(mages, basePos, zOffset, laneSpacing, unitUpgrades) * -direction.z * rowSpacing;
        zOffset += SpawnLine(archers, basePos, zOffset, laneSpacing, unitUpgrades) * -direction.z * rowSpacing;

        spawnList.Clear();
    }

    int SpawnLine(List<UnitWithoutState> units, Vector3 basePos, float zOffset, float laneSpacing, List<UnitUpgrade> unitUpgrades)
    {
        const int maxPerRow = 10;
        float rowSpacing = 1.5f;

        int count = units.Count;
        int totalRows = Mathf.CeilToInt(count / (float)maxPerRow);

        for (int i = 0; i < count; i++)
        {
            int row = i / maxPerRow;
            int col = i % maxPerRow;

            int currentRowCount = Mathf.Min(maxPerRow, count - row * maxPerRow);
            float startX = -(laneSpacing * (currentRowCount - 1)) / 2f;

            Vector3 spawnPos = new Vector3(
                basePos.x + startX + col * laneSpacing,
                basePos.y,
                basePos.z + zOffset - direction.z * row * rowSpacing
            );

            SpawnUnit(units[i], spawnPos, Quaternion.LookRotation(direction, Vector3.up), transform);

            UnitWithoutState unit = null;
            if (!units[i].IsAsset)
            {
                unit = units[i];

                unit.transform.position = spawnPos;
                unit.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            }
            else
            {
                unit = Instantiate(units[i], spawnPos, Quaternion.LookRotation(direction, Vector3.up), transform);
                unit.NetworkObject.Spawn();
                Debug.Log($"{unit} is spwaned ? {unit.NetworkObject.IsSpawned}");
                unit.ApplyStatsAndStatus();
                unit.SourcePrefab = units[i];
                ColoredUnit(unit);
                ColoredUnitClientRpc(unit.NetworkObjectId);
                unit.Home = this;
                unit.OnDieEvent.AddListener(delegate { CheckEndRound(unit); });
            }
            var upgrades = unitUpgrades.Where(u => u.Target.Contains(t => t.ID == unit.ID)).ToList();
            var buffs = upgrades.Where(u => !u.Buff.PowerUp.Equals(UnitPowerUp.Identity && u.Buff.Heal != 0 && u.Buff.Dispel != false)).Select(u => u.Buff).ToList();
            if (buffs.Any())
            {
                buffs.ForEach(p => unit.AddBuff(p)); 
                Debug.Log($"Applying power-up to {unit.name}: (Total: {unit.Stats})");
            }

            var modules = upgrades.SelectMany(u => u.Modules).ToList();
            if (modules.Any())
            {
                unit.AddModules(modules);
                Debug.Log($"Adding modules to {unit.name}:  Total: {modules.Count})");
            } 

            var actions = upgrades.SelectMany(u => u.Actions).ToList();
            if (actions.Any())
            {
                unit.AddActions(actions);
                Debug.Log($"Adding actions to {unit.name}:  Total: {actions.Count})");
            } 
            unit.StartFSM();
            unit.name = unit.name + " " + Guid.NewGuid().ToString();
            spawnedUnits.Add(unit);
        }

        return totalRows;
    }
    #endregion

    #region Color unit
    private void ColoredUnit(UnitWithoutState unit) => unit.rendererToColor.ForEach(v => v.renderer.sharedMaterials[v.id] = material);

    [ClientRpc]
    private void ColoredUnitClientRpc(ulong unitId) => ColoredUnit(GetNetworkObject(unitId).GetComponent<UnitWithoutState>());
    #endregion  
     
    #region Minion management
    internal void AddMinion(UnitWithoutState prefab) => spawnList.Add(prefab);
    [ServerRpc(RequireOwnership = false)]
    internal void AddMinionServerRpc(int id) => spawnList.Add(DbResolver.GetMinionById(id));
    [ServerRpc(RequireOwnership = false)]
    internal void RemoveMinionServerRpc(int id) => spawnList.Remove(spawnList.FirstOrDefault(s => s.ID == id));

    internal bool RemoveMinion(UnitWithoutState prefab)
    {
        if (spawnList.Contains(prefab))
        {
            spawnList.Remove(prefab);
            return true;
        }
        return false;
    }

/*    internal void ResetForNextRound()
    {
        if (spawnedUnits.Any())
        {
            if (IsServer)
            {
                Debug.Log($"ResetForNextRound: Stopping {spawnedUnits.Count} mobs");
                spawnedUnits.ForEach(u => u.SetState(MinionState.Stop));
            }
            spawnList.AddRange(spawnedUnits);
        }
        spawnedUnits.Clear();
    }*/
    public void CheckEndRound(UnitWithoutState unit)
    {
        if (unit) spawnedUnits.Remove(unit);
        if (!spawnedUnits.Any()) EndOfRoundEvent.Invoke();
    }
    #endregion
}

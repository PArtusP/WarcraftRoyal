using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Base : Hitable
{
    [SerializeField] List<Minion> spawnList;
    [SerializeField] Material material;
    private List<Minion> spawnedUnits = new List<Minion>();

    public Vector3 direction { get; private set; }
    public UnityEvent EndOfRoundEvent { get; private set; } = new UnityEvent();
    public List<Minion> SpawnList { get => spawnList; set => spawnList = value; }
    protected override float MaxHealth { get; set; } = 1000;
    public List<Minion> SpawnedUnits  => spawnedUnits; 

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
    public void SpawnMinion(Dictionary<int, List<UnitBuff>> minionPowerUps, Dictionary<int, List<UnitModule>> minionModules)
    {
        float laneSpacing = 1.5f;
        float rowSpacing = 1.5f;

        List<Minion> melees = new List<Minion>();
        List<Minion> mages = new List<Minion>();
        List<Minion> archers = new List<Minion>();

        foreach (var prefab in spawnList)
        {
            if (prefab.Type == Class.Melee) melees.Add(prefab);
            else if (prefab.Type == Class.Mage) mages.Add(prefab);
            else if (prefab.Type == Class.Range) archers.Add(prefab);
        }

        Vector3 basePos = transform.position;

        float zOffset = Mathf.Sign(direction.z) * 3f;
        zOffset += SpawnLine(melees, basePos, zOffset, laneSpacing, minionPowerUps, minionModules) * -direction.z * rowSpacing;
        zOffset += SpawnLine(mages, basePos, zOffset, laneSpacing, minionPowerUps, minionModules) * -direction.z * rowSpacing;
        zOffset += SpawnLine(archers, basePos, zOffset, laneSpacing, minionPowerUps, minionModules) * -direction.z * rowSpacing;

        spawnList.Clear();
    }

    int SpawnLine(List<Minion> units, Vector3 basePos, float zOffset, float laneSpacing, Dictionary<int, List<UnitBuff>> minionPowerUps, Dictionary<int, List<UnitModule>> minionModules)
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

            Minion unit = null;
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
                unit.ApplyStatsAndStatus();
                unit.SourcePrefab = units[i];
                ColoredUnit(unit);
                ColoredUnitClientRpc(unit.NetworkObjectId);
                unit.Home = this;
                unit.OnDieEvent.AddListener(delegate { CheckEndRound(unit); });
            }
            if (minionPowerUps.TryGetValue(unit.ID, out List<UnitBuff> powerUp))
            {
                powerUp.ForEach(p => unit.AddPowerUp(p)); 
                Debug.Log($"Applying power-up to {unit.name}: {powerUp} (Total: {unit.Stats})");
            }

            if (minionModules.TryGetValue(unit.ID, out List<UnitModule> modules))
            {
                unit.AddModules(modules);
                Debug.Log($"Adding modules to {unit.name}:  Total: {modules.Count})");
            }

            unit.SetState(MinionState.Walk);
            unit.name = unit.name + " " + Guid.NewGuid().ToString();
            spawnedUnits.Add(unit);
        }

        return totalRows;
    }
    #endregion

    #region Color unit
    private void ColoredUnit(Minion unit) => unit.rendererToColor.ForEach(v => v.renderer.sharedMaterials[v.id] = material);

    [ClientRpc]
    private void ColoredUnitClientRpc(ulong unitId) => ColoredUnit(GetNetworkObject(unitId).GetComponent<Minion>());
    #endregion  


    #region Minion management
    internal void AddMinion(Minion prefab) => spawnList.Add(prefab);
    [ServerRpc(RequireOwnership = false)]
    internal void AddMinionServerRpc(int id) => spawnList.Add(DbResolver.GetMinionById(id));
    [ServerRpc(RequireOwnership = false)]
    internal void RemoveMinionServerRpc(int id) => spawnList.Remove(spawnList.FirstOrDefault(s => s.ID == id));

    internal bool RemoveMinion(Minion prefab)
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
    public void CheckEndRound(Minion unit)
    {
        if (unit) spawnedUnits.Remove(unit);
        if (!spawnedUnits.Any()) EndOfRoundEvent.Invoke();
    }
    #endregion
}

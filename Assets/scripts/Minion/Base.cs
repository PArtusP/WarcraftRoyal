using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;

public class Base : Hitable
{
    [SerializeField] List<Minion> spawnList;
    [SerializeField] Material material;
    private List<Minion> spawnedUnits = new List<Minion>();
    private float health;

    public Vector3 direction { get; private set; }
    public UnityEvent EndOfRoundEvent { get; private set; } = new UnityEvent();
    public override float Health { get => health; set => health = value; }

    // Start is called before the first frame update
    override protected void AwakeInternal()
    {
        home = this;
        var bases = FindObjectsByType<Base>(FindObjectsSortMode.None).ToList();
        bases.Remove(this);

        direction = (bases[0].transform.position - this.transform.position).normalized;
    }
    public void SpawnMinion(Dictionary<Minion, MinionCombatStats> minionPowerUps)
    {
        float laneSpacing = 1.5f;
        float rowSpacing = 1.5f;

        List<Minion> melees = new List<Minion>();
        List<Minion> mages = new List<Minion>();
        List<Minion> archers = new List<Minion>();

        foreach (var prefab in spawnList)
        {
            if (prefab.GetComponent<MeleeCombat>()) melees.Add(prefab);
            else if (prefab.GetComponent<MageCombat>()) mages.Add(prefab);
            else if (prefab.GetComponent<ArcherCombat>()) archers.Add(prefab);
        }

        Vector3 basePos = transform.position;

        float zOffset = Mathf.Sign(direction.z) * 3f;
        zOffset += SpawnLine(melees, basePos, zOffset, laneSpacing, minionPowerUps) * -direction.z * rowSpacing;
        zOffset += SpawnLine(mages, basePos, zOffset, laneSpacing, minionPowerUps) * -direction.z * rowSpacing;
        zOffset += SpawnLine(archers, basePos, zOffset, laneSpacing, minionPowerUps) * -direction.z * rowSpacing;

        spawnList.Clear();
    }

    int SpawnLine(List<Minion> units, Vector3 basePos, float zOffset, float laneSpacing, Dictionary<Minion, MinionCombatStats> minionPowerUps)
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
                unit.ApplyStatsAndStatus();
                unit.SourcePrefab = units[i];
                unit.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial = material;
                unit.Home = this;
                unit.OnDieEvent.AddListener(delegate { CheckEndRound(unit); });
            }
            if (minionPowerUps.TryGetValue(unit.SourcePrefab, out MinionCombatStats powerUp))
            {
                unit.SetPowerUp(powerUp);  
                Debug.Log($"Applying power-up to {unit.name}: {powerUp} (Total: {unit.Stats})");
            }
            else unit.PowerUp = MinionCombatStats.Zero; 

            unit.Target = null;
            spawnedUnits.Add(unit);
        }

        return totalRows;
    }


    public void CheckEndRound(Minion unit)
    {
        if (unit) spawnedUnits.Remove(unit);
        if (!spawnedUnits.Any()) EndOfRoundEvent.Invoke();
    }

    internal void AddMinion(Minion prefab) => spawnList.Add(prefab);

    internal bool RemoveMinion(Minion prefab)
    {
        if (spawnList.Contains(prefab))
        {
            spawnList.Remove(prefab);
            return true;
        }
        return false;
    }

    internal void ResetForNextRound()
    {
        if (spawnedUnits.Any())
        {
            spawnedUnits.ForEach(u => u.SetState(MinionState.Stop));
            spawnList.AddRange(spawnedUnits);
        }
        spawnedUnits.Clear();
    }
}

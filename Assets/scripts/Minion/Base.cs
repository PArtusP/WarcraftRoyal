using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Base : Hitable
{
    [SerializeField] List<Hitable> minionPrefab;
    [SerializeField] Material material;

    public Vector3 direction { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        home = this;
        var bases = FindObjectsByType<Base>(FindObjectsSortMode.None).ToList();
        bases.Remove(this);

        direction = (bases[0].transform.position - this.transform.position).normalized; 

        StartCoroutine(SpawnMinion());
    }
    IEnumerator SpawnMinion()
    {
        yield return new WaitForSeconds(0.5f);

        // Configuration
        float laneSpacing = 1.5f;  // spacing between horizontal lanes (X)
        float roleSpacing = 1.5f;  // spacing between role rows (Z)

        // Organize prefabs by type
        List<Hitable> melees = new List<Hitable>();
        List<Hitable> mages = new List<Hitable>();
        List<Hitable> archers = new List<Hitable>();

        foreach (var prefab in minionPrefab)
        {
            if (prefab.GetComponent<MeleeCombat>()) melees.Add(prefab);
            else if (prefab.GetComponent<MageCombat>()) mages.Add(prefab);
            else if (prefab.GetComponent<ArcherCombat>()) archers.Add(prefab);
        }

        Vector3 basePos = transform.position;

        // Spawn by role
        SpawnLine(melees, basePos, direction.z * roleSpacing, laneSpacing);
        SpawnLine(mages, basePos, 0f, laneSpacing);
        SpawnLine(archers, basePos, -direction.z * roleSpacing, laneSpacing);
    }

    void SpawnLine(List<Hitable> units, Vector3 basePos, float zOffset, float laneSpacing)
    {
        int count = units.Count;
        float startX = -(laneSpacing * (count - 1)) / 2f;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = new Vector3(basePos.x + startX + i * laneSpacing, basePos.y, basePos.z + zOffset);
            var unit = Instantiate(units[i], spawnPos, Quaternion.identity, transform);
            unit.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial = material;
            unit.Home = this;
        }
    }

}

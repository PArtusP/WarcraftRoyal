using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AiPlayer : MonoBehaviour
{
    [SerializeField] private Base home;
    [SerializeField] private List<Minion> meleePrefabs;
    [SerializeField] private List<Minion> magePrefabs;
    [SerializeField] private List<Minion> archerPrefabs;

    [SerializeField] Dictionary<Minion, MinionCombatStats> minionPowerUps = new Dictionary<Minion, MinionCombatStats>();

    public Base Home => home;  

    public void StartNewRound(int value)
    {
        int budget = value;

        // Step 1: Pool for picks
        List<Minion> picks = new List<Minion>();  

        // Combine all pools by type
        var allPrefabs = new List<List<Minion>> { meleePrefabs, archerPrefabs, magePrefabs };

        while (budget >= allPrefabs.SelectMany(ps => ps).Min(p => p.cost))
        {
            bool boughtSomething = false;

            Shuffle(allPrefabs);
            // Try each type randomly (or reorder if you want priority)
            foreach (var pool in allPrefabs)
            {
                // Shuffle pool to add randomness
                var shuffled = new List<Minion>(pool);
                Shuffle(shuffled);

                foreach (var prefab in shuffled)
                {
                    if (prefab.cost <= budget)
                    {
                        budget -= prefab.cost;
                        picks.Add(prefab);
                        home.AddMinion(prefab);
                        boughtSomething = true;
                        break; // buy only one per pass to balance distribution
                    }
                }

                if (boughtSomething)
                    break;
            }

            // No valid unit found => break
            if (!boughtSomething)
                break;
        }

        home.SpawnMinion(minionPowerUps);
    }

    // Fisher-Yates Shuffle
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randIndex = Random.Range(i, list.Count);
            (list[i], list[randIndex]) = (list[randIndex], list[i]);
        }
    }
}

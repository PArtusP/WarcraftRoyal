using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class PlayerExperience
{
    private static readonly float[] thresholds = { 2f, 5f, 10f };
    private static readonly string[] romanLevel = { "I", "II", "III" };

    public static float GetThreshold(int level)
    {
        if (level < 0 || level >= thresholds.Length)
            throw new ArgumentOutOfRangeException(nameof(level), $"No threshold for level {level}");

        return thresholds[level];
    }

    // New method to get cumulative threshold sum up to given level (inclusive)
    private static float GetCumulativeThreshold(int level)
    {
        float sum = 0f;
        for (int i = 0; i <= level; i++)
        {
            sum += GetThreshold(i);
        }
        return sum;
    }

    [SerializeField] private HealthBar healthBar;
    [SerializeField] private TMPro.TextMeshProUGUI currentLevel;
    [SerializeField] private TMPro.TextMeshProUGUI nextLevel;
    [SerializeField] private float currentXp = 0f;
    [SerializeField] private int level = 0;

    public float CurrentXp => currentXp;
    public int Level => level;

    public UnityEvent<int> LevelUpEvent { get; } = new UnityEvent<int>();
    public static int NbLevel => thresholds.Length - 1;

    public HealthBar HealthBar { get => healthBar; set => healthBar = value; }

    public void AddExperience(float value)
    {
        if (level == thresholds.Length - 1) return;

        currentXp += value;
        Debug.Log($"XP, AddExperience : add {value}, level {level}, max {thresholds.Length - 1}");

        // While total XP exceeds cumulative threshold for next level, level up
        while (level < thresholds.Length - 1 && currentXp >= GetCumulativeThreshold(level))
        {
            Debug.Log($"XP, AddExperience : level up '{level + 1}'");
            level++;
            currentLevel.text = romanLevel[level];
            nextLevel.text = level + 1 < thresholds.Length ? romanLevel[level + 1] : string.Empty;
            LevelUpEvent.Invoke(level);

            if (level == thresholds.Length - 1)
            {
                Debug.Log($"XP, AddExperience : max level reached");
                currentXp = GetCumulativeThreshold(level);
            }
        }

        // Calculate XP progress relative to current level
        float prevThreshold = level > 0 ? GetCumulativeThreshold(level - 1) : 0f;
        float levelProgress = currentXp - prevThreshold;
        float levelThreshold = GetThreshold(level);
        healthBar.SetHealth(levelProgress / levelThreshold * 100f);
    }
}

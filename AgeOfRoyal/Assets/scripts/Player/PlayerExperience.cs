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

    [SerializeField] private HealthBar healthBar;
    [SerializeField] private TMPro.TextMeshProUGUI currentLevel;
    [SerializeField] private TMPro.TextMeshProUGUI nextLevel;
    [SerializeField] private float currentXp = 0f;
    [SerializeField] private int level = 0;

    public float CurrentXp => currentXp;
    public int Level => level;

    public UnityEvent<int> LevelUpEvent { get; } = new UnityEvent<int>();
    public static int NbLevel => thresholds.Length - 1;

    public HealthBar HealthBar  => healthBar; 

    public void AddExperience(float value)
    {
        if (level == thresholds.Length - 1) return;
        currentXp += value;
        Debug.Log($"XP, AddExperience : add {value}, level {level}, max {thresholds.Length - 1}");

        while (level < thresholds.Length - 1 && currentXp >= GetThreshold(level))
        {
            Debug.Log($"XP, AddExperience : level up '{level + 1}'");
            level++;
            currentXp -= GetThreshold(level);
            currentLevel.text = romanLevel[level];
            nextLevel.text = level + 1 < thresholds.Length ? romanLevel[level + 1] : string.Empty;
            LevelUpEvent.Invoke(level);

            if (level == thresholds.Length - 1)
            {
                Debug.Log($"XP, AddExperience : max level reached");
                currentXp = GetThreshold(level);
            }
        }
        healthBar.SetHealth(currentXp / GetThreshold(level) * 100f);
    }
}

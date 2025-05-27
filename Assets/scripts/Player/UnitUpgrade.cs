using UnityEngine;

[CreateAssetMenu(fileName = "Unit Upgrade", menuName = "Buttons/Unit Upgrade", order = 1)]
public class UnitUpgrade : ScriptableObject
{
    [SerializeField] MinionCombatStats powerUp;
    [SerializeField] int cost;
    [SerializeField] Minion target;
    [SerializeField] Sprite image;

    public MinionCombatStats PowerUp => powerUp;
    public int Cost => cost;
    public Minion Target => target;
    public string Name => name;

    public Sprite Image => image;
}


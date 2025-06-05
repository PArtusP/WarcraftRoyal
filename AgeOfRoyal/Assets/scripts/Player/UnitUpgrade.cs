using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unit Upgrade", menuName = "Buttons/Unit Upgrade", order = 1)]
public class UnitUpgrade : ScriptableObject
{
    [SerializeField] public int ID = -1;
    [SerializeField] string perkName;
    [SerializeField] MinionCombatStats powerUp = MinionCombatStats.Zero;
    [SerializeField] List<UnitModule> modules;
    [SerializeField] int cost;
    [SerializeField] string description;
    [SerializeField] Minion target;
    [SerializeField] Sprite image;

    public string Name => perkName;
    public MinionCombatStats PowerUp => powerUp;
    public List<UnitModule> Modules => modules;
    public int Cost => cost;
    public string Description => description;
    public Minion Target => target; 
    public Sprite Image => image; 
}


using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unit Upgrade", menuName = "Buttons/Unit Upgrade", order = 1)]
public class UnitUpgrade : ScriptableObject
{
    [SerializeField] public int ID = -1;
    [SerializeField] string perkName; 
    [SerializeField] UnitBuff buff = new UnitBuff();  
    [SerializeField] List<UnitModule> modules;
    [SerializeField] int cost;
    [SerializeField] string description;
    [SerializeField] List<Minion> target;
    [SerializeField] Sprite image;

    public string Name => perkName; 
    public UnitBuff Buff => buff;  
    public List<UnitModule> Modules => modules;
    public int Cost => cost;
    public string Description => description;
    public List<Minion> Target => target; 
    public Sprite Image => image; 
}


using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UpgradeDescription
{
    [SerializeField, TextArea(1, 3)]
    public string description;
    [SerializeField]
    public Sprite icon; 
}


[CreateAssetMenu(fileName = "Unit Upgrade", menuName = "Buttons/Unit Upgrade", order = 1)]
public class UnitUpgrade : ScriptableObject
{
    [Header("Global")]
    [SerializeField] public int ID = -1;
    [SerializeField] string perkName; 
    [SerializeField] int cost;
    [SerializeField] UpgradeDescription descriptionOverride;
    [SerializeField] List<UpgradeDescription> descriptionLines;
    [SerializeField] List<UnitWithoutState> target;
    [SerializeField] Sprite image;
    [Header("Bonuses")]
    [SerializeField] UnitBuff buff = new UnitBuff();  
    [SerializeField] List<UnitModule> modules;
    [SerializeField] List<UnitAction> actions; 

    public string Name => perkName; 
    public UnitBuff Buff => buff;  
    public int Cost => cost;
    public List<UpgradeDescription> Descriptions => descriptionLines;
    public List<UnitWithoutState> Targets => target; 
    public Sprite Image => image; 
    
    public List<UnitModule> Modules => modules;
    public List<UnitAction> Actions => actions; 

    public UpgradeDescription DescriptionOverride { get => descriptionOverride; set => descriptionOverride = value; }
}


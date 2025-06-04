
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class UnitUpgradeButton : RightClickButton
{ 
    [SerializeField] TMPro.TextMeshProUGUI status;  
    [SerializeField] UnitUpgrade upgrade;

    public int ID => upgrade.ID;
    public Minion Target => upgrade.Target;
    public MinionCombatStats PowerUp => upgrade.PowerUp;
    public List<UnitModule> Modules => upgrade.Modules;
    public int Cost => upgrade.Cost; 
    public string Name => upgrade.Name; 
    public bool IsOwned => status.text == "Sold !";

    override public void Buy() => status.text = "Sold !"; 
    override public void Sell() => status.text = "";
    protected override void SetCost() => cost.text = upgrade.Cost.ToString();
    protected override void SetSprite() => Image = upgrade.Image;

    internal void Reset() => status.text = 0.ToString();
}
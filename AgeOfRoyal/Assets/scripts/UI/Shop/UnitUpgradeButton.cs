
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class UnitUpgradeButton : RightClickButton
{ 
    [SerializeField] TMPro.TextMeshProUGUI status;  
    [SerializeField] UnitUpgrade upgrade;
     
    public bool IsOwned => status.text == "Sold !";

    public UnitUpgrade Upgrade { get => upgrade; }

    override public void Buy() => status.text = "Sold !"; 
    override public void Sell() => status.text = "";
    protected override void SetCost() => cost.text = upgrade.Cost.ToString();
    protected override void SetSprite() => Image = upgrade.Image;

    internal void Reset() => status.text = 0.ToString();

    private void OnValidate()
    {
        if (!upgrade) return;
        SetSprite();
        SetCost();
    }
}
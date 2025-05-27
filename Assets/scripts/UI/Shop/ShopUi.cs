using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopUi : MonoBehaviour
{
    Player player;
    [SerializeField] UnitUpgradeDetailUi detailUi;
    [SerializeField] List<UnitButton> unitButtons;
    [SerializeField] List<UnitUpgradeButton> unitUpradeButtons;

    internal void Reset()
    {
        unitButtons.ForEach(b => b.Reset());
    }

    private void Awake()
    {
        player = GetComponentInParent<Player>(); 
        
        foreach (var button in unitButtons)
        {
            button.OnLeftClick = () => {
                bool res = player.TryBuy(button);
                if (res) button.Buy();
            };

            button.OnRightClick = () => {
                bool res = player.TrySell(button);
                if (res) button.Sell();
            };
        }
        
        foreach (var button in unitUpradeButtons)
        {
            button.PointerEnter = () => {
                detailUi.gameObject.SetActive(true);
                detailUi.Display(button);
            };
            button.PointerExit = () => {
                detailUi.gameObject.SetActive(false); 
            };
            button.OnLeftClick = () => {
                bool res = player.TryBuy(button);
                if (res) button.Buy();
            };

            button.OnRightClick = () => {
                bool res = player.TrySell(button);
                if (res) button.Sell();
            };
        }
    }
}

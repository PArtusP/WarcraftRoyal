using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ListWrapper<T>
{
    public List<T> List;
}

public class ShopUi : MonoBehaviour
{
    Player player;
    [SerializeField] UnitUpgradeDetailUi detailUi;
    [SerializeField] List<UnitButton> unitButtons = new List<UnitButton>();
    [SerializeField] List<ListWrapper<UnitUpgradeButton>> unitUpradeButtons = new List<ListWrapper<UnitUpgradeButton>>();

    internal void Reset() => unitButtons.ForEach(b => b.Reset());  

    private void Awake()
    {
        player = GetComponentInParent<Player>();

        foreach (var button in unitButtons)
        {
            button.OnLeftClick = () =>
            {
                bool res = player.TryBuy(button);
                if (res) button.Buy();
            };
            button.OnRightClick = () =>
            {
                bool res = player.TrySell(button);
                if (res) button.Sell();
            };
        }

        foreach (var button in unitUpradeButtons.SelectMany(l => l.List))
        {
            button.PointerEnter = () =>
            {
                detailUi.gameObject.SetActive(true);
                detailUi.Display(button);
            };
            button.PointerExit = () =>
            {
                detailUi.gameObject.SetActive(false);
            };
            button.OnLeftClick = () =>
            {
                bool res = button.Button.interactable && player.TryBuy(button);
                if (res) button.Buy();
            };
            button.OnRightClick = () =>
            {
                bool res = button.Button.interactable && player.TrySell(button);
                if (res) button.Sell();
            };
        }
    }
    public void EnableButtons(int level) => unitUpradeButtons[level].List.ForEach(b => b.Button.interactable = true);
}

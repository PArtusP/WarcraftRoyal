using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[System.Serializable]
public class ListWrapper<T>
{
    public List<T> List;
}

public class ShopUi : MonoBehaviour
{
    Player player;
    [SerializeField] UnitUpgradeDetailUi detailUi;
    [SerializeField] List<ListWrapper<UnitButton>> unitButtons = new List<ListWrapper<UnitButton>>();
    [SerializeField] List<ListWrapper<UnitUpgradeButton>> unitUpradeButtons = new List<ListWrapper<UnitUpgradeButton>>();
    private int lastLevelUnlocked = 0;

    public UnitUpgradeDetailUi DetailUi { get => detailUi; set => detailUi = value; }

    internal void Reset() => unitButtons.SelectMany(b => b.List).ToList().ForEach(b => b.Reset());

    //private void Validate() => SetUp(); 
    private void Awake() => SetUp();

    private void SetUp()
    {
        player = GetComponentInParent<Player>();

        foreach (var button in unitButtons.SelectMany(b => b.List).ToList())
        {
            button.PointerEnter = () =>
            {
                player.MinionPowerUps.TryGetValue(button.Prefab.ID, out var value);
                detailUi.Display(button.Prefab, value == null ?
                    null :
                    value.Select(v => v.PowerUp).Any(v => v != null && !UnitPowerUp.Identity.Equals(v)) ?
                    value.Select(v => v.PowerUp).ToList().SumPowerUps() :
                    null);
            };
            button.PointerExit = () =>
            {
                detailUi.Close();
            };
            button.OnLeftClick = () =>
            {
                if (button.Button.interactable && player.TryBuy(button)) button.Buy();
            };
            button.OnRightClick = () =>
            {
                if (button.Button.interactable && player.TrySell(button)) button.Sell();
            };
        }

        foreach (var button in unitUpradeButtons.SelectMany(l => l.List))
        {
            button.PointerEnter = () =>
            {
                detailUi.Display(button);
            };
            button.PointerExit = () =>
            {
                detailUi.Close();
            };
            button.OnLeftClick = () =>
            {
                if (button.Button.interactable && player.TryBuy(button)) button.Buy();
            };
            button.OnRightClick = () =>
            {
                if (button.Button.interactable && player.TrySell(button)) button.Sell();
            };
        }
    }

    public void EnableNewButtons(int level)
    {
        lastLevelUnlocked = level;
        unitButtons[level].List.ForEach(b => b.Button.interactable = true);
        unitUpradeButtons[level].List.ForEach(b => b.Button.interactable = true);
    }

    internal void EnableButtons(bool value)
    {
        if (value)
            for (int i = 0; i <= lastLevelUnlocked; i++)
            {
                unitButtons[i].List.ForEach(b => b.Button.interactable = true);
                unitUpradeButtons[i].List.ForEach(b => b.Button.interactable = true);
            }
        else
            for (int i = 0; i < unitButtons.Count; i++)
            {
                unitButtons[i].List.ForEach(b => b.Button.interactable = false);
                unitUpradeButtons[i].List.ForEach(b => b.Button.interactable = false);
            }
    }
}

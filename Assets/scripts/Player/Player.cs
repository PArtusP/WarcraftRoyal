using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [SerializeField] Base home;
    [SerializeField] Button startButton;

    [SerializeField] Dictionary<Minion, MinionCombatStats> minionPowerUps = new Dictionary<Minion, MinionCombatStats>();

    PlayerWallet wallet = new PlayerWallet(10);
    PlayerScore walletUi;
    ShopUi shopUi;

    internal PlayerWallet Wallet => wallet;

    public Base Home  => home; 

    private void Awake()
    {
        shopUi = GetComponentInChildren<ShopUi>();
        walletUi = GetComponentInChildren<PlayerScore>();
        wallet.OnChange.AddListener(walletUi.Set);
        walletUi.Set(wallet.Value);

        startButton.onClick.AddListener(delegate
        {
            FindObjectOfType<GameManager>().StartRound();
            ShowPreparationUi(false);
        });
    }

    internal bool TryBuy(RightClickButton prefab)
    {
        switch (prefab)
        {
            case UnitButton unit:
                if (wallet.Spend(unit.Prefab.cost))
                {
                    home.AddMinion(unit.Prefab);
                    return true;
                }
                return false;
            case UnitUpgradeButton unitUpgrade:
                if (wallet.Spend(unitUpgrade.CostValue))
                {
                    AddMinionUpgrade(unitUpgrade.Prefab, unitUpgrade.PowerUp);
                    return true;
                }
                return false;
            default:
                return false;
        }
    }
    internal bool TrySell(RightClickButton prefab)
    {
        switch (prefab)
        {
            case UnitButton unit:
                if (home.RemoveMinion(unit.Prefab))
                {
                    wallet.Earn(unit.Prefab.cost);
                    return true;
                }
                return false;
            case UnitUpgradeButton unitUpgrade:
                if (unitUpgrade.IsOwned)
                {
                    wallet.Earn(unitUpgrade.CostValue);
                    AddMinionUpgrade(unitUpgrade.Prefab, -unitUpgrade.PowerUp);
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    private void AddMinionUpgrade(Minion prefab, MinionCombatStats powerUp)
    {
        if (minionPowerUps.TryGetValue(prefab, out MinionCombatStats existingPowerUp))
            existingPowerUp.Add(powerUp);
        else
        {
            var newPowerUp = MinionCombatStats.Zero + powerUp; 
            minionPowerUps.Add(prefab, newPowerUp);
        }
    }

    internal void ShowPreparationUi(bool v)
    {
        walletUi.gameObject.SetActive(v);
        startButton.gameObject.SetActive(v);
        shopUi.gameObject.SetActive(v);
    }

    internal void StartNewRound()
    {
        Home.SpawnMinion(minionPowerUps);
        Home.CheckEndRound(null);
    }
}

internal class PlayerWallet
{
    private int value;
    public UnityEvent<int> OnChange = new UnityEvent<int>();

    public int Value => value;

    public PlayerWallet(int v)
    {
        this.value = v;
    }

    public bool Spend(int v)
    {
        if (value >= v)
        {
            value -= v;
            OnChange.Invoke(value);
            return true;
        }
        return false;
    }
    public void Earn(int v)
    {
        value += v;
        OnChange.Invoke(value);
    }

}
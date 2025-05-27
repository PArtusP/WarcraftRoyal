using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class Player : MonoBehaviour
{
    [SerializeField] Base home;
    [SerializeField] Button startButton;

    Dictionary<Minion, MinionCombatStats> minionPowerUps = new Dictionary<Minion, MinionCombatStats>();

    [SerializeField] PlayerExperience xp = new PlayerExperience();
    [SerializeField] PlayerStats stats = new PlayerStats();
    PlayerWallet wallet = new PlayerWallet(5);

    PlayerScore walletUi;
    ShopUi shopUi;
    [SerializeField] AnimationCurve levelUpAnimCurve;

    internal PlayerWallet Wallet => wallet;

    public Base Home  => home; 
    public UnityEvent OnReadyEvent { get; } = new UnityEvent(); 

    private void Awake()
    {
        shopUi = GetComponentInChildren<ShopUi>();
        walletUi = GetComponentInChildren<PlayerScore>();
        wallet.OnChange.AddListener(walletUi.Set);
        walletUi.Set(wallet.Value);

        startButton.onClick.AddListener(delegate() { StartCoroutine(WaitToStartRound()); });

        xp.LevelUpEvent.AddListener(shopUi.EnableButtons);
    }

    private IEnumerator WaitToStartRound()
    {
        if(wallet.Value > 0)
        {
            var currXp = xp.CurrentXp;
            var newXp = xp.CurrentXp + wallet.Value;
            var level = xp.Level;
            while(level < PlayerExperience.NbLevel - 1 && newXp >= PlayerExperience.GetThreshold(level))
            {
                newXp -= PlayerExperience.GetThreshold(level);
                level++;
            }

            var time = 0f;
            var xpTransitionTime = Mathf.Min(.5f, (newXp - currXp) * .25f); 
            while (xp.CurrentXp < newXp || xp.Level < level)
            {
                time += Time.deltaTime / xpTransitionTime;

                var t = levelUpAnimCurve.Evaluate(time);
                var v = t * (newXp - currXp) + currXp; 
                xp.AddExperience(v - xp.CurrentXp);
                Debug.Log($"target: {v}, res: {xp.CurrentXp}");
                yield return new WaitForEndOfFrame();
            }
            wallet.Reset();
        }
        yield return new WaitForSeconds(.5f);
        OnReadyEvent.Invoke();
        ShowPreparationUi(false);
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
                if (!unitUpgrade.IsOwned && wallet.Spend(unitUpgrade.Cost))
                {
                    AddMinionUpgrade(unitUpgrade.Target, unitUpgrade.PowerUp);
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
                    wallet.Earn(unitUpgrade.Cost);
                    AddMinionUpgrade(unitUpgrade.Target, -unitUpgrade.PowerUp);
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

    internal void Reset() => Spend(value);
}
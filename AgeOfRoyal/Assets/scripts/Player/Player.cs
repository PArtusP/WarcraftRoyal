using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class PlayerGameInfo
{
    public string name;
    public Guid id;
    public float elo;
    public string gameVersion;
}

public class Player : NetworkBehaviour
{
    [SerializeField] Button startButton;

    [SerializeField] Button xpPlusButton;

    Dictionary<int, List<UnitBuff>> minionPowerUps = new Dictionary<int, List<UnitBuff>>();
    Dictionary<int, List<UnitModule>> minionModules = new Dictionary<int, List<UnitModule>>();

    [SerializeField] PlayerExperience xp = new PlayerExperience();
    [SerializeField] PlayerStats stats = new PlayerStats();
    PlayerWallet wallet = new PlayerWallet(0);

    PlayerScore walletUi;
    ShopUi shopUi;
    [SerializeField] AnimationCurve levelUpAnimCurve;
    [SerializeField] internal Sprite selectedSprite;
    [SerializeField] internal Sprite iconSprite;
    private List<UnitUpgrade> upgrades = new List<UnitUpgrade>();
    private bool waitToSpendXp;

    public NetworkVariable<bool> IsReadyForBattle { get; } = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    internal PlayerWallet Wallet => wallet;

    public Base Home { get; set; }
    public UnityEvent OnReadyEvent { get; } = new UnityEvent();
    public UnityEvent OnDieEvent { get; } = new UnityEvent();
    public Dictionary<int, List<UnitBuff>> MinionPowerUps => minionPowerUps;
    public Dictionary<int, List<UnitModule>> MinionModules => minionModules;

    public ShopUi ShopUi => shopUi;

    public PlayerExperience Xp { get => xp; set => xp = value; }

    #region Init & Awake
    private void Awake()
    {
        shopUi = GetComponentInChildren<ShopUi>();
        walletUi = GetComponentInChildren<PlayerScore>();
        ShowPreparationUi(false);

        xpPlusButton.onClick.AddListener(TryAddXp);
        xpPlusButton.interactable = false;
        startButton.interactable = false;
        shopUi.EnableButtons(false);
    }

    [ClientRpc]
    public void InitPlayerClientRpc(ulong objId)
    {
        Debug.Log("Player, SetHomeClientRpc: " + objId);
        Home = GetNetworkObject(objId).GetComponent<Base>();
        if (IsOwner)
        {
            Home.OnDieEvent.AddListener(OnDieEvent.Invoke);
            wallet.OnChange.AddListener(walletUi.Set);
            walletUi.Set(wallet.Value);

            startButton.onClick.AddListener(WaitToStartRound);

            xp.LevelUpEvent.AddListener(LevelUpSync);
            ShowPreparationUi(true);
        }
    }

    #endregion

    #region Game loop
    internal void StartPreparationPhase(int earnings)
    {
        Wallet.Earn(earnings);
        shopUi.Reset();
        ShowPreparationUi(true);
        shopUi.EnableButtons(true);
        xpPlusButton.interactable = true;
        startButton.interactable = true;
        IsReadyForBattle.Value = false;
    }
    internal void WaitToStartRound()
    {
        xpPlusButton.interactable = false;
        shopUi.EnableButtons(false);
        startButton.interactable = false;
        IsReadyForBattle.Value = true;
    }

    internal void StartNewCombatRound()
    {
        Home.SpawnMinion(minionPowerUps, minionModules);
        Home.CheckEndRound(null);
        StartNewCombatRoundClientRpc();
    }
    [ClientRpc]
    private void StartNewCombatRoundClientRpc()
    {
        ShowPreparationUi(false);
    }
#endregion

    #region Shop methods
    internal bool TryBuy(RightClickButton prefab)
    {
        switch (prefab)
        {
            case UnitButton unit:
                if (wallet.Spend(unit.Prefab.cost))
                {
                    Home.AddMinion(unit.Prefab);
                    Home.AddMinionServerRpc(unit.Prefab.ID);
                    return true;
                }
                return false;
            case UnitUpgradeButton unitUpgrade:
                if (!unitUpgrade.IsOwned && wallet.Spend(unitUpgrade.Cost))
                {
                    upgrades.Add(DbResolver.GetUpgradeById(unitUpgrade.ID));
                    unitUpgrade.Target.ForEach(t =>
                    {
                        AddMinionModules(t.ID, unitUpgrade.Modules);// @TOOO : Better to do ask and approve
                        AddMinionPowerUp(t.ID, unitUpgrade.Buff);// @TOOO : Better to do ask and approve
                    });
                    AddMinionUpgradeServerRpc(unitUpgrade.ID);
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
                if (Home.RemoveMinion(unit.Prefab))
                {
                    Home.RemoveMinionServerRpc(unit.Prefab.ID);
                    wallet.Earn(unit.Prefab.cost);
                    return true;
                }
                return false;
            case UnitUpgradeButton unitUpgrade:
                if (unitUpgrade.IsOwned)
                {
                    wallet.Earn(unitUpgrade.Cost);
                    upgrades.Remove(DbResolver.GetUpgradeById(unitUpgrade.ID));
                    unitUpgrade.Target.ForEach(t =>
                    {
                        RemoveMinionModules(t.ID, unitUpgrade.Modules);// @TOOO : Better to do ask and approve
                        RemoveMinionPowerUp(t.ID, unitUpgrade.Buff);// @TOOO : Better to do ask and approve
                    });
                    RemoveMinionUpgradeServerRpc(unitUpgrade.ID);
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    private void AddMinionPowerUp(int prefabID, UnitBuff powerUp)
    {
        if (minionPowerUps.TryGetValue(prefabID, out List<UnitBuff> existingPowerUp))
            existingPowerUp.Add(powerUp);
        else
            minionPowerUps.Add(prefabID, new List<UnitBuff>() { powerUp });
    }
    private void RemoveMinionPowerUp(int prefabID, UnitBuff powerUp)
    {
        if (minionPowerUps.TryGetValue(prefabID, out List<UnitBuff> existingPowerUp))
            existingPowerUp.Remove(powerUp);
    }
    private void AddMinionModules(int prefabID, List<UnitModule> modules)
    {
        if (minionModules.TryGetValue(prefabID, out List<UnitModule> existingModules))
            existingModules.AddRange(modules);
        else
            minionModules.Add(prefabID, new List<UnitModule>(modules));
    }
    private void RemoveMinionModules(int prefabID, List<UnitModule> modules)
    {
        modules.ForEach(m =>
        {
            if (minionModules.TryGetValue(prefabID, out List<UnitModule> existingModules))
                existingModules.Remove(m);
        });
    }

    [ServerRpc]
    private void AddMinionUpgradeServerRpc(int iD)
    {
        var upgrade = DbResolver.GetUpgradeById(iD);
        upgrades.Add(upgrade);
        upgrade.Target.ForEach(t =>
        {
            AddMinionModules(t.ID, upgrade.Modules);
            AddMinionPowerUp(t.ID, upgrade.Buff);
        });
    }
    [ServerRpc]
    private void RemoveMinionUpgradeServerRpc(int iD)
    {
        var upgrade = DbResolver.GetUpgradeById(iD);
        upgrades.Remove(upgrade);
        upgrade.Target.ForEach(t =>
        {
            RemoveMinionModules(t.ID, upgrade.Modules);
            RemoveMinionPowerUp(t.ID, upgrade.Buff);
        });
    }


    internal void ShowPreparationUi(bool v)
    {
        xp.HealthBar.transform.parent.parent.gameObject.SetActive(v); // @WHACK
        walletUi.gameObject.SetActive(v);
        startButton.gameObject.SetActive(v);
        shopUi.gameObject.SetActive(v);
    }
    #endregion

    #region XP

    private void TryAddXp()
    {
        if (!waitToSpendXp && wallet.Value > 0 && xp.Level < PlayerExperience.NbLevel)
        {
            wallet.Spend(1);
            waitToSpendXp = true;
            StartCoroutine(AddXp(1f * stats.moneyToExperienceMultiplier));
        }
    }
    private IEnumerator AddXp(float value)
    {
        float currXp = xp.CurrentXp;
        float targetXp = currXp + value;

        float xpTransitionTime = Mathf.Min(0.5f, (targetXp - currXp) * 0.25f);
        float elapsed = 0f;

        while (xp.CurrentXp < targetXp)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / xpTransitionTime);
            float curveT = levelUpAnimCurve.Evaluate(t);

            float interpolatedXp = Mathf.Lerp(currXp, targetXp, curveT);
            float xpToAdd = interpolatedXp - xp.CurrentXp;

            if (xpToAdd > 0f)
            {
                xp.AddExperience(xpToAdd);
            }

            Debug.Log($"target xp: {interpolatedXp}, xp: {xp.CurrentXp}, level: {xp.Level}");
            yield return new WaitForEndOfFrame();
        }
        waitToSpendXp = false;
    }
    private void LevelUpSync(int level)
    {
        shopUi.EnableNewButtons(level);
        LevelUpSyncServerRpc(level);
    }

    [ServerRpc]
    private void LevelUpSyncServerRpc(int level)
    {
        LevelUpSyncClientRpc(level);
        if (IsHost) return;
        xp.SetLevel(level);
    }

    [ClientRpc]
    private void LevelUpSyncClientRpc(int level)
    {
        if (IsOwner) return;
        xp.SetLevel(level);
    }
    #endregion
}

internal class PlayerWallet
{
    private int value = 0;
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
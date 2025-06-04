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

    Dictionary<int, MinionCombatStats> minionPowerUps = new Dictionary<int, MinionCombatStats>();
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

    public NetworkVariable<bool> IsReadyForBattle { get; } = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    internal PlayerWallet Wallet => wallet;

    public Base Home { get; set; }
    public UnityEvent OnReadyEvent { get; } = new UnityEvent();
    public UnityEvent OnDieEvent { get; } = new UnityEvent();
    public Dictionary<int, MinionCombatStats> MinionPowerUps => minionPowerUps;
    public Dictionary<int, List<UnitModule>> MinionModules => minionModules;

    #region Init & Awake
    private void Awake()
    {
        shopUi = GetComponentInChildren<ShopUi>();
        walletUi = GetComponentInChildren<PlayerScore>();
        ShowPreparationUi(false);
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

            startButton.onClick.AddListener(delegate () { StartCoroutine(WaitToStartRound()); });

            xp.LevelUpEvent.AddListener(shopUi.EnableButtons);
            ShowPreparationUi(true);
        }
    }
    #endregion

    internal void StartPreparationPhase(int earnings)
    {
        Home.ResetForNextRound();
        Wallet.Earn(earnings);
        shopUi.Reset();
        ShowPreparationUi(true);
        IsReadyForBattle.Value = false;
    }
    internal IEnumerator WaitToStartRound()
    {
        if (wallet.Value > 0 && xp.Level < PlayerExperience.NbLevel)
        {
            Debug.Log("Player, WaitToStartRound: reset waller");
            var currXp = xp.CurrentXp;
            var newXp = xp.CurrentXp + wallet.Value;
            var xpTransitionTime = Mathf.Min(.5f, (newXp - currXp) * .25f);

            var level = xp.Level;
            while (level < PlayerExperience.NbLevel && newXp >= PlayerExperience.GetThreshold(level))
            {
                newXp -= PlayerExperience.GetThreshold(level);
                level++;
            }

            var time = 0f;
            while (xp.CurrentXp < newXp || xp.Level < level)
            {
                time += Time.deltaTime / xpTransitionTime;

                var t = levelUpAnimCurve.Evaluate(Mathf.Clamp(time, 0f, 1f));
                var v = t * (newXp - currXp) + currXp;
                xp.AddExperience(v - xp.CurrentXp);
                Debug.Log($"target xp: {v}, xp: {xp.CurrentXp}, target level: {level}, level: {xp.Level}");
                yield return new WaitForEndOfFrame();
            }
            wallet.Reset();
        }
        yield return new WaitForSeconds(.5f);
        ShowPreparationUi(false);

        IsReadyForBattle.Value = true;
        //SendUnitToServerRpc(JsonUtility.ToJson(Home.SpawnList.Select(m => m.Serialized)), JsonUtility.ToJson(minionPowerUps)); 
    }
    internal void StartNewCombatRound()
    {
        Home.SpawnMinion(minionPowerUps, minionModules);
        Home.CheckEndRound(null);
    }

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
                    AddMinionPowerUp(unitUpgrade.Target.ID, unitUpgrade.PowerUp);
                    AddMinionModules(unitUpgrade.Target.ID, unitUpgrade.Modules);
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
                    AddMinionPowerUp(unitUpgrade.Target.ID, -unitUpgrade.PowerUp);  // @TOOO : Better to do ask and approve
                    RemoveMinionModules(unitUpgrade.Target.ID, unitUpgrade.Modules);  // @TOOO : Better to do ask and approve
                    RemoveMinionUpgradeServerRpc(unitUpgrade.ID);
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    private void AddMinionPowerUp(int prefabID, MinionCombatStats powerUp)
    {
        if (minionPowerUps.TryGetValue(prefabID, out MinionCombatStats existingPowerUp))
            existingPowerUp.Add(powerUp);
        else
        {
            var newPowerUp = MinionCombatStats.Zero + powerUp;
            minionPowerUps.Add(prefabID, newPowerUp);
        }
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
        AddMinionModules(upgrade.Target.ID, upgrade.Modules);
        AddMinionPowerUp(upgrade.Target.ID, upgrade.PowerUp);
    }
    [ServerRpc]
    private void RemoveMinionUpgradeServerRpc(int iD)
    {
        var upgrade = DbResolver.GetUpgradeById(iD);
        upgrades.Remove(upgrade);
        RemoveMinionModules(upgrade.Target.ID, upgrade.Modules);
        AddMinionPowerUp(upgrade.Target.ID, -upgrade.PowerUp);
    }


    internal void ShowPreparationUi(bool v)
    {
        xp.HealthBar.transform.parent.gameObject.SetActive(v);
        walletUi.gameObject.SetActive(v);
        startButton.gameObject.SetActive(v);
        shopUi.gameObject.SetActive(v);
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
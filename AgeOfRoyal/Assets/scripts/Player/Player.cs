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

    [SerializeField] PlayerExperience xp = new PlayerExperience();
    [SerializeField] PlayerStats stats = new PlayerStats();
    PlayerInterest interest;
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

    public ShopUi ShopUi => shopUi;

    public PlayerExperience Xp { get => xp; set => xp = value; }
    public PlayerInterest Interest => interest;

    public List<UnitUpgrade> Upgrades { get => upgrades; set => upgrades = value; }

    #region Init & Awake
    private void Awake()
    {
        shopUi = GetComponentInChildren<ShopUi>();
        walletUi = GetComponentInChildren<PlayerScore>();
        interest = GetComponent<PlayerInterest>();
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
            //Home.OnDieEvent.AddListener(OnDieEvent.Invoke);  @TODO we need to trigger when the player lost
            wallet.OnChange.AddListener(walletUi.Set);
            wallet.OnChange.AddListener(SyncWalletServerRpc);
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
        Home.SpawnMinion(upgrades);
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
                if (!unitUpgrade.IsOwned && wallet.Spend(unitUpgrade.Upgrade.Cost))
                {
                    upgrades.Add(DbResolver.GetUpgradeById(unitUpgrade.Upgrade.ID)); 
                    AddMinionUpgradeServerRpc(unitUpgrade.Upgrade.ID);
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
                    wallet.Earn(unitUpgrade.Upgrade.Cost);
                    upgrades.Remove(DbResolver.GetUpgradeById(unitUpgrade.Upgrade.ID)); 
                    RemoveMinionUpgradeServerRpc(unitUpgrade.Upgrade.ID);
                    return true;
                }
                return false;
            default:
                return false;
        }
    } 

    [ServerRpc]
    private void AddMinionUpgradeServerRpc(int iD)
    {
        var upgrade = DbResolver.GetUpgradeById(iD);
        upgrades.Add(upgrade); 
    }

    [ServerRpc]
    private void RemoveMinionUpgradeServerRpc(int iD)
    {
        var upgrade = DbResolver.GetUpgradeById(iD);
        upgrades.Remove(upgrade); 
    }


    internal void ShowPreparationUi(bool v)
    {
        xp.HealthBar.transform.parent.parent.gameObject.SetActive(v); // @WHACK
        walletUi.gameObject.SetActive(v);
        startButton.gameObject.SetActive(v);
        shopUi.gameObject.SetActive(v);
    }
    [ServerRpc]
    private void SyncWalletServerRpc(int value) => Wallet.Set(value);

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
        shopUi.EnableNextLevel(level);
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
    internal void Set(int v) => value = v;
}

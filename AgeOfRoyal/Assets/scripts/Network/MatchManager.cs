using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;


public enum Phase
{
    Preparation,
    Combat,
}
public enum RewardType
{
    KeepSurvivor,
    EarnHalfValue,
}
public class MatchManager : NetworkBehaviour
{
    private const int preparationTime = 20;
    [Header("UI")]
    [SerializeField] Canvas restartCanvas;
    [SerializeField] RewardType rewardType = RewardType.EarnHalfValue;
    [SerializeField] CountDownUi countDownUi;
    
    UnitManager unitManager; 
    int roundCount = 0;
    Phase phase = Phase.Preparation;
    PlayerManager playerManager;
    
    private void Awake()
    {
        unitManager = GetComponent<UnitManager>();
        playerManager = GetComponent<PlayerManager>();
    }

    private void Update()
    {
        if (UnitUpgradeDetailUi.Instance == null) return;
        if (UnitUpgradeDetailUi.Instance.gameObject.activeSelf
            && Mouse.current.rightButton.wasPressedThisFrame)
            UnitUpgradeDetailUi.Instance.Close();
    }


    #region Set game up 
    public void AskSetUpGame()
    {
        StartCoroutine(SetUpGame());
    }

    IEnumerator SetUpGame()
    {
        yield return new WaitForSeconds(2f);

        var players = playerManager.Players;
        for (int i = 0; i < players.Count; i++)
        {
            Debug.Log("SetUpGame : i = " + i);
            var player = players[i];
            player.OnDieEvent.AddListener(delegate { ProcessDeath(player.NetworkObjectId); });
            player.Home.EndOfRoundEvent.AddListener(EndOfCombatRound);
            player.IsReadyForBattle.OnValueChanged += MarkPlayerReady(player);
            SetUpPlayerClientRpc(i, players[i].NetworkObjectId);
        }

        SetUi();
        SetUpUiClientRpc();

        yield return new WaitForSeconds(1f);
        StartCoroutine(PreparationPhase());
    }

    private NetworkVariable<bool>.OnValueChangedDelegate MarkPlayerReady(Player player) =>
        new NetworkVariable<bool>.OnValueChangedDelegate((previousValue, newValue) =>
        {
            if (newValue)
            {
                if (!playerManager.Players.Any(p => !p.IsReadyForBattle.Value))
                    CombatPhase();
            }
        });


    #endregion

    #region Set up UI
    [ClientRpc]
    private void SetUpUiClientRpc()
    {
        SetUi();
    }

    [ClientRpc]
    private void SetUpPlayerClientRpc(int nb, ulong networkObjectId, bool isTraining = false)
    {
        if (!IsServer)
        {
            if (playerManager.Players.Count <= nb)
                while (playerManager.Players.Count <= nb)
                    playerManager.Players.Add(null);
            var player = GetNetworkObject(networkObjectId).GetComponent<Player>();

            playerManager.Players[nb] = player;
            if (player.IsOwner)
            {
                player.IsReadyForBattle.OnValueChanged += StopCountDown;
                countDownUi.EndCountDownEvent.AddListener(delegate () { player.WaitToStartRound(); });
            }
        }
    }

    private void StopCountDown(bool previousValue, bool newValue)
    {
        if (newValue) countDownUi.StopCountDown();
    }

    private void SetUiServer()
    {
        for (int i = 0; i < playerManager.Players.Count; i++)
        {
            // If nedded.. it's here bro
        }
    }
    private void SetUiClient()
    {
        Debug.Log("Start SetUiClient");
        foreach (var pl in playerManager.Players)
        {
            if (pl.IsOwner)
            {
                Debug.Log("SetUiClient : is owner");
                // If nedded.. it's here bro
            }
            //else pl.SetHealthBar(healthbars[1]);
        }
    }

    private void SetUi()
    {
        if (IsServer && !IsHost)
            SetUiServer();
        else
            SetUiClient();
    }

    #endregion

    #region Game loop
    #region Game loop - Preparation phase
    private IEnumerator PreparationPhase()
    {
        yield return new WaitForSeconds(1f);
        var despawnSurvivors = true;
        playerManager.Players.ForEach(p =>
        {
            var moneyReward = 5 + roundCount * 2;
            switch (rewardType)
            {
                case RewardType.KeepSurvivor: // Stop survivors, respawn them next time
                    if (p.Home.SpawnedUnits.Any())
                    {
                        p.Home.SpawnedUnits.ForEach(u => u.SetState(MinionState.Stop));
                        p.Home.SpawnList.AddRange(p.Home.SpawnedUnits);
                        p.Home.SpawnedUnits.Clear();
                        despawnSurvivors = false;
                    }
                    break;
                case RewardType.EarnHalfValue: // Despawn survivors, earn half the total value of the survivors
                    if (p.Home.SpawnedUnits.Any())
                    {
                        p.Home.SpawnedUnits.ForEach(u => u.NetworkObject.Despawn());
                        moneyReward += p.Home.SpawnedUnits.Sum(u => Mathf.Min(1, Mathf.FloorToInt(u.cost * .5f)));
                        p.Home.SpawnedUnits.Clear();
                    }
                    break;
                default:
                    break;
            }

            PreparationPhase_ResetPlayerClientRpc(p.NetworkObjectId, moneyReward);
        });
        unitManager.Clean(despawnSurvivors);
        countDownUi.StartCountDown(preparationTime);
        roundCount++;
    }
    [ClientRpc]
    private void PreparationPhase_ResetPlayerClientRpc(ulong playerObjectId, int moneyReward)
    {
        var player = GetNetworkObject(playerObjectId).GetComponent<Player>();
        if (!player.IsOwner) return;

        countDownUi.StartCountDown(preparationTime);
        player.StartPreparationPhase(moneyReward);
        roundCount++;
    }
    #endregion

    #region Game loop - Combat phase
    private void CombatPhase()
    {
        if (phase == Phase.Combat) return;
        phase = Phase.Combat;
        playerManager.Players.ForEach(p => p.StartNewCombatRound());
        playerManager.Players.ForEach(p => unitManager.AddRange(p.Home.SpawnedUnits));
    }

    private void EndOfCombatRound()
    {
        if (phase == Phase.Preparation) return;
        phase = Phase.Preparation;
        StartCoroutine(PreparationPhase());
    }

    #endregion

    #endregion

    #region Death
    private void ProcessDeath(ulong objectId)
    {
        var nb = -1;
        for (var i = 0; i < playerManager.Players.Count; i++)
        {
            if (playerManager.Players[i].NetworkObjectId == objectId)
                nb = i;
        }
        Debug.Log("SetUpGame : ProcessDeath : i = " + nb);

        if (nb == -1) return;
        playerManager.Lifestocks[nb]--;

        if (playerManager.Lifestocks[nb] == 0)
            GameOver(nb);
        else
            StartCoroutine(PreparationPhase());

        playerManager.SubmitLifeLostClientRpc(nb, playerManager.Lifestocks[nb]);
    }


    #endregion

    #region Gameover & restart

    private void GameOver(int loserNb)
    {
        var winnerId = loserNb == 0 ? 1 : 0;

        _ = FindFirstObjectByType<ConnectionManager>().ShutdownServer();
    }
    #endregion
}

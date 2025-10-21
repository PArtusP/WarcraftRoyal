using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;


public enum Phase
{
    Preparation,
    Combat,
}
public class MatchManager : NetworkBehaviour
{
    private const int preparationTime = 20;
    [Header("UI")]
    [SerializeField] Canvas restartCanvas;
    [SerializeField] CountDownUi countDownUi;
    ShopUi shopUi;
    int roundCount = 0;
    Phase phase = Phase.Preparation;

    [Header("Players")]
    [SerializeField] PlayerManager playerManager;

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
                countDownUi.EndCountDownEvent.AddListener(delegate () { EndPreparationCooldown(player); });
        }
    }

    private void SetUiServer()
    {
        for (int i = 0; i < playerManager.Players.Count; i++)
        {
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
                //pl.SetHealthBar(healthbars[0]);
                //pl.SetSpecialBar(specialBar);
                //pl.SetMoveActionBar(moveActionBar);
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

    private void EndOfCombatRound()
    {
        if (phase == Phase.Preparation) return;
        phase = Phase.Preparation;
        StartCoroutine(PreparationPhase());
    }
    #region Game loop - Preparation phase
    private IEnumerator PreparationPhase()
    {
        yield return new WaitForSeconds(1f);
        playerManager.Players.ForEach(p => p.Home.ResetForNextRound());
        PreparationPhase_ResetPlayerClientRpc();
        StartCoroutine(countDownUi.CountDown(preparationTime));
        roundCount++;
    }
    [ClientRpc]
    private void PreparationPhase_ResetPlayerClientRpc()
    {
        StartCoroutine(countDownUi.CountDown(preparationTime));
        playerManager.Players.ForEach(p =>
        {
            if (p.IsOwner)
            {
                p.StartPreparationPhase(5 + roundCount * 2);
                roundCount++;
            }
        });
    }

    private void EndPreparationCooldown(Player p) => StartCoroutine(p.WaitToStartRound());
    #endregion

    #region Game loop - Combat phase
    private void CombatPhase()
    {
        if (phase == Phase.Combat) return;
        phase = Phase.Combat;
        playerManager.Players.ForEach(p => p.StartNewCombatRound());
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

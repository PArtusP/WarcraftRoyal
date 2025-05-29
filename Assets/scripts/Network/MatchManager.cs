using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class MatchManager : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] Canvas lifeStockCanvas;
    [SerializeField] Canvas restartCanvas;
    [SerializeField] CountDownUi countDownUi;

    [Header("Players")]
    [SerializeField] PlayerManager playerManager;
    [Header("Settings")]
    [SerializeField] bool isTraining = false;

    public List<Player> Players { get => playerManager.Players; }
    public bool IsTraining { get => isTraining; set => isTraining = value; }

    #region Set game up
    /*private void Awake()
    {
        playerManager.Ready.AddListener(delegate { AskSetUpGameServerRpc(); });
    }*/

    //[ServerRpc(RequireOwnership = false)]
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
            SetUpPlayerClientRpc(i, players[i].NetworkObjectId, isTraining);
        }

        SetUi();
        SetUpUiClientRpc();

        yield return new WaitForSeconds(1f);
        StartPreparationPhase();
    }

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

            playerManager.Players[nb] = GetNetworkObject(networkObjectId).GetComponent<Player>();
        }
    }

    private void SetUiServer()
    {
        lifeStockCanvas.gameObject.SetActive(true);
        for (int i = 0; i < playerManager.Players.Count; i++)
        {
            //playerManager.Players[i].SetHealthBar(healthbars[i]);
        }
    }
    private void SetUiClient()
    {
        Debug.Log("Start SetUiClient");
        lifeStockCanvas.gameObject.SetActive(true);
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
    #region Countdown 
    [ClientRpc]
    private void StartCountdownClientRpc()
    {
        StartCoroutine(CountDown());
    }
    private IEnumerator CountDown()
    {
        countDownUi.gameObject.SetActive(true);

        countDownUi.SetCount(3);
        yield return new WaitForSeconds(1f);
        countDownUi.SetCount(2);
        yield return new WaitForSeconds(1f);
        countDownUi.SetCount(1);
        yield return new WaitForSeconds(1f);
        countDownUi.SetCount(0);
        yield return new WaitForSeconds(1f);

        countDownUi.gameObject.SetActive(false);
    }
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
            StartPreparationPhase();

        playerManager.SubmitLifeLostClientRpc(nb, playerManager.Lifestocks[nb]);
    }


    #endregion
    #region Gameover & restart

    private void GameOver(int loserNb)
    {
        var winnerId = loserNb == 0 ? 1 : 0;

        _ = FindFirstObjectByType<ConnectionManager>().ShutdownServer();
    }
    private void StartPreparationPhase()
    {
        throw new NotImplementedException();
    }
    #endregion
}

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{

    [Header("Player")]
    [SerializeField] List<Player> players = new List<Player>();

    [Header("UI - Life stock")]
    [SerializeField] int[] lifestocks = new int[2];
    [SerializeField] LifestockUi[] lifestockUis = new LifestockUi[2];

    public List<Player> Players { get => players; set => players = value; }
    public int[] Lifestocks { get => lifestocks; set => lifestocks = value; }


    [SerializeField] private int MaxPlayer = 1;
    [SerializeField] private MatchManager matchManager;

    public void AddPlayer(Player player)
    {
        if (players.Count >= MaxPlayer) return;

        players.Add(player);

        if (players.Count == MaxPlayer)
            matchManager.AskSetUpGame();
    }

    public void RestarWholeGame()
    {
        SubmitResetLifeClientRpc();
        ResetLifeStocks();
    }


    [ClientRpc]
    public void SubmitLifeLostClientRpc(int i, int value)
    {
        if (players[i].IsOwner)
        {
            lifestocks[i] = value;
            lifestockUis[0].SetLifeLeft(lifestocks[i]);
        }
        else
        {
            lifestocks[i] = value;
            lifestockUis[1].SetLifeLeft(lifestocks[i]);
        }
    }

    [ClientRpc]
    private void SubmitResetLifeClientRpc()
    {
        ResetLifeStocks();
    }

    private void ResetLifeStocks()
    {
        for (int i = 0; i < players.Count; i++)
        {
            lifestocks[i] = 5;
            if (players[i].IsOwner)
            {
                lifestockUis[0].SetLifeLeft(5);
            }
            else
            {
                lifestockUis[1].SetLifeLeft(5);
            }
        }
    }

}

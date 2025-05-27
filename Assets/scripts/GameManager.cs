using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum Phase
{
    Preparation,
    Combat,
}

public class GameManager : MonoBehaviour
{
    List<Player> players;
    List<AiPlayer> ais;
    int roundCount = 1;
    Phase phase = Phase.Preparation;
    ShopUi shopUi;

    private void Awake()
    {
        players = FindObjectsOfType<Player>().ToList();
        ais = FindObjectsOfType<AiPlayer>().ToList();
        shopUi = FindObjectOfType<ShopUi>();
        players.ForEach(p => p.Home.EndOfRoundEvent.AddListener(EndOfRound));
        ais.ForEach(p => p.Home.EndOfRoundEvent.AddListener(EndOfRound));
    }

    private void EndOfRound()
    {
        if (phase == Phase.Preparation) return;
        phase = Phase.Preparation;
        StartCoroutine(WaitToStartPrepa());
    }

    private IEnumerator WaitToStartPrepa()
    {
        yield return new WaitForSeconds(.2f);
        players.ForEach(p =>
        {
            p.Home.ResetForNextRound();
            p.Wallet.Earn(5 + roundCount * 2);
            p.ShowPreparationUi(true);
        }); 
        ais.ForEach(p =>
        {
            p.Home.ResetForNextRound();
        }); 
        roundCount++;
    }

    public void StartRound()
    {
        if (phase == Phase.Combat) return;
        phase = Phase.Combat;
        ais.ForEach(a => a.StartNewRound(roundCount == 1 ? 10 : 5 + roundCount * 2));
        players.ForEach(p => p.StartNewRound());
        shopUi.Reset();

    }
}
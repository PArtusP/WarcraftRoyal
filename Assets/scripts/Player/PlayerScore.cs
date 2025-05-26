using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScore : MonoBehaviour
{
    int score = 0;
    [SerializeField] TMPro.TextMeshProUGUI scoreBoard;

    public int Score { get => score; }

    internal void Add(int bonus)
    {
        score += bonus;
        scoreBoard.text = score.ToString();
    }
    internal void Set(int score)
    {
        this.score = score;
        scoreBoard.text = score.ToString();
    }

    internal void Reset()
    {
        score = 0;
        scoreBoard.text = "0";
    }
}

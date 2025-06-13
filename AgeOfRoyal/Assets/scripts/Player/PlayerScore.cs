using System.Collections;
using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    int score = 0;
    [SerializeField] TMPro.TextMeshProUGUI scoreBoard;
    [SerializeField] Animator animation; 
    [SerializeField] RectTransform animContainer; 
    [SerializeField] Color addColor; 
    [SerializeField] Color removeColor; 

    public int Score { get => score; }

    internal void Add(int bonus)
    {
        score += bonus;
        scoreBoard.text = score.ToString();
    }
    internal void Set(int score)
    {
        var diff = score - this.score;
        this.score = score;
        scoreBoard.text = score.ToString();
        if (diff == 0) return;
        var anim = Object.Instantiate(animation, animContainer);
        var label = anim.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        label.text = StatLine.FormatSigned(diff);
        label.color = diff > 0 ? addColor : removeColor;
        Destroy(anim.gameObject, 1f);
    } 

    internal void Reset()
    {
        score = 0;
        scoreBoard.text = "0";
    }
}

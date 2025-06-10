using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

internal class StatLineDouble : MonoBehaviour
{
    [SerializeField] StatLine statline1; 
    [SerializeField] StatLine statline2;  

    public void SetLines(string label1, float val1, float buffs1,
        string label2, float val2, float buffs2)
    {
        statline1.SetLineSmall(label1, val1, buffs1);
        statline2.SetLineSmall(label2, val2, buffs2);
    }

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionAnimatorEvent : MonoBehaviour
{
    MinionCombat combat;
    private void Awake()
    {
        combat = GetComponentInParent<MinionCombat>();
    }
    public void OnAttack()
    {
        combat.Attack();
    }
}

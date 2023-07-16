using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatorEvent : MonoBehaviour
{
    PlayerCombat combat;
    private void Awake()
    {
        combat = GetComponentInParent<PlayerCombat>();
    }
    public void OnShoot()
    {
        combat.Shoot();
    }
}

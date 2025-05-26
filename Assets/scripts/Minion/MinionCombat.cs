using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class MinionCombat : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float cooldown = 1.5f;
    [SerializeField] protected float hitRadius = .4f;
    protected float nextAttack = 0f;

    [Header("Components")]
    [SerializeField] TriggerSVFX attackFx;
    [SerializeField] protected Transform hitPoint;
    protected MinionAnimator animator;
    protected Minion minion;

    private void Awake()
    {
        animator = GetComponent<MinionAnimator>();
        minion = GetComponent<Minion>();
    }


    internal void TryAttack(Hitable target)
    {
        if (nextAttack <= Time.time)
        {
            nextAttack = Time.time + cooldown;
            animator.Attack();
        }
    }

    public void Attack()
    {
        attackFx.PlayBase(true, this);
        AttackInternal();
    }
    abstract protected void AttackInternal();
}


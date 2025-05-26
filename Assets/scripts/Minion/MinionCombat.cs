using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class MinionCombat : MonoBehaviour
{ 
    protected float nextAttack = 0f;

    [Header("Components")]
    [SerializeField] TriggerSVFX attackFx;
    [SerializeField] protected Transform hitPoint;
    protected MinionAnimator animator;
    protected Minion minion;

    public Minion Owner { get; private set; }

    private void Awake()
    {
        animator = GetComponent<MinionAnimator>();
        minion = GetComponent<Minion>();
    }


    internal void TryAttack(Hitable target)
    {
        if (nextAttack <= Time.time)
        {
            nextAttack = Time.time + Owner.Stats.cooldown;
            animator.Attack();
        }
    }

    public void Attack()
    {
        attackFx.PlayBase(true, this);
        AttackInternal();
    }
    abstract protected void AttackInternal();

    internal void Init(Minion owner) => Owner = owner;
}


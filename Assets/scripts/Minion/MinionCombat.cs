using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionCombat : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] float damage = 10f;
    [SerializeField] float cooldown = 1.5f;
    [SerializeField] float hitRadius = .4f;
    float nextAttack = 0f;

    [Header("Components")]
    [SerializeField] Transform hitPoint;
    MinionAnimator animator;
    Minion minion;

    private void Awake()
    {
        animator = GetComponent<MinionAnimator>();
        minion = GetComponent<Minion>();
    }


    internal void TryAttack(Hitable target)
    {
        if(nextAttack <= Time.time)
        {
            nextAttack = Time.time + cooldown;
            animator.Attack();
        }
    }

    public void Attack()
    {
        var cols = Physics.OverlapSphere(hitPoint.position, hitRadius, minion.HitableLayer);
        List<float> distances = new List<float>();
        List<Hitable> victims = new List<Hitable>();

        if (cols.Length > 0)
            foreach (var col in cols)
            {
                if (col.GetComponent<Hitable>() && col.GetComponent<Hitable>() != this && col.GetComponent<Hitable>().Home != minion.Home)
                {
                    distances.Add((hitPoint.position - col.GetComponent<Hitable>().transform.position).magnitude);
                    victims.Add(col.GetComponent<Hitable>());
                }
            }

        if(victims.Count == 0) return;

        var smallestValue = distances[0];
        var closest = 0;

        for(int i = 1; i < distances.Count; i++)
        {
            if (distances[i] < smallestValue) closest = i;
        }


        if (victims[closest].GetHit(damage, minion))
        {
            minion.Target = null;
        };
    }
}


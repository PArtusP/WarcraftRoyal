using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AI;

enum MinionState
{
    Walk,
    Follow,
    Combat,
    Stop
}
public class Minion : Hitable
{
    MinionController controller;
    MinionCombat combat;
    [SerializeField] private MinionCombatStats baseStats;
    [SerializeField] private MinionCombatStats powerUp; 
    [SerializeField] float sightRadius; 
    MinionState state;

    [SerializeField] private LayerMask hitableLayer;
    private Hitable target;
    private bool isAsset = true;

    public LayerMask HitableLayer { get => hitableLayer; set => hitableLayer = value; }
    public Hitable Target { get => target; set
        {
            target = value;
            if(target == null)
            {
                state = MinionState.Walk;
                controller.SetDestination(new Vector3(home.transform.position.x, home.transform.position.y, -home.transform.position.z));
            }
        }
    }

    public bool IsAsset => isAsset;

    public MinionCombatStats Stats => baseStats + powerUp;

    public MinionCombatStats PowerUp { get => powerUp; set => powerUp = value; }
    public Minion SourcePrefab { get; internal set; } = null;

    override protected void AwakeInternal()
    {
        isAsset = false;
        controller = GetComponent<MinionController>();
        combat = GetComponent<MinionCombat>();
        combat.Init(this);
    }

    private void Update()
    { 
        switch (state)
        {
            case MinionState.Walk:
                CheckForTarget();
                break;
            case MinionState.Follow:
                if ((transform.position - controller.Destination).magnitude > sightRadius || target == null) 
                    Target = null;   
                else if ((transform.position - controller.Destination).magnitude > Stats.hitRadius)
                    controller.SetDestination(target.transform.position);
                else if ((transform.position - controller.Destination).magnitude < Stats.hitRadius)
                {
                    state = MinionState.Combat;
                    controller.SetDestination(target.transform.position);
                    controller.Stop(true);
                }
                break;
            case MinionState.Combat:
                if(target == null) 
                    Target = null; 
                if ((transform.position - controller.Destination).magnitude > Stats.hitRadius)
                    state = MinionState.Follow;
                else
                {
                    transform.LookAt(target.transform, Vector3.up);
                    combat.TryAttack(target);
                }
                break;
            default:
                break;
        }
    }

    private void CheckForTarget()
    {
        var cols = Physics.OverlapSphere(transform.position, sightRadius, hitableLayer);
        List<Hitable> targets = new List<Hitable>();
        if (cols.Length > 0)
        {
            foreach (var col in cols)
            {
                if (col.GetComponent<Hitable>() && col.GetComponent<Hitable>() != this && col.GetComponent<Hitable>().Home != this.Home) 
                    targets.Add(col.GetComponent<Hitable>()); 
            }
            if (targets.Any())
            {
                targets = targets.OrderBy(t => (transform.position - t.transform.position).magnitude).ToList();

                Target = targets.First();
                controller.SetDestination(target.transform.position);
                state = MinionState.Follow;
            }
        }
    }

    private void OnDrawGizmos()
    {
        switch (state)
        {
            case MinionState.Walk:
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, Stats.sightRadius);
                break;
            case MinionState.Follow:
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, Stats.hitRadius);
                break;
            case MinionState.Combat:
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, Stats.hitRadius);
                break;
            default:
                break;
        }
    }

    internal void SetState(MinionState state)
    {
        switch(this.state)
        {
        }
        this.state = state; 
        switch (this.state)
        {
            case MinionState.Stop:
                controller.Stop(true);
                break;

            case MinionState.Walk: 
                controller.Stop(false);
                break;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

enum MinionState
{
    Walk,
    Follow,
    Combat,
    Stop
}
public class SerializedMinion
{
    public int ID = -1;
    public float Health = 0;
}
public class Minion : Hitable
{
    [Header("Shop attributes")]
    [SerializeField] public int ID = -1;
    [SerializeField] internal int cost;

    [Header("Visuals")]
    [SerializeField] public List<RendererToColor> rendererToColor = new List<RendererToColor>(); 
    MinionController controller;
    MinionCombat combat;

    [Header("Stats")]
    [SerializeField] private MinionCombatStats baseStats;
    [SerializeField] private MinionCombatStats powerUp;
    [SerializeField] float sightRadius;
    [SerializeField] private LayerMask hitableLayer;

    Hitable target;

    MinionState State { get; set; } = MinionState.Stop;
    public bool IsAsset { get; set; } = true; 
    public bool IsStopped => State == MinionState.Stop; 

    public LayerMask HitableLayer { get => hitableLayer; set => hitableLayer = value; }
    public Hitable Target
    {
        get => target; set
        { 
            target = value;
            if (target == null)
            { 
                SetState(MinionState.Walk);
                controller.SetDestination(new Vector3(home.transform.position.x, home.transform.position.y, -home.transform.position.z));
            }
        }
    } 
    public MinionCombatStats Stats => baseStats + powerUp; 
    public MinionCombatStats PowerUp { get => powerUp; set => powerUp = value; }
    public Minion SourcePrefab { get; internal set; } = null;
    public SerializedMinion Serialized => new SerializedMinion() { ID = ID, Health = Health }; // Serialized data for saving/loading purposes

    override protected void AwakeInternal()
    {
        IsAsset = false;
        controller = GetComponent<MinionController>();
        ApplyStatsAndStatus();
        combat = GetComponent<MinionCombat>();
        combat.Init(this);
    }

    private void Update()
    {
        if (!IsServer) return;
        switch (State)
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
                    SetState(MinionState.Combat);
                    controller.SetDestination(target.transform.position);
                    controller.Stop(true);
                }
                break;
            case MinionState.Combat:
                if (target == null)
                    Target = null;
                if ((transform.position - controller.Destination).magnitude > Stats.hitRadius)
                    SetState(MinionState.Follow);
                else
                {
                    transform.LookAt(target.transform, Vector3.up);
                    combat.TryAttack(target);
                }
                break;
            case MinionState.Stop:
                Debug.Log($"Stopped: {gameObject.name}"); break;
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
                SetState(MinionState.Follow);
            }
        }
    }

    private void OnDrawGizmos()
    {
        switch (State)
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
        Debug.Log($"Minion, SetState: {gameObject.name} to {state}");
        switch (this.State)
        {
        }
        this.State = state;
        switch (this.State)
        {
            case MinionState.Stop:
                controller.Stop(true);
                break;

            case MinionState.Walk:
                controller.Stop(false);
                break;
        }
    }

    internal void ApplyStatsAndStatus()
    {
        controller.SetSpeed(Stats.speed);
        healthbar.SetMaxHealth(Stats.health);
        Health = Stats.health; 
    }

    internal void SetPowerUp(MinionCombatStats powerUp)
    {
        var old = Stats;
        PowerUp = powerUp;

        if (old.speed != Stats.speed)
            controller.SetSpeed(Stats.speed);

        if (old.health != Stats.health)
        {
            var percentHealth = Health / old.health;
            var currPercent = Health / Stats.health;

            if (currPercent < percentHealth)
                Health = percentHealth * Stats.health;
        }
    }
}
[Serializable]
public class RendererToColor
{
    public int id = 0;
    public Renderer renderer;
}

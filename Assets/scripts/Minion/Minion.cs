using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum MinionState
{
    Walk,
    Follow,
    Combat
}
public class Minion : Hitable
{
    MinionController controller;
    MinionCombat combat;
    [SerializeField] float sightRadius;
    [SerializeField] float hitRadius;
    MinionState state;

    [SerializeField] private LayerMask hitableLayer;
    private Hitable target;

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

    private void Start()
    {
        controller = GetComponent<MinionController>();
        combat = GetComponent<MinionCombat>();
        controller.SetDestination(new Vector3(home.transform.position.x, home.transform.position.y, -home.transform.position.z));
    }

    private void Update()
    {
        if(target)
            Debug.Log("Follow : " + (transform.position - controller.Destination).magnitude);
        switch (state)
        {
            case MinionState.Walk:
                CheckForTarget();
                break;
            case MinionState.Follow:
                if ((transform.position - controller.Destination).magnitude > sightRadius || target == null)
                {
                    target = null;
                    state = MinionState.Walk; 
                    controller.SetDestination(new Vector3(home.transform.position.x, home.transform.position.y, -home.transform.position.z));
                }
                else if ((transform.position - controller.Destination).magnitude > hitRadius)
                    controller.SetDestination(target.transform.position);
                else if ((transform.position - controller.Destination).magnitude < hitRadius)
                {
                    state = MinionState.Combat;
                    controller.SetDestination(target.transform.position);
                    controller.Stop();
                }
                break;
            case MinionState.Combat:
                if(target == null)
                {
                    target = null;
                    state = MinionState.Walk;
                    controller.SetDestination(new Vector3(home.transform.position.x, home.transform.position.y, -home.transform.position.z));
                }
                if ((transform.position - controller.Destination).magnitude > hitRadius)
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
        if (cols.Length > 0)
        {
            foreach (var col in cols)
            {
                if (col.GetComponent<Hitable>() && col.GetComponent<Hitable>() != this && col.GetComponent<Hitable>().Home != this.Home)
                {
                    target = col.GetComponent<Hitable>();
                    controller.SetDestination(target.transform.position);
                    state = MinionState.Follow;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        switch (state)
        {
            case MinionState.Walk:
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, sightRadius);
                break;
            case MinionState.Follow:
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, hitRadius);
                break;
            case MinionState.Combat:
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, hitRadius);
                break;
            default:
                break;
        }
    }
}

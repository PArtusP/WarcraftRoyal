using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MinionController : MonoBehaviour
{
    NavMeshAgent agent;
    MinionAnimator animator;

    public Vector3 Destination { get => agent.destination; }

    private void Awake()
    {
        animator = GetComponent<MinionAnimator>();
        agent = GetComponent<NavMeshAgent>();
    }
    private void Update()
    {
        Debug.Log("Speed = " + agent.velocity);
        animator.SetSpeed(agent.velocity);
    }
    public void SetDestination(Vector3 destination)
    {
        if (!agent.isOnNavMesh) return;
        if (agent.isStopped) agent.isStopped = false;
        if (!agent.SetDestination(destination))
        {
            agent.SetDestination(transform.position);
        }
    }
    public void Stop()
    {
        if (!agent.isOnNavMesh) return;
        agent.isStopped = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(Destination, Vector3.one * .2f);
    }
}

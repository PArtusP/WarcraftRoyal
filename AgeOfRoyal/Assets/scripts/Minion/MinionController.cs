using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class MinionController : NetworkBehaviour
{
    NavMeshAgent agent;
    MinionAnimator animator;

    public Vector3 Destination { get => agent ? agent.destination : transform.position; }
    public NavMeshAgent Agent
    {
        get
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            return agent;
        }
    }

    private void Awake()
    {
        animator = GetComponent<MinionAnimator>();
        agent = GetComponent<NavMeshAgent>();
    }
    private void Update()
    { 
        if(IsServer) animator.SetSpeed(agent.velocity); 
    }  

    public void SetDestination(Vector3 destination)
    {
        if (!agent.isOnNavMesh || !enabled) return;
        if (agent.isStopped) agent.isStopped = false;
        if (!agent.SetDestination(destination))
        {
            agent.SetDestination(transform.position);
        }
    }
    public void Stop(bool v)
    {
        if (agent.isOnNavMesh) 
            agent.isStopped = v; 
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(Destination, Vector3.one * .2f);
    }

    internal void SetSpeed(float speed) => Agent.speed = speed;
}

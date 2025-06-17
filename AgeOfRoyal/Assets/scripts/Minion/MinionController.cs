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
    Coroutine chargeRoutine;
    [SerializedField] float rotationSpeed = 10f;

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
        if(!IsServer) return; 
        
        animator.SetSpeed(agent.velocity); 

        if(agent.destination == null) return;
        
        Vector3 dir = agent.Target.transform.position - agent.transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.1f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
            controller.transform.rotation = Quaternion.RotateToward(controller.transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }
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
        agent.velocity = Vector3.zero; 
        animator.SetSpeed(agent.velocity);
    }

    internal void SetSpeed(float speed) => Agent.speed = speed;

    public void StartCharge(Transform targetTransform, float speed, float stopDistance, System.Action onArrival)
    {
        // Play impact animation
        animator.Action("Charge"); //@TODO should be a loop, how do i get out of it ? On Action "chargeImpact" and/or on action "ChargeStopped"
        
        if (chargeRoutine != null)
            StopCoroutine(chargeRoutine);

        agent.SetDestination(targetTransform.position);
        agent.speed = speed;
    
        chargeRoutine = StartCoroutine(ChargeRoutine(targetTransform, speed, stopDistance, onArrival));
    }

    private IEnumerator ChargeRoutine(Transform targetTransform, float speed, float stopDistance, System.Action onArrival)
    {
        while (targetTransform != null)
        {
            Vector3 direction = (targetTransform.position - transform.position);
            float distance = direction.magnitude;
    
            if (distance <= stopDistance)
                break;
    
            direction.Normalize();
            Vector3 movement = direction * speed * Time.deltaTime;
    
            // Move
            controller.Move(movement);
    
            yield return null;
        }
    
        chargeRoutine = null;
        onArrival?.Invoke();
    }
    
    public void CancelCharge()
    {
        if (chargeRoutine != null)
        {
            StopCoroutine(chargeRoutine);
            chargeRoutine = null;
            animator.Action("ChargeStopped");
        }
    } 
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(Destination, Vector3.one * .2f);
    }

}

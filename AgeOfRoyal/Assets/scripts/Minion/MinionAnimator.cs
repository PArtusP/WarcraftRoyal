using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionAnimator : MonoBehaviour
{

    Animator animator;
    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }
    internal void Action(string animation) => animator.SetTrigger(animation);  
    internal void SetSpeed(Vector3 velocity) => animator.SetFloat("Speed", velocity.magnitude); 
    internal void Resurect() => animator.SetTrigger("Resurect"); 
}

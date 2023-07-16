using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    internal void SetSpeed(Vector3 movement)
    {
        animator.SetFloat("Speed", movement.magnitude);
    }

    internal void BaseAttackAnimation()
    {
        animator.SetTrigger("BaseAttack");
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MinionAnimator : MonoBehaviour
{

    List<Animator> animators;
    private float temporarySpeed;

    private void Awake()
    {
        animators = GetComponentsInChildren<Animator>().ToList();
    }
    internal void Action(string animation) => animators.ForEach(a => a.SetTrigger(animation));  
    internal void SetSpeed(Vector3 velocity) => animators.ForEach(a => a.SetFloat("Speed", velocity.magnitude)); 
    internal void Resurect() => animators.ForEach(a => a.SetTrigger("Resurect")); 
    internal void Play(string trigger) => animators.ForEach(a => a.SetTrigger(trigger)); 

    public void SetAnimatorSpeedTemporary(float tempSpeed, float duration)
    {
        CancelInvoke("ResetTemporarySpeed");
        // If the status is temporary (like freeze), modify the speed accordingly 
        animators.ForEach(a => a.speed = tempSpeed);
        Invoke("ResetTemporarySpeed", duration); // Reset speed after the duration
    }
    private void ResetTemporarySpeed() => animators.ForEach(a => a.speed = 1f); 
}

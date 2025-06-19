using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MinionAnimator : MonoBehaviour
{

    List<Animator> animators;
    private void Awake()
    {
        animators = GetComponentsInChildren<Animator>().ToList();
    }
    internal void Action(string animation) => animators.ForEach(a => a.SetTrigger(animation));  
    internal void SetSpeed(Vector3 velocity) => animators.ForEach(a => a.SetFloat("Speed", velocity.magnitude)); 
    internal void Resurect() => animators.ForEach(a => a.SetTrigger("Resurect")); 
    internal void Play(string trigger) => animators.ForEach(a => a.SetTrigger(trigger)); 
}

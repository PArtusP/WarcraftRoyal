using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Stats")]
    float health = 100f;

    public bool GetHit(float damage)
    {
        health = Mathf.Max(0f, health - damage);
        if(health == 0f)
        {
            Die();
            return true;
        }
        return false;
    }

    private void Die()
    {
        throw new NotImplementedException();
    }
}

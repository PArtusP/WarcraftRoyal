using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerCombat : MonoBehaviour
{
    // Start is called before the first frame update
    private RaycastHit rayHit;
    [SerializeField] private float bulletRange;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private string EnemyTag;
    private int mouseCheck=0;
    void Start()
    {
        
    }
    public void OnBaseAttack(InputAction.CallbackContext context)
    {
        mouseCheck = context.performed.GetHashCode();
    }
    // Update is called once per frame
    void Update()
    {
        if (mouseCheck == 1) {
            shoot();
            mouseCheck = 0;
        }
    }

    private void shoot()
    {
        if(Physics.Raycast(transform.position, transform.forward, out rayHit, bulletRange) && rayHit.collider.gameObject.tag == EnemyTag)
        {
            //ENEMY TAKING DAMAGE
        }
        muzzleFlash.Play();
    }
}

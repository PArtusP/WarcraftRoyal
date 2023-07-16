using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerCombat : MonoBehaviour, IPlayerCombat
{
    [Header("Base attack")]
    [SerializeField] private float bulletRange;
    [SerializeField] private float baseHitDamage;
    [SerializeField] private ParticleSystem baseHitParticles;
    private PlayerAnimator animator;

    [Header("Settings")]
    [SerializeField] private string EnemyTag;

    private void Awake()
    {
        animator = GetComponent<PlayerAnimator>();
    }
    public void BaseAttack(bool performed, bool cancel)
    {
        if (performed)
            animator.BaseAttackAnimation();
    }

    public void Shoot()
    {
        if(Physics.Raycast(transform.position, transform.forward, out var rayHit, bulletRange) && rayHit.collider.gameObject.tag == EnemyTag)
        {
            rayHit.collider.GetComponent<Player>().GetHit(baseHitDamage);
        }
        baseHitParticles.Play();
    }

    public void StrongAttack(bool performed, bool cancel)
    {
        throw new NotImplementedException();
    }

    public void UltAttack(bool performed, bool cancel)
    {
        throw new NotImplementedException();
    }

    public void MoveAction(bool performed, bool cancel)
    {
        throw new NotImplementedException();
    }
}

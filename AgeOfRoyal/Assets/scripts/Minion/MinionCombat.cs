using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

abstract public class MinionCombat : NetworkBehaviour
{
    protected float nextAttack = 0f;

    [Header("Components")]
    [SerializeField] TriggerSVFX attackFx;
    [SerializeField] protected Transform hitPoint;
    protected MinionAnimator animator;
    protected List<UnitModule> modules = new List<UnitModule>();
    protected Minion minion;

    [Header("Range specific")]
    [SerializeField] private ProjectileMove vfx;

    public Minion Owner { get; private set; }
    public Transform HitPoint { get => hitPoint; set => hitPoint = value; }
    public List<UnitModule> Modules => modules; 

    private void Awake()
    {
        animator = GetComponent<MinionAnimator>();
        minion = GetComponent<Minion>();
    }
    private void Update()
    {
        modules.ForEach(m => m.Use(this));
    }

    internal void TryAttack(Hitable target)
    {
        if (nextAttack <= Time.time)
        {
            nextAttack = Time.time + Owner.Stats.cooldown;
            animator.Attack();
        }
    }

    public void Attack()
    {
        if(AttackInternal() && !minion.IsStopped)
            minion.Target = null;
        PlayAttackVfx();
        PlayAttackVfxClientRpc();
    }

    private void PlayAttackVfx() => attackFx.PlayBase(true, this);
    [ClientRpc]
    private void PlayAttackVfxClientRpc()
    {
        if (IsHost) return;
        PlayAttackVfx();
    }

    abstract protected bool AttackInternal();

    internal void Init(Minion owner) => Owner = owner;
    protected void PlayVfx(Hitable hitable)
    {
        var fx = Instantiate(vfx, hitPoint.transform.position, hitPoint.transform.rotation, null);
        fx.Target = hitable.aimPoint;
    }
    [ClientRpc]
    protected void PlayVfxClientRpc(ulong targetId)
    {
        if (IsHost) return;
        PlayVfx(GetNetworkObject(targetId).GetComponent<Hitable>());
    }
}


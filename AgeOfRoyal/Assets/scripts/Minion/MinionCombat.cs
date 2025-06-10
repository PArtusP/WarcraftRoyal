using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MinionCombat : NetworkBehaviour
{
    protected float nextAttack = 0f;

    [Header("Components")]
    [SerializeField] TriggerSVFX attackFx;
    [SerializeField] UnitAttack attack;
    [SerializeField] protected Transform hitPoint;
    protected MinionAnimator animator;
    [SerializeField] protected List<UnitModule> modules = new List<UnitModule>();
    protected UnitWithoutState owner;

    [Header("Range specific")]
    [SerializeField] private ProjectileMove vfx;

    public UnitWithoutState Owner { get; private set; }
    public Transform HitPoint { get => hitPoint; set => hitPoint = value; }
    public List<UnitModule> Modules => modules; 

    private void Awake()
    {
        animator = GetComponent<MinionAnimator>();
        owner = GetComponent<Minion>();
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
        if (!IsServer) return;
        if(!owner.IsStopped && attack.Use(owner))
            owner.Target = null;
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

    //abstract protected bool AttackInternal();

    internal void Init(UnitWithoutState owner) => Owner = owner;
    internal void PlayShootVfx(Hitable hitable)
    {
        var fx = Instantiate(vfx, hitPoint.transform.position, hitPoint.transform.rotation, null);
        fx.Target = hitable.aimPoint;
    }
    [ClientRpc]
    internal void PlayShootVfxClientRpc(ulong targetId)
    {
        if (IsHost) return;
        PlayShootVfx(GetNetworkObject(targetId).GetComponent<Hitable>());
    }
}


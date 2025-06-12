using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Netcode;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Events;

public class MinionCombat : NetworkBehaviour
{  
    [Header("Components")]
    [SerializeField] TriggerSVFX attackFx; //To remove
    [SerializeField] protected Transform hitPoint;
    [SerializeField] protected List<UnitModule> modules = new List<UnitModule>();
    protected MinionAnimator animator;
    protected UnitWithoutState owner;
    protected UnitAction action;

    [Header("Range specific")]
    [SerializeField] private ProjectileMove vfx;

    public UnitWithoutState Owner { get; private set; }
    public Transform HitPoint { get => hitPoint; set => hitPoint = value; } 

    public UnityEvent OnEndActionEvent { get; } = new UnityEvent();
    public List<UnitModule> Modules { get => modules; set => modules = value; }

    private void Awake()
    {
        animator = GetComponent<MinionAnimator>();
        owner = GetComponent<Minion>();
    }
    private void Update()
    {
        modules.ForEach(m => m.Use(this));
    }

    internal void StartAction(Hitable target, UnitAction action)
    {
        if (action == null)
        {
            UnityEngine.Debug.LogError("Action is null in StartAction"); 
        }
        this.action = action;
        UnityEngine.Debug.Log("StartAction: " + action.name + " on " + target.name);
        animator.Action(action.AnimationTrigger);
    }

    public void Action()
    {
        if (!IsServer) return;
        if (!owner.IsStopped && action.Use(owner))
            owner.Target = null; 
        if(action.Vfx != null)
        {
            PlayAttackVfx();
            PlayAttackVfxClientRpc(Owner.Actions.IndexOf(action));
        }
    }

    private void PlayAttackVfx() => action.Vfx.PlayBase(true, this);

    [ClientRpc]
    void PlayAttackVfxClientRpc(int v)
    {
        if (IsHost) return;
        action = Owner.Actions[v];
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


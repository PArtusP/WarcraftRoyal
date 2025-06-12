using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public enum MinionState
{
    Walk,
    Follow,
    Combat,
    Stop
}
public enum Class
{
    Melee,
    Range,
    Mage
}
public class SerializedMinion
{
    public int ID = -1;
    public float Health = 0;
}
abstract public class UnitWithoutState : Hitable, IPointerEnterHandler
{
    [Header("Shop attributes")]
    [SerializeField] public int ID = -1;
    [SerializeField] protected Class type = Class.Melee;
    [SerializeField] new protected string name;
    [SerializeField] internal int cost;
    [SerializeField] internal Sprite icon;
    [SerializeField] protected string description;

    [Header("Visuals")]
    [SerializeField] public List<RendererToColor> rendererToColor = new List<RendererToColor>();
    protected MinionController controller;
    protected MinionCombat combat;
    protected UnityEvent<Minion> OnDisplayToUpdate { get; } = new UnityEvent<Minion>();

    [Header("Stats")]
    [SerializeField] protected UnitStats baseStats;
    [SerializeField] protected LayerMask hitableLayer;
    [SerializeField] protected List<UnitBuff> buffs = new List<UnitBuff>();

    [Header("Actions")]
    /// <summary>
    /// The first item of the array have to be the attack action, the rest are the actions that can be used in the subclass.
    /// </summary>
    [SerializeField] private List<UnitAction> actions;

    protected Hitable target;
    public bool IsAsset { get; set; } = true;
    public UnitStats Stats => baseStats + TotalBuff;
    public List<UnitBuff> Buffs => buffs;
    List<UnitBuff> StatBuffs => buffs.Where(b => !UnitPowerUp.Identity.Equals(b.PowerUp) && b.PowerUp.IsBuff).ToList();
    List<UnitBuff> StatDebuffs => buffs.Where(b => !UnitPowerUp.Identity.Equals(b.PowerUp) && !b.PowerUp.IsBuff).ToList();
    public UnitPowerUp TotalBuff => buffs.Select(b => b.PowerUp).SumPowerUps();
    public List<UnitModule> Modules => combat.Modules;
    public Minion SourcePrefab { get; internal set; } = null;
    public SerializedMinion Serialized => new SerializedMinion() { ID = ID, Health = Health }; // Serialized data for saving/loading purposes

    protected override float MaxHealth { get => Stats.health; set => Stats.health = value; }
    public string Name { get => name; set => name = value; }
    public MinionCombat Combat { get => combat; set => combat = value; }
    internal Class Type { get => type; set => type = value; }
    public LayerMask HitableLayer { get => hitableLayer; set => hitableLayer = value; }

    public Hitable Target
    {
        get => target; set
        {
            target = value;
            if (target == null)
                controller.SetDestination(new Vector3(home.transform.position.x, home.transform.position.y, -home.transform.position.z));
        }
    }

    abstract public bool IsStopped { get; }
    public List<UnitAction> Actions { get => actions; set => actions = value; }

    override protected void AwakeInternal()
    {
        controller = GetComponent<MinionController>();
        combat = GetComponent<MinionCombat>();
        if (combat.Modules.Any())
            combat.Modules = combat.Modules.Select(m => m.Clone()).ToList(); // Init modules
        IsAsset = false;
        combat.Init(this);
        ApplyStatsAndStatus();
    }
    private void Update()
    {
        var toDelete = buffs.Where(b => b.IsExpired()).ToList();
        if (toDelete.Any())
        {
            toDelete.ForEach(b => Debug.Log($"Expired buff: '{b}' from {name}"));
            var old = Stats;
            toDelete.ForEach(b => buffs.Remove(b)); 
            UpdateStatsEffects(old); // IsServer
            OnDisplayToUpdate.Invoke(this as Minion); // Notify UI to update stats display
        }
        if (!IsServer) return;


        var sumHeal = buffs.Sum(b => b.Heal);
        if (sumHeal > 0)
            Heal(sumHeal * Time.deltaTime);

        UpdateInternal();
    }

    protected abstract void UpdateInternal();

    private void OnValidate()
    {
        controller = GetComponent<MinionController>();
        combat = GetComponent<MinionCombat>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsServer) return;
        UnitUpgradeDetailUi.Instance.Display(this);
        UnitUpgradeDetailUi.Instance.OnUpdateEvent = OnDisplayToUpdate;
    }
    internal void ApplyStatsAndStatus()
    {
        controller.SetSpeed(Stats.speed);
        healthbar.SetMaxHealth(Stats.health);
        Health = Stats.health;
    }

    public override bool GetHit(float damage, Hitable opponent)
    {
        if (opponent is Minion m)
        {
            if (m.Type == Class.Mage) damage *= 1f - Stats.armorRange;
            if (m.Type == Class.Range) damage *= 1f - Stats.armorRange;
            if (m.Type == Class.Melee) damage *= 1f - Stats.armorMelee;
        }
        return base.GetHit(damage, opponent);
    }

    internal void SetTarget(Hitable unit) => target = unit;

    #region Modules & Buffs
    internal void AddBuff(UnitBuff unitBuff, float duration = 0, Hitable source = null)
    {
        unitBuff = unitBuff.Clone();
        unitBuff.Source = source != null ? source : this;

        if (buffs.Count(b => b.SourceId == unitBuff.SourceId) == 1 && unitBuff.BuffType == UnitBuffType.Refreshable)
        {
            buffs.First(b => b.SourceId == unitBuff.SourceId).Refresh();
            return;
        }

        if ((unitBuff.BuffType == UnitBuffType.Stackable && buffs.Count(b => b.SourceId == unitBuff.SourceId && b.Source == unitBuff.Source) >= unitBuff.MaxStack)
            || (unitBuff.BuffType != UnitBuffType.Stackable && buffs.Count(b => b.SourceId == unitBuff.SourceId) >= unitBuff.MaxStack))
            return;

        if (unitBuff.BuffType == UnitBuffType.OneShot)
        { // Add other OneShot effect here, like dispel, heal, etc.
            if (unitBuff.Heal != 0)
                Heal(unitBuff.Heal);
            if (unitBuff.Dispel)
                Dispel(Home == source.Home);
        }
        else if (!UnitPowerUp.Identity.Equals(unitBuff.PowerUp) || unitBuff.Heal != 0)
        {
            var old = Stats;
            buffs.Add(unitBuff);

            // TODO : add heal

            UpdateStatsEffects(old);
            OnDisplayToUpdate.Invoke(this as Minion); // Notify UI to update stats display

            if (IsServer)
                AddBuffClientRpc(unitBuff, duration, unitBuff.Source.NetworkObjectId);
        }
        unitBuff.Apply();

    }

    private void UpdateStatsEffects(UnitStats old)
    {
        if (IsServer)
        {
            if (old.speed != Stats.speed)
                controller.SetSpeed(Stats.speed);

            if (old.health != Stats.health)
            {
                var percentHealth = Health / old.health;
                var currPercent = Health / Stats.health;

                if (currPercent < percentHealth)
                    Health = percentHealth * Stats.health;
            }
        }
    }

    [ClientRpc]
    private void AddBuffClientRpc(UnitBuff buff, float duration, ulong source)
    {
        if (IsHost) return;
        Debug.Log("Adding buff: '" + buff + "' to " + name + " from source: " + source);
        AddBuff(buff, duration, GetNetworkObject(source).GetComponent<Hitable>());
    }

    internal void RemoveBuff(UnitBuff unitBuff)
    {
        try
        {
            var old = Stats;
            buffs.Remove(buffs.First(buffs => buffs.SourceId == unitBuff.SourceId && buffs.Source == unitBuff.Source));
            RemoveBuffClientRpc(unitBuff, unitBuff.Source.NetworkObjectId);
            UpdateStatsEffects(old);
            OnDisplayToUpdate.Invoke(this as Minion); // Notify UI to update stats display
        }
        catch (Exception e)
        {
            var l = "";
            buffs.ForEach(b => l += $"  - buff: {b.SourceId} from '{b.Source.NetworkObjectId}'" + "\n");
            Debug.LogError($"Error removing buff: {(unitBuff == null ? "<>" : $"{unitBuff.SourceId} '{(unitBuff.Source == null ? "<>" : unitBuff.Source.NetworkObjectId)}'")} from {name}. \nException: {e.Message} \n" +
                $"Buff list:" + "\n" + l);
        }
    }
    [ClientRpc]
    private void RemoveBuffClientRpc(UnitBuff buff, ulong source)
    {
        if (IsHost) return;
        buff.Source = GetNetworkObject(source).GetComponent<Hitable>();
        Debug.Log("Removing buff: " + buff + " to " + name + " from source: " + source);
        RemoveBuff(buff);
    }

    internal void AddModules(List<UnitModule> modules)
    {
        // @TODO check if exiting same modules, compare, take best stats
        combat.Modules.AddRange(modules);
        modules.ForEach(m => AddModuleClientRpc(m.ID)); // @TODO pas opti
    }
    [ClientRpc]
    private void AddModuleClientRpc(int moduleID)
    {
        if (IsHost) return;
        Debug.Log("Adding module with ID: " + moduleID + " to " + name + " from client.");
        combat.Modules.Add(DbResolver.GetModuleById(moduleID));
    }
    #endregion

    #region Dispel
    public bool CanBeDispelled(bool sameTeam)
    {
        if (buffs.Any(b => b.PowerUp.Short == "-10% rate"))
            Debug.Log("here");
        var res = GetBuffsDispellable(sameTeam);
        res.ForEach(b => Debug.Log($"{Name}'s buff '{b}' can be dispelled (same team? {sameTeam}) from {name}"));
        return res.Any();
    }
    private List<UnitBuff> GetBuffsDispellable(bool sameTeam) =>
        (sameTeam ? StatDebuffs : StatBuffs).Where(b => b.BuffType != UnitBuffType.OneShot && b.BuffType != UnitBuffType.Permanent && b.BuffType != UnitBuffType.Aura && b.CanBeDispelled).ToList();

    internal void Dispel(bool removeDebuff)
    {
        var toDebuf = GetBuffsDispellable(removeDebuff);
        toDebuf.ForEach(b => Debug.Log($"Dispel '{b}' from {name}"));
        toDebuf.ForEach(b => buffs.Remove(b));  
    }
    #endregion

    #region VFX
    internal void PlayVfx(TriggerSVFX vfx, bool value = true) => vfx.PlayBase(value, this);

    [ClientRpc]
    internal void PlayModuleOnTargetVfxClientRpc(int ID, ulong sourceId, bool value = true)
    {
        if (IsHost) return; // Don't play VFX on host, it will be played on server and synced to clients
        PlayVfx(DbResolver.GetModuleById(ID).OnTargetVfx, value);
    }

    [ClientRpc]
    internal void PlayModuleOnSelfVfxClientRpc(int ID, ulong sourceId, bool value = true)
    {
        if (IsHost) return; // Don't play VFX on host, it will be played on server and synced to clients
        PlayVfx(DbResolver.GetModuleById(ID).OnSelfVfx, value);
    }
    #endregion
}

abstract public class UnitBase<T> : UnitWithoutState where T : Enum
{
    protected Dictionary<UnitAction, float> nextAttackDict = new Dictionary<UnitAction, float>();
    protected FSM<T> fsm = new FSM<T>();
    private AttackConditions<T> validContition;
    private List<AttackConditions<T>> conditons = new List<AttackConditions<T>>();

    abstract public T Stop { get; }
    abstract public T Walk { get; }
    abstract public T Follow { get; }
    abstract public T InCombat { get; }

    protected override void UpdateInternal()
    {
        fsm.CheckNextState();
        fsm.Update();
    }
    protected override void AwakeInternal()
    {
        base.AwakeInternal();
        SetUpFSM();
        Actions = Actions.Select(a => a = a.Clone()).ToList(); // Preserve changes in scriptable assets
        Combat.Modules = Combat.Modules.Select(a => a = a.Clone()).ToList(); // Preserve changes in scriptable assets
        (Actions[0] as UnitAttack).radius = Stats.hitRadius;
        Actions.ForEach(a => nextAttackDict.Add(a, 0f));
        SetUpCondition();
        Combat.OnEndActionEvent.AddListener(delegate ()
        {
            fsm.SwitchState(Follow);
        });
    }

    private void SetUpCondition()
    {
        conditons.Add(new AttackConditions<T>
        {
            action = Actions[0],
            NextStage = InCombat,
            Condition = new AttackCondition<T>
            {
                outRadius = Stats.hitRadius,
                cooldown = Stats.cooldown,
            }
        });
        SetUpConditionInternal(conditons);
    }

    protected abstract void SetUpConditionInternal(List<AttackConditions<T>> conditons);

    #region FSM
    virtual protected void SetUpFSM()
    {
        fsm.states = new List<State<T>>() {
            new State<T>(Stop,
                    null,
                    null,
                    () => controller.Stop(true)
                ),
            new State<T>(Walk,
                    () => CheckForTarget(),
                    null,
                    () => {
                        controller.Stop(false);
                        Target = null;
                    }
            ),
            new State<T>(Follow,
            () => {
                if (target == null)
                    return Walk;

                AttackConditions<T> success = conditons.Where(c => TryCondition(c)).FirstOrDefault();
                if (success != null)
                {
                    validContition = success;
                    Debug.Log("Set valid condition: " + validContition.action.name);
                    return !success.NextStage.Equals(Walk) ? success.NextStage : InCombat;
                }


                else if ((transform.position - target.transform.position).magnitude < Actions.Min(a => a.MinRadius))
                    if(!controller.Agent.isStopped)
                        controller.Stop(true);
                else if((transform.position - controller.Destination).magnitude > .2f)
                    controller.SetDestination(target.transform.position);

                return Follow;
            },
            null
            ),
            new State<T>(InCombat,
                    () => {
                        if (target == null)
                            return Walk;
                        return InCombat;
                    },
                    null,
                    () =>
                    {
                        controller.Stop(true);
                        combat.StartAction(target, validContition.action);
                        nextAttackDict[validContition.action] = Time.time + validContition.Condition.cooldown; 
                    }
                ),
        };
    }

    protected bool TryCondition(AttackConditions<T> c)
    {
        try
        {
            Debug.Log("Try valid condition: " + c.action.name);
            return (transform.position - Target.transform.position).magnitude <= c.Condition.outRadius
                    && (transform.position - Target.transform.position).magnitude >= c.Condition.inRadius
                    && (c.Condition.Check == null || c.Condition.Check(this, Target))
                    //&& CheckAngle(Target, c.Condition.angle)
                    //&& (!c.Condition.directSight || IsClearPathToHitable(Target, c.Condition.outRadius, GameLayers.Hitable.Mask
                    && (nextAttackDict.TryGetValue(c.action, out var nextAttack) && nextAttack <= Time.time);
        }
        catch (Exception e)
        {

            throw;
        }
    }
    public bool IsClearPathToHitable(Hitable target, float checkRadius, LayerMask check)
    {
        Ray ray = new Ray(transform.position + Vector3.up * .5f, target.transform.position - transform.position);
        var all = Physics.RaycastAll(
            transform.position + Vector3.up * .5f,
            target.transform.position - transform.position,
            checkRadius,
            check).ToList();
        all = all.OrderBy(h => (transform.position + Vector3.up * .5f - h.point).magnitude).ToList();
        foreach (var item in all)
        {
            // Check if the hit object is the target
            if (item.collider.gameObject == target.gameObject)
                return true; // Direct hit, no obstruction 
            else
            {
                UnitWithoutState hit = item.collider.GetComponent<UnitWithoutState>() ? item.collider.GetComponent<UnitWithoutState>() : item.collider.GetComponentInParent<UnitWithoutState>() ? item.collider.GetComponentInParent<UnitWithoutState>() : null;
                if (!hit /*|| hit.BlockAttack*/) return false; // Something is obstructing the view
            }
        }
        return false;
    }


    protected T CheckForTarget()
    {
        var cols = Physics.OverlapSphere(transform.position, Stats.sightRadius, hitableLayer);
        List<Hitable> targets = new List<Hitable>();
        if (cols.Length > 0)
        {
            foreach (var col in cols)
            {
                if (col.GetComponent<Hitable>() && col.GetComponent<Hitable>() != this && col.GetComponent<Hitable>().Home != this.Home)
                    targets.Add(col.GetComponent<Hitable>());
            }
            if (targets.Any())
            {
                targets = targets.OrderBy(t => (transform.position - t.transform.position).magnitude).ToList();

                Target = targets.First();
                controller.SetDestination(target.transform.position);
                return Follow;
            }
        }
        return Walk;
    }
    private void OnDrawGizmos()
    {
        switch (fsm.CurrentState)
        {
            case MinionState.Walk:
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, Stats.sightRadius);
                break;
            case MinionState.Follow:
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, Stats.hitRadius);
                break;
            case MinionState.Combat:
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, Stats.hitRadius);
                break;
            default:
                break;
        }
    }

    internal void SetState(T state) => fsm.SwitchState(state);

    #endregion

}

public class Minion : UnitBase<MinionState>
{
    public override bool IsStopped => fsm.CurrentState == MinionState.Stop;

    public override MinionState Stop => MinionState.Stop;

    public override MinionState Walk => MinionState.Walk;

    public override MinionState Follow => MinionState.Follow;

    public override MinionState InCombat => MinionState.Combat;

    protected override void SetUpConditionInternal(List<AttackConditions<MinionState>> conditons)
    {
    }
}
[Serializable]
public class RendererToColor
{
    public int id = 0;
    public Renderer renderer;
}



[System.Serializable]
public class FSM<T> where T : Enum
{
    [SerializeField] private State<T> currentState;
    public List<State<T>> states;

    public T CurrentState { get => currentState != null ? currentState.type : default; }

    public void CheckNextState()
    {
        if (currentState.checkNextState == null) return;
        var nextState = currentState.checkNextState();
        if (!nextState.Equals(currentState.type))
        {
            SwitchState(nextState);
        }
    }

    public void SwitchState(T nextState)
    {
        var oldState = currentState;
        currentState = states.Find(s => s.type.Equals(nextState));
        if (oldState != null && oldState.onEnd != null)
            oldState.onEnd();
        if (currentState.onStart != null)
            currentState.onStart();
    }

    public void Update()
    {
        if (currentState.onUpdate != null)
            currentState.onUpdate();
    }


}
[System.Serializable]
public class State<T> where T : Enum
{
    public T type;
    internal Func<T> checkNextState = null;
    internal Action onUpdate = null;
    internal Action onStart = null;
    internal Action onEnd = null;

    public State(T type, Func<T> checkNextState, Action onUpdate, Action onStart = null, Action onEnd = null)
    {
        this.type = type;
        this.checkNextState = checkNextState;
        this.onUpdate = onUpdate;
        this.onStart = onStart;
        this.onEnd = onEnd;
    }
}
public class AttackConditions<T> where T : Enum
{
    internal UnitAction action = null;
    internal T NextStage = default;
    internal AttackCondition<T> Condition { get; set; }
}
public class AttackCondition<T> where T : Enum
{
    internal float inRadius = 0f;
    internal float outRadius = .5f;
    //internal float angle = 5f;
    internal float cooldown = .6f;
    internal bool directSight = false;
    //internal float rotateUntil = 0f;

    internal Func<UnitBase<T>, Hitable, bool> Check { get; set; } = null;
}
[Serializable]
public class MinionSound
{
    public AudioClip detect;
    public AudioClip hitReaction;
    public AudioClip die;

    void SetClipAndPlay(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.Play();
    }
    void SetClipAndPlayLoop(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.loop = true;
        source.Play();
    }

    public void Detect(AudioSource source) => SetClipAndPlay(source, detect);
    public void Die(AudioSource source) => SetClipAndPlay(source, die);
    internal void HitReaction(AudioSource source) => SetClipAndPlay(source, hitReaction);
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.InputSystem.Controls.AxisControl;
using static UnityEngine.UI.CanvasScaler;

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

public class CachedPowerUp
{
    public float lastUpdate = 0f;
    public UnitPowerUp value = UnitPowerUp.Identity;
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
    private UnitsManager unitManager;
    protected MinionController controller;
    protected MinionCombat combat;
    protected MinionAnimator animator;
    protected UnityEvent<UnitWithoutState> OnDisplayToUpdate { get; } = new UnityEvent<UnitWithoutState>();

    [Header("Stats")]
    [SerializeField] protected UnitStats baseStats;
    [SerializeField] protected List<UnitBuff> buffs = new List<UnitBuff>();

    [Header("Actions")]
    /// <summary>
    /// The first item of the array have to be the attack action, the rest are the actions that can be used in the subclass.
    /// </summary>
    [SerializeField] private List<UnitAction> actions;


    // Trigger system
    private UnitTriggers unitTriggers = new UnitTriggers(); 
    protected Dictionary<UnitAction, float> nextAttackDict = new Dictionary<UnitAction, float>();
    private CachedPowerUp _cachedBuff = new CachedPowerUp();

    // System
    protected Hitable target;
    protected float clampedMaxSpeed = Mathf.Infinity;
     
     private HealthLink sourceHealthLink = null;
     private HealthLink targetHealthLink = null;

    #region Properties
    public bool IsAsset { get; set; } = true;
    public UnitStats Stats => baseStats + TotalBuff;
    public List<UnitBuff> Buffs => buffs;

    List<UnitBuff> StatBuffs => buffs.Where(b => !UnitPowerUp.Identity.Equals(b.PowerUp) && b.PowerUp.IsBuff).ToList();
    List<UnitBuff> StatDebuffs => buffs.Where(b => !UnitPowerUp.Identity.Equals(b.PowerUp) && !b.PowerUp.IsBuff).ToList();
    public UnitPowerUp TotalBuffNoFilter => buffs.Select(b => b.PowerUp).SumPowerUps();
    public UnitPowerUp TotalBuff
    {
        get
        {
            if (_cachedBuff.lastUpdate < Time.time)
            {
                _cachedBuff.lastUpdate = Time.time;
                _cachedBuff.value = buffs
                    .Where(b => (b.Filters == null || (unitManager && b.Filters.ApplyFilter(unitManager, this)))
                             && !b.PowerUp.Equals(UnitPowerUp.Identity))
                    .Select(b => b.PowerUp)
                    .SumPowerUps();
            }

            return _cachedBuff.value;
        }
    }
    public List<UnitModule> Modules => combat.Modules;
    public List<UnitModule> AllModules
    {
        get
        {
            var res = new List<UnitModule>();
            res.AddRange(combat.Modules);
            res.AddRange(actions.Where(a => a is ModulesAction m).Select(m => m as ModulesAction).SelectMany(m => m.Modules));
            res.AddRange(actions.Where(a => a is ChargeAttack m).Select(m => m as ChargeAttack).SelectMany(m => m.OnHitModules));
            return res;
        }
    }
    public bool Taunting => combat.Modules.Any(m => m.GetType() == typeof(AoeTauntModule));
    public UnitWithoutState SourcePrefab { get; internal set; } = null;
    public SerializedMinion Serialized => new SerializedMinion() { ID = ID, Health = Health }; // Serialized data for saving/loading purposes

    public override float MaxHealth { get => Stats.health; set => Stats.health = value; }
    public string Name { get => name; set => name = value; }
    public MinionCombat Combat { get => combat; set => combat = value; }
    internal Class Type { get => type; set => type = value; }
    public LayerMask HitableLayer => GameLayers.Hitable.Mask;

    virtual public Hitable Target
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
    public List<TriggerSVFX> ClientVfxs { get; private set; } = new List<TriggerSVFX>();
    public MinionController Controller => controller;

    public CachedPowerUp CachedBuff { get => _cachedBuff; set => _cachedBuff = value; }
    internal HealthLink SourceHealthLink { get => sourceHealthLink; set => sourceHealthLink = value; }
    internal HealthLink TargetHealthLink { get => targetHealthLink; set => targetHealthLink = value; }
    #endregion

    override protected void AwakeInternal()
    {
        unitManager = FindFirstObjectByType<UnitsManager>();
        controller = GetComponent<MinionController>();
        combat = GetComponent<MinionCombat>();
        animator = GetComponent<MinionAnimator>();
        Debug.Log("Init unit 1#: " + string.Join(", ", Combat.Modules.Select(m => m.ID)));
        if (combat.Modules.Any())
            combat.Modules = combat.Modules.Select(m => m.Clone()).ToList(); // Init modules
        Debug.Log("Init unit 2#: " + string.Join(", ", Combat.Modules.Select(m => m.ID)));
        IsAsset = false;
        combat.Init(this);
        ApplyStatsAndStatus();

        foreach (Trigger key in Enum.GetValues(typeof(Trigger)))  
        {
            unitTriggers.GetEvent(key).AddListener(delegate
            {
                PlayTrigger(key);
            });
        }
        OnDieEvent.AddListener(unitTriggers.OnDieEvent.Invoke);

    }

    private void PlayTrigger(Trigger key)
    { 
        Buffs.Where(b => b.Triggers != null && b.Triggers.Any(t => t == key)).ToList().ForEach(b => AddBuff(b.Clone()));
        combat.Modules.Where(m => m.Triggers != null && m.Triggers.Any(t => t == key)).ToList().ForEach(m => m.Use(combat));
        Actions.Where(m => m.Triggers != null && m.Triggers.Any(t => t == key)).ToList().ForEach(m => m.Use(this));
    }

    private void Update()
    {
        var toDelete = buffs.Where(b => b.IsExpired()).ToList();
        if (toDelete.Any())
        {
            toDelete.ForEach(b => Debug.Log($"Expired buff: '{b}' from {name}"));
            var old = Stats;
            toDelete.ForEach(b =>
            {
                buffs.Remove(b);
            });
            UpdateStatsEffects(old); // IsServer
            OnDisplayToUpdate.Invoke(this as UnitWithoutState); // Notify UI to update stats display
        }
        if (!IsServer) return;

        ComputeEffectDamages(); // @TODO Need to replicate shader on client


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
        UnitUpgradeDetailUi.Instance.Display(this, null, true);
        UnitUpgradeDetailUi.Instance.OnUpdateEvent = OnDisplayToUpdate;
    }
    internal void ApplyStatsAndStatus()
    {
        controller.SetSpeed(Stats.speed);
        healthbar.SetMaxHealth(Stats.health);
        Health = Stats.health;
    }
    internal override void Heal(float v)
    {
        unitTriggers.OnHealEvent.Invoke();
        base.Heal(v * Stats.healModifier);
    }
    public override bool GetHit(float damage, Hitable opponent)
    {
        if (opponent is UnitWithoutState m)
        {
            var armor = 0f;
            switch (m.Type)
            {
                case Class.Melee:
                    armor = Stats.armorMelee;
                    break;
                case Class.Range:
                case Class.Mage:
                    armor = Stats.armorMelee;
                    break;
            }
            damage += -armor + MathF.Max(armor, Stats.ignoreArmor);
        }
        if (targetHealthLink != null)
        {
            var linkDamage = Mathf.Min(targetHealthLink.source.Health - 1f, targetHealthLink.value * damage);
            targetHealthLink.source.GetHit(linkDamage, opponent);
            damage -= targetHealthLink.value * damage;

            if (targetHealthLink.source.Health <= 1f)
            {
                targetHealthLink.source.ClearSourceHealthLink();
                //targetHealthLink = null; targethealthlink is clear in the method above

                // @TODO once SFX are here, stop it there
            }
        }
        return base.GetHit(damage <= 0 ? 1f : damage, opponent);
    }

    internal void SetTarget(Hitable unit) => Target = unit;

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
            if (unitBuff.HealthLink > 0 && source.Health > 1f)
            {
                if (targetHealthLink != null)
                    targetHealthLink.source.ClearSourceHealthLink();

                (source as UnitWithoutState).SetSourceHealthLink(this, unitBuff.HealthLink);
                targetHealthLink = new HealthLink() { source = source as UnitWithoutState, value = unitBuff.HealthLink };
            }
        }
        else if (!unitBuff.isNull)
        {
            var old = Stats;
            buffs.Add(unitBuff);

            UpdateStatsEffects(old);
            OnDisplayToUpdate.Invoke(this as UnitWithoutState); // Notify UI to update stats display

            if (IsServer) // todo
                AddBuffClientRpc(unitBuff, duration, unitBuff.Source.NetworkObjectId);
        }
        unitBuff.Apply();

    }

    private void SetSourceHealthLink(UnitWithoutState unitWithoutState, float healthLink)
    {
        ClearSourceHealthLink();
        sourceHealthLink = new HealthLink() { source = unitWithoutState, value = healthLink };
    }

    private void ClearSourceHealthLink()
    {
        if (sourceHealthLink == null) return;
        var oldSource = sourceHealthLink.source;
        sourceHealthLink = null;
        oldSource.ClearTargetHealthLink();
    }

    private void ClearTargetHealthLink()
    {
        if (targetHealthLink == null) return;
        var oldSource = targetHealthLink.source;
        targetHealthLink = null;
        oldSource.ClearSourceHealthLink(); //@ No, it is the last one called/. edit: hmm maybe yes
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
        AddBuff(buff.Clone(), duration, GetNetworkObject(source).GetComponent<Hitable>());
    }

    internal void RemoveBuff(UnitBuff unitBuff)
    {
        try
        {
            var old = Stats;
            buffs.Remove(buffs.First(buffs => buffs.SourceId == unitBuff.SourceId && buffs.Source == unitBuff.Source));
            RemoveBuffClientRpc(unitBuff, unitBuff.Source.NetworkObjectId);
            UpdateStatsEffects(old);
            OnDisplayToUpdate.Invoke(this as UnitWithoutState); // Notify UI to update stats display
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
        combat.Modules.Add(DbResolver.GetModuleById(moduleID).Clone());
    }
    internal void AddActions(List<UnitAction> actions)
    {
        // @TODO check if exiting same action, compare, take best stats  
        AddActionsInternal(actions);
        actions.ForEach(m => AddActionClientRpc(m.ID)); // @TODO pas optic
    }

    [ClientRpc]
    private void AddActionClientRpc(int actionId)
    {
        if (IsHost) return;
        Debug.Log("Adding action with ID: " + actionId + " to " + name + " from client.");
        AddActionsInternal(actions);
        var action = DbResolver.GetActionById(actionId).Clone();
    }

    protected virtual void AddActionsInternal(List<UnitAction> actions)
    {
        if (actions.Any(a => a is UnitAttack))
        {
            nextAttackDict.Remove(Actions[0]);
            Actions[0] = actions.First(a => a is UnitAttack);
            ResetAttackCondition();
            nextAttackDict.Add(Actions[0], 0f);
            return;
        }
        actions.ForEach(a =>
        {
            if (a.replacedAction != null && Actions.Any(ac => ac.ID == a.replacedAction.ID))
            {
                var toReplace = Actions.First(ac => ac.ID == a.replacedAction.ID);
                Actions[Actions.IndexOf(toReplace)] = a;
            }
            else Actions.Add(a);

            if (!nextAttackDict.TryGetValue(a, out var v))
                nextAttackDict.Add(a, 0f);
        });
    }

    protected abstract void ResetAttackCondition();

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

    /*    internal void PlayModuleClient(TriggerSVFX vfx, ulong sourceId, string id, bool value)
        {
            if (IsHost) return; // Don't play VFX on host, it will be played on server and synced to clients
            Debug.Log($"PlayModuleClient: {(value ? "play" : "stop")} '{id}'");
            if (value)
            {
                vfx.id = Guid.Parse(id);
                PlayVfx(vfx, value);
            ClientVfxs.Add(vfx);
        }
            else
            {
                var effects = FindObjectsByType<TriggerSFVXItem>(FindObjectsSortMode.InstanceID).Where(e => e.Id == Guid.Parse(id)).ToList();
                if (effects.Any())
                {
                    effects.ForEach(e => Destroy(e.gameObject)); // @TODO should probably stop effect then destroy
                }
            }
        }*/

    [ClientRpc]
    internal void PlayModuleOnTargetVfxClientRpc(int moduleId, ulong sourceId, string id, bool value = true)
    {
        if (IsHost) return; // Don't play VFX on host, it will be played on server and synced to clients
        var source = GetNetworkObject(sourceId).GetComponent<UnitWithoutState>();
        var mod = source.AllModules.FirstOrDefault(m => m.ID == moduleId);
        if (mod.OnTargetVfx != null)
            PlayVfx(mod.OnTargetVfx, value);
    }
    [ClientRpc]
    internal void PlayModuleOnSelfVfxClientRpc(int moduleId, ulong sourceId, string id, bool value = true)
    {
        if (IsHost) return; // Don't play VFX on host, it will be played on server and synced to clients
        var source = GetNetworkObject(sourceId).GetComponent<UnitWithoutState>();
        var mod = source.AllModules.FirstOrDefault(m => m.ID == moduleId);
        if (mod.OnTargetVfx != null)
            PlayVfx(mod.OnSelfVfx, value);
    }

    #endregion

    internal void PlayResurectAnimation() => animator.Resurect();
    internal void PlayAnimation(string trigger) => animator.Play(trigger);

    virtual internal void SetAlive(bool value)
    {
        gameObject.SetActive(value);
    }

    [ClientRpc]
    internal void SetAliveClientRpc(bool value)
    {
        if (IsHost) return;
        SetAlive(value);
    }

    internal abstract void StartFSM();


    override internal void ReduceMaxSpeedTemporary(float percentValue, float delay)
    {
        CancelInvoke("ResetMaxSpeed");
        controller.SetSpeed(percentValue * Stats.speed);
        Invoke("ResetMaxSpeed", delay);
    }
    private void ResetMaxSpeed() => controller.SetSpeed(Stats.speed);

    internal override void SetAnimatorSpeedTemporary(float v, float t)
    {
        CancelInvoke("ResetAnimatorSpeed");
        //if(v == 0f) anim.
        Debug.Log($"Set speed {v}, for {t}s");
        animator.SetAnimatorSpeedTemporary(v, t);
    }

}

internal class UnitTriggers
{
    public UnityEvent OnHealEvent { get; internal set; } = new UnityEvent();
    public UnityEvent OnDieEvent { get; internal set; } = new UnityEvent();

    internal UnityEvent GetEvent(Trigger key)
    {
        switch (key)
        {
            case Trigger.Heal:
                return OnHealEvent;
            case Trigger.Die:
                return OnDieEvent;
            default:
                throw new Exception("euuuh ?");
        }
    }
}

abstract public class UnitBase<T> : UnitWithoutState where T : Enum
{
    [SerializeField] protected FSM<T> fsm = new FSM<T>();
    protected AttackConditions<T> validContition;
    protected List<AttackConditions<T>> conditons = new List<AttackConditions<T>>();

    abstract public T Stop { get; }
    abstract public T Walk { get; }
    abstract public T Follow { get; }
    abstract public T InCombat { get; }
    override public Hitable Target
    {
        get => target; set
        {
            if (value != null && target != null && target != value && !Equals(fsm.CurrentState, InCombat))
            {
                target = value;
                fsm.SwitchState(Follow);
            }
            else
                target = value;
            if (target == null)
            {
                try
                {
                    controller.SetDestination(new Vector3(home.transform.position.x, home.transform.position.y, -home.transform.position.z));
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }
    }

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
            priority = 99,
            NextStage = InCombat,
            Condition = new AttackCondition<T>
            {
                outRadius = Stats.hitRadius,
                cooldown = Stats.cooldown,
            }
        });
        SetUpConditionInternal(conditons);
    }
    protected override void ResetAttackCondition()
    {
        conditons[0].action = Actions[0];
    }

    protected abstract void SetUpConditionInternal(List<AttackConditions<T>> conditons);

    #region FSM
    internal override void StartFSM() => fsm.SwitchState(Walk);
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

                AttackConditions<T> success = conditons.OrderBy(c => c.priority).Where(c => TryCondition(c)).FirstOrDefault();
                if (success != null)
                {
                    validContition = success;
                    Debug.Log("Set valid condition: " + validContition.action.name);
                    return !success.NextStage.Equals(Walk) ? success.NextStage : InCombat;
                }
                else if ((transform.position - target.transform.position).magnitude < Actions.Min(a => a.MinRadius))
                {
                    if(!controller.Agent.isStopped)
                        controller.Stop(true);
                }
                else if((transform.position - controller.Destination).magnitude > .1f)
                    controller.SetDestination(target.transform.position);

                return Follow;
            },
            null
            ),
            new State<T>(InCombat,
                    () => {
                        if (target == null || target.Dead)
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
    protected override void AddActionsInternal(List<UnitAction> actions)
    {
        base.AddActionsInternal(actions);
        if (actions.Any(a => a is UnitAttack))
            return;

        var startTime = Time.time;
        var moduleActions = actions.Select(m => m as ModulesAction).ToList();
        moduleActions.ForEach(m =>
        {
            var modules = m.Modules.Select(m => m as UnitModule);
            conditons.Add(new AttackConditions<T>
            {
                action = m,
                priority = m.Priority,
                NextStage = InCombat,
                Condition = new AttackCondition<T>
                {
                    Check = (owner, target) =>
                    {
                        List<UnitWithoutState> minions = new List<UnitWithoutState>();
                        foreach (var item in modules)
                        {
                            if (startTime + item.Delay < Time.time)
                                minions.AddRange(item.FindTargets(Combat));
                        }
                        Debug.Log("Check: " + (minions.Count > 0));
                        var res = minions.Count > 0; // Check if there are any targets in range 

                        return res;
                    },
                    outRadius = m.MinRadius,
                    inRadius = 0f,
                    cooldown = modules.Min(m => m.Cooldown),
                }
            });
        });
    }

    protected bool TryCondition(AttackConditions<T> c)
    {
        try
        {

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
        var cols = Physics.OverlapSphere(transform.position, Stats.sightRadius, HitableLayer);
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

    internal override void SetAlive(bool value)
    {
        base.SetAlive(value);
        if (!IsServer) return;
        if (value) fsm.SwitchState(Walk);
        else fsm.SwitchState(Stop);
    }


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
    internal int priority;

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

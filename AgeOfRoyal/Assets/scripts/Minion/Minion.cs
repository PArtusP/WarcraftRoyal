using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

    [Header("Stats")]
    [SerializeField] protected UnitStats baseStats;
    [SerializeField] protected LayerMask hitableLayer;
    [SerializeField] protected List<UnitBuff> buffs = new List<UnitBuff>();

    protected Hitable target;
    public bool IsAsset { get; set; } = true;
    public UnitStats Stats => baseStats + TotalBuff;
    public List<UnitBuff> Buffs => buffs;
    UnitPowerUp StatBuffs => buffs.Where(b => b.PowerUp.IsBuff).Select(b => b.PowerUp).SumPowerUps();
    UnitPowerUp StatDebuffs => buffs.Where(b => b.PowerUp.IsBuff).Select(b => b.PowerUp).SumPowerUps();
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
    override protected void AwakeInternal()
    {
        IsAsset = false;
        controller = GetComponent<MinionController>();
        combat = GetComponent<MinionCombat>();
        combat.Init(this);
        ApplyStatsAndStatus();
    }


    private void OnValidate()
    {
        controller = GetComponent<MinionController>();
        combat = GetComponent<MinionCombat>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsServer)
            UnitUpgradeDetailUi.Instance.Display(this, TotalBuff);
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

    internal void AddPowerUp(UnitBuff powerUp, float duration = 0, Hitable source = null)
    {
        powerUp = powerUp.Clone();
        powerUp.Source = source ?? this;
        if (buffs.Where(b => b.SourceId == powerUp.SourceId).Count() == 1 && powerUp.BuffType == UnitBuffType.Refreshable)
            buffs.First(b => b.SourceId == powerUp.SourceId).Refresh();

        if (buffs.Where(b => b.SourceId == powerUp.SourceId).Count() >= powerUp.MaxStack)
            return;

        if (buffs.Any(b => b.BuffType != UnitBuffType.Stackable && b.SourceId == powerUp.SourceId && b.Source == powerUp.Source))
            return;

        if (powerUp.BuffType == UnitBuffType.OneShot)
        { // Add other OneShot effect here, like dispel, heal, etc.
            if (powerUp.Heal != 0)
                Heal(powerUp.Heal);
            if (powerUp.Dispel)
                RemoveBuffs(Home == source.Home);
        }
        else if (!UnitPowerUp.Identity.Equals(powerUp.PowerUp) || powerUp.Heal != 0)
        {
            var old = Stats;
            buffs.Add(powerUp);

            if (old.speed != Stats.speed)
                controller.SetSpeed(Stats.speed);

            if (old.health != Stats.health)
            {
                var percentHealth = Health / old.health;
                var currPercent = Health / Stats.health;

                if (currPercent < percentHealth)
                    Health = percentHealth * Stats.health;
            }

            if (powerUp.BuffType == UnitBuffType.Temporary || powerUp.BuffType == UnitBuffType.Refreshable)
                StartCoroutine(WaitToRemovePowerUp(powerUp, duration));
        }
        powerUp.Apply();

    }
    private IEnumerator WaitToRemovePowerUp(UnitBuff powerUp, float duration)
    {
        while (!powerUp.IsExpired())
        {
            yield return new WaitForSeconds(powerUp.AppliedTime);
        }
        buffs.Remove(powerUp);
    }

    internal void AddModules(List<UnitModule> modules)
    {
        // @TODO check if exiting same modules, compare, take best stats
        combat.Modules.AddRange(modules);
    }

    internal void SetTarget(Hitable unit) => target = unit;

    internal void RemoveBuffs(bool removeDebuff) => buffs.RemoveAll(item => buffs.Where(b => b.CanBeDispelled && b.PowerUp.IsBuff != removeDebuff).Contains(item)); // @TODO pas opti
}

abstract public class UnitBase<T> : UnitWithoutState where T : Enum
{
    protected FSM<T> fsm = new FSM<T>();
    private AttackConditions<T> validContition;
    private List<AttackConditions<T>> conditons;

    abstract public T Stop { get; }
    abstract public T Walk { get; }
    abstract public T Follow { get; }
    abstract public T InCombat { get; }

    private void Update()
    {
        if (!IsServer) return;

        buffs.RemoveAll(b => b.IsExpired());

        var sumHeal = buffs.Sum(b => b.Heal);
        if (sumHeal > 0)
            Heal(sumHeal * Time.deltaTime);

        fsm.CheckNextState();
        fsm.Update();
    }
    protected override void AwakeInternal()
    {
        base.AwakeInternal();
        SetUpFSM();
    }

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
                if ((transform.position - controller.Destination).magnitude > Stats.sightRadius || target == null)
                {
                    return Walk;
                }
                else if ((transform.position - controller.Destination).magnitude > Stats.hitRadius)
                    controller.SetDestination(target.transform.position);
                else if ((transform.position - controller.Destination).magnitude < Stats.hitRadius)
                {
                    controller.SetDestination(target.transform.position);
                    controller.Stop(true);
                    return InCombat;


                    AttackConditions<T> success = conditons.Where(c => TryAttackCondition(c)).FirstOrDefault();
                    if (success != null)
                    {
                        validContition = success;
                        //return !success.NextStage.Equals(CheckForTarget) ? success.NextStage : HitTarget;
                        return success.NextStage;
                    }

                }
                return Follow;
            },
            null
            ),
            new State<T>(InCombat,
                    () => {
                        if (target == null)
                            return Walk;
                        else if ((transform.position - controller.Destination).magnitude > Stats.hitRadius)
                            return Follow;
                        return InCombat;
                    },
            () => {
                transform.LookAt(target.transform, Vector3.up);
                        combat.TryAttack(target);
                    }
                ),
        };
    }

    protected bool TryAttackCondition(AttackConditions<T> c)
    {
        return (transform.position - Target.transform.position).magnitude <= c.Condition.outRadius
                                && (transform.position - Target.transform.position).magnitude >= c.Condition.inRadius
                                //&& CheckAngle(Target, c.Condition.angle)
                                && (!c.Condition.directSight
                                //|| IsClearPathToHitable(Target, c.Condition.outRadius, GameLayers.Hitable.Mask
                                );
        //&& (c.attack.lastComboEnd == 0 || c.attack.lastComboEnd + c.attack.Previous.isNotAttackingTime < Time.time);
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
    internal float angle = 5f;
    internal float cooldown = .6f;
    internal bool directSight = true;
    internal float rotateUntil = 0f;

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
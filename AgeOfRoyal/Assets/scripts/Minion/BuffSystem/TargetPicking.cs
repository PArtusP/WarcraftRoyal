using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;

public enum PickingFields
{
    Cost = 1,
    Health = 2,
    Damage = 3,
    Rate = 4,
    Melee = 5,
    Range = 6,
    Mage = 7,
    ToDispel = 8,
    Furthest = 9,
}

[Serializable]
public class TargetPicking
{ 
    [SerializeField] int maxTarget = 3;
    [SerializeField] protected Target sameTeam = Target.Friends;
    [SerializeField] List<TargetingOrderBy> orders = new List<TargetingOrderBy>();
    [SerializeField] List<TargetingFilter> filters = new List<TargetingFilter>();

    public Target SameTeam  => sameTeam;

    public int MaxTarget => maxTarget;  

    public List<Minion> PickTargets(List<Minion> targets, UnitWithoutState source)
    {
        targets = targets.Where(t => t != null && t.Health > 0 && CheckTeam(source, t)).ToList(); // @TODO for resurect, change here

        foreach (var filter in filters) 
            targets = filter.Pick(targets, source); 

        for (int i = orders.Count - 1; i >  0; i--) 
            targets = orders[i].Pick(targets, source); 

        return targets.Any() ? targets.Where(t => targets.IndexOf  (t) < maxTarget).ToList() : new List<Minion>();
    }
    private bool CheckTeam(UnitWithoutState owner, Minion target)
    {
        switch (sameTeam)
        {
            case Target.Friends:
                return owner.Home == target.Home;
            case Target.Foes:
                return owner.Home != target.Home;
            case Target.All:
            default:
                return true;
        }
    }
}

[Serializable]
public class TargetingOrderBy
{
    /// <summary>
    /// / Picking field to order by.
    /// </summary>
    [SerializeField] PickingFields field;
    /// <summary>
    /// Whether to order in ascending or descending order. Is not applicable for 'type' and 'to displel'.
    /// </summary>
    [SerializeField] bool ascending;

    public List<Minion> Pick(List<Minion> targets, Hitable source)
    {
        switch (field)
        {
            case PickingFields.Cost:
                return ascending ? targets.OrderBy(t => t.cost).ToList() : targets.OrderByDescending(t => t.cost).ToList();
            case PickingFields.Health:
                return ascending ? targets.OrderBy(t => t.Health).ToList() : targets.OrderByDescending(t => t.Health).ToList();
            case PickingFields.Damage:
                return ascending ? targets.OrderBy(t => t.Stats.damage).ToList() : targets.OrderByDescending(t => t.Stats.damage).ToList();
            case PickingFields.Rate:
                return ascending ? targets.OrderBy(t => t.Stats.cooldown).ToList() : targets.OrderByDescending(t => t.Stats.cooldown).ToList();

            case PickingFields.Melee:
                return ReorderByMatch(targets, t => t.Type == Class.Melee);
            case PickingFields.Range:
                return ReorderByMatch(targets, t => t.Type == Class.Range);
            case PickingFields.Mage:
                return ReorderByMatch(targets, t => t.Type == Class.Mage);

            case PickingFields.ToDispel:
                return ReorderByMatch(targets, t => t.TotalBuff.IsBuff != (t.Home == source.Home));

            case PickingFields.Furthest:
                return ascending ? targets.OrderBy(t => (t.transform.position - source.transform.position).magnitude).ToList() : targets.OrderByDescending(t => (t.transform.position - source.transform.position).magnitude).ToList();

            default:
                throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }
    }

    private List<Minion> ReorderByMatch(List<Minion> input, Func<Minion, bool> predicate)
    {
        var matched = input.Where(predicate);
        var unmatched = input.Where(t => !predicate(t));
        return matched.Concat(unmatched).ToList();
    }
}

[Serializable]
public class TargetingFilter
{
    [SerializeField] PickingFields field; 

    public List<Minion> Pick(List<Minion> targets, Hitable source)
    {
        switch (field)
        {
/*            case PickingFields.Cost:
                return targets.OrderBy(t => t.cost).ToList();
            case PickingFields.Health:
                return targets.OrderBy(t => t.Health).ToList();
            case PickingFields.Damage:
                return targets.OrderBy(t => t.Stats.damage).ToList();
            case PickingFields.Rate:
                return targets.OrderBy(t => t.Stats.cooldown).ToList();*/

            case PickingFields.Melee:
                return targets.Where(t => t.Type == Class.Melee).ToList();
            case PickingFields.Range:
                return targets.Where(t => t.Type == Class.Range).ToList();
            case PickingFields.Mage:
                return targets.Where(t => t.Type == Class.Mage).ToList();

            case PickingFields.ToDispel:
                return targets.Where(t => t.TotalBuff.IsBuff != (t.Home == source.Home)).ToList();

/*            case PickingFields.Furthest:
                return targets.OrderBy(t => (t.transform.position - source.transform.position).magnitude).ToList();*/

            default:
                throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }
    } 
}

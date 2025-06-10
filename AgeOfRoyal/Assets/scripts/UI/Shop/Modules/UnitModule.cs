using System.Linq;
using UnityEngine;

[System.Serializable]
abstract public class UnitModule : ScriptableObject
{
    abstract public string Description { get; }
    abstract public int Use(MinionCombat owner, int maxTargetOverride = -1);
}

public enum Target
{
    Friends,
    Foes,
    All
}

[System.Serializable]
abstract public class AoeUnitModule : UnitModule
{
    [SerializeField] protected float cooldown = 5f;
    [SerializeField] protected float radius = 8f;
    [SerializeField] protected TargetPicking picking;

    float lastUsed = 0f;

    public override int Use(MinionCombat owner, int maxTargetOverride = -1)
    {
        if (!owner.IsServer) return 0;
        var maxTarget = maxTargetOverride == -1 ? picking.MaxTarget : maxTargetOverride; 
        var cols = Physics.OverlapSphere(owner.HitPoint.position, radius, GameLayers.Hitable.Mask);
        var nbTouched = 0;
        var minions = picking.PickTargets(cols
            .Where(col => col.GetComponent<Minion>() != null && col.GetComponent<Minion>() != owner.Owner)
            .Select(col => col.GetComponent<Minion>()).ToList(), owner.Owner); // @TODO on self to change here 


        foreach (var h in minions)
        {
            nbTouched++;
            ApplyEffect(h, owner);
            if (nbTouched >= maxTarget)
                break; // Stop if we reached the max target limit 
        }
        DrawCircle(owner.transform.position, radius, 12, nbTouched > 0 ? Color.green : Color.red);
        return nbTouched;
    }


    protected abstract void ApplyEffect(Hitable target, MinionCombat owner);
    void DrawCircle(Vector3 center, float radius, int segments, Color color)
    {
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angleCurrent = Mathf.Deg2Rad * (i * angleStep);
            float angleNext = Mathf.Deg2Rad * ((i + 1) % segments * angleStep);

            Vector3 pointCurrent = center + new Vector3(Mathf.Cos(angleCurrent), 0f, Mathf.Sin(angleCurrent)) * radius;
            Vector3 pointNext = center + new Vector3(Mathf.Cos(angleNext), 0f, Mathf.Sin(angleNext)) * radius;

            Debug.DrawLine(pointCurrent, pointNext, color);
        }
    }
}
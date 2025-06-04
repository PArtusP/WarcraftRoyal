using System.Linq;
using UnityEngine;

[System.Serializable]
abstract public class UnitModule : ScriptableObject
{
    abstract public void Use(MinionCombat owner);
}


[System.Serializable]
[CreateAssetMenu(fileName = "___ - MOD AOE heal", menuName = "Unit Modules/AOE heal", order = 1)]
public class AoeHealModule : UnitModule
{
    [SerializeField] private float healSpeed = 4f;
    [SerializeField] private float healRadius = 5f;

    override public void Use(MinionCombat owner)
    {
        var cols = Physics.OverlapSphere(owner.HitPoint.position, healRadius, GameLayers.Hitable.Mask);
        cols.ToList().ForEach(c =>
        {
            var h = c.GetComponent<Hitable>();
            if (h != null 
            && h != owner.Owner 
            && h.Home == owner.Owner.Home
            && h.HealthPercent < 1f)
                h.Heal(healSpeed * Time.deltaTime);
        });
    }
}

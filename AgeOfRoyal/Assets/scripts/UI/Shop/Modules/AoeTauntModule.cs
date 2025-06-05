using System.Linq;
using UnityEngine;
 
[System.Serializable]
[CreateAssetMenu(fileName = "___ - MOD AOE Taunt", menuName = "Unit Modules/AOE Taunt", order = 1)]
public class AoeTauntModule : UnitModule
{ 
    [SerializeField] private float radius = 8f;

    override public void Use(MinionCombat owner)
    {
        var cols = Physics.OverlapSphere(owner.HitPoint.position, radius, GameLayers.Hitable.Mask);
        cols.ToList().ForEach(c =>
        {
            var h = c.GetComponent<Minion>();
            if (h != null 
            && h != owner.Owner 
            && h.Home != owner.Owner.Home )
                h.SetTarget(owner.Owner);
        });
    }
}

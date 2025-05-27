
using UnityEngine;
using UnityEngine.UI;

public class UnitUpgradeButton : RightClickButton
{

    [SerializeField] MinionCombatStats powerUp;
    [SerializeField] int costValue;
    [SerializeField] Minion prefab;
    [SerializeField] TMPro.TextMeshProUGUI status;
    [SerializeField] Image image;
    [SerializeField] new string name;

    public Minion Prefab { get => prefab; set => prefab = value; }
    public int CostValue   => costValue;

    public MinionCombatStats PowerUp   => powerUp;
    public bool IsOwned => status.text == "Sold !";

    public Sprite Image { get => image.sprite; set => image.sprite = value; }
    public string Name { get => name; set => name = value; }

    override public void Buy() => status.text = "Sold !";

    override public void Sell() => status.text = "";
    protected override void SetCost() => cost.text = costValue.ToString();

    internal void Reset() => status.text = 0.ToString();
}
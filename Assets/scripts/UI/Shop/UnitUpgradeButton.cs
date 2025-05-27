
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

    [SerializeField] UnitUpgrade upgrade;

    public Minion Target => upgrade.Target;
    public int Cost => upgrade.Cost;

    public MinionCombatStats PowerUp => upgrade.PowerUp;
    public bool IsOwned => status.text == "Sold !";

    public Sprite Image { get => image.sprite; set => image.sprite = value; }
    public string Name => upgrade.Name;

    override public void Buy() => status.text = "Sold !";

    override public void Sell() => status.text = "";
    protected override void SetCost() => cost.text = costValue.ToString();

    internal void Reset() => status.text = 0.ToString();
}
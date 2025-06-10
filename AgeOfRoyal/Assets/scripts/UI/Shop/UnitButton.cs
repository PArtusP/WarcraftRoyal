using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

abstract public class RightClickButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Button button;
    [SerializeField] protected TMPro.TextMeshProUGUI cost;
    [SerializeField] Image image;
    public Sprite Image { get => image.sprite; set => image.sprite = value; }
    public Button Button { get => button; set => button = value; }

    public System.Action OnLeftClick;
    public System.Action OnRightClick;
    public System.Action PointerEnter;
    public System.Action PointerExit;

    private void Awake()
    {
        SetSprite();
        SetCost();
    }

    protected abstract void SetSprite();
    protected abstract void SetCost();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick?.Invoke();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick?.Invoke();
        }
    }
    public void OnPointerEnter(PointerEventData eventData) => PointerEnter?.Invoke();
    public void OnPointerExit(PointerEventData eventData) => PointerExit?.Invoke();

    abstract public void Buy();
    abstract public void Sell();
}

public class UnitButton : RightClickButton
{

    [SerializeField] Minion prefab;
    [SerializeField] TMPro.TextMeshProUGUI buyCounter;
    [SerializeField] TMPro.TextMeshProUGUI currentCounter; // Not used now

    public Minion Prefab { get => prefab; set => prefab = value; }

    override public void Buy() => buyCounter.text = (buyCounter.text == string.Empty ? 1 : int.Parse(buyCounter.text) + 1).ToString();

    override public void Sell() => buyCounter.text = (buyCounter.text == string.Empty ? 0 : int.Parse(buyCounter.text) - 1).ToString();

    protected override void SetCost() => cost.text = prefab.cost.ToString();
    protected void SetCurrentCount(int count) => currentCounter.text = count.ToString(); // @TODO
    protected override void SetSprite() => Image = prefab.icon;

    internal void Reset() => buyCounter.text = string.Empty;
}

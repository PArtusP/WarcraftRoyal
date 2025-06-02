using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

abstract public class RightClickButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Button button;
    [SerializeField] protected TMPro.TextMeshProUGUI cost;
    public Button Button { get => button; set => button = value; }

    public System.Action OnLeftClick;
    public System.Action OnRightClick;
    public System.Action PointerEnter;
    public System.Action PointerExit;

    private void Awake()
    {
        SetCost();
    }

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
    [SerializeField] TMPro.TextMeshProUGUI counter;

    public Minion Prefab { get => prefab; set => prefab = value; } 

    override public void Buy() => counter.text = (int.Parse(counter.text) + 1).ToString();

    override public void Sell() => counter.text = (int.Parse(counter.text) - 1).ToString();

    protected override void SetCost() => cost.text = prefab.cost.ToString();

    internal void Reset() => counter.text = 0.ToString();
}

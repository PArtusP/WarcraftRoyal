using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputs : MonoBehaviour
{
    PlayerController controller;
    IPlayerCombat combat;

    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();
        controller = GetComponent<PlayerController>();
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        var move = context.ReadValue<Vector2>();
        controller.SetMove(move);
    }
    public void OnBaseAttack(InputAction.CallbackContext context)
    {
        combat.BaseAttack(context.performed, context.canceled);
    }
    public void OnStrongAttack(InputAction.CallbackContext context)
    {
        combat.StrongAttack(context.performed, context.canceled);
    }
    public void OnUltAttack(InputAction.CallbackContext context)
    {
        combat.UltAttack(context.performed, context.canceled);
    }
    public void OnMoveAction(InputAction.CallbackContext context)
    {
        combat.MoveAction(context.performed, context.canceled);
    }
}

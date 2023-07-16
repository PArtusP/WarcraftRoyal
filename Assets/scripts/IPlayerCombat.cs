using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerCombat
{
    public void BaseAttack(bool performed, bool cancel);
    public void StrongAttack(bool performed, bool cancel);
    public void UltAttack(bool performed, bool cancel);
    public void MoveAction(bool performed, bool cancel);
}

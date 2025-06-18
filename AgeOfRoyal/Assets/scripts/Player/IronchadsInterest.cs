using UnityEngine;
abstract public class PlayerInterest : MonoBehaviour
{
    abstract public int Use(int moneyReward, Player owner);
}
public class IronchadsInterest : PlayerInterest
{
    public override int Use(int moneyReward, Player owner)
    {
        return Mathf.FloorToInt(owner.Wallet.Value / 10f) + moneyReward;
    }
}

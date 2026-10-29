using UnityEngine;

namespace CostaBrava
{
    public class MoneySystem : MonoBehaviour
    {
        public int Balance { get; private set; }=200;
        public void Add(int amount) { Balance+=amount; GameManager.Instance.hud.Toast("+$"+amount); GameManager.Instance.audioManager.Play("reward",GameManager.Instance.player.Position); }
        public bool Spend(int amount) { if (Balance<amount) return false; Balance-=amount; return true; }
    }
}

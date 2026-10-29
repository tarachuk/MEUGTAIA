using UnityEngine;
using UnityEngine.InputSystem;

namespace CostaBrava
{
    public class ShopSystem : MonoBehaviour
    {
        public Transform counter;
        public bool IsOpen { get; private set; }
        public bool Near => GameManager.Instance && !GameManager.Instance.player.Vehicle && Vector3.Distance(GameManager.Instance.player.Position,counter.position)<3;
        public string Message { get; private set; }="Suprimentos para sua próxima missão.";
        void Update() { if (!GameManager.Instance || GameManager.Instance.Blocked) return; if (Near && GameInput.Pressed(Key.E)) { IsOpen=true; Message="Suprimentos para sua próxima missão."; GameManager.SetCursor(false); } }
        public void Close() { IsOpen=false; GameManager.SetCursor(true); }
        public void Buy(int item)
        {
            int cost=item==0?50:(item==1?100:75); var game=GameManager.Instance;
            if (item==2 && game.player.health.Current>=100) {Message="Sua vida já está completa.";return;}
            if (!game.money.Spend(cost)) {Message="Dinheiro insuficiente. Complete uma missão.";return;}
            if(item<2) game.player.combat.AddAmmo(item,item==0?36:90); else game.player.health.Heal();
            Message="Compra concluída!"; game.audioManager.Play("reward",game.player.Position);
        }
    }
}

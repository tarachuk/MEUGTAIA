using UnityEngine;
using UnityEngine.InputSystem;
namespace CostaBrava
{
    public class WeaponShop : MonoBehaviour
    {
        public Transform counter;
        public bool IsOpen { get; private set; }
        public string Message { get; private set; }="Rifle Farol com luneta · compra exclusiva desta loja.";
        public bool Near => GameManager.Instance && !GameManager.Instance.player.Vehicle && Vector3.Distance(GameManager.Instance.player.Position,counter.position)<3;
        void Update()
        {
            var game=GameManager.Instance;if(!game || game.Blocked)return;
            if(Near && GameInput.Pressed(Key.E)) {IsOpen=true;Message="Rifle com luneta · $100 · inventário 3.";GameManager.SetCursor(false);}
        }
        public bool BuyRifle()
        {
            var game=GameManager.Instance;
            if(!IsOpen || !Near)return false;
            if(game.player.combat.Owns(2)) {Message="Você já possui o rifle. Munição disponível abaixo.";return false;}
            if(!game.money.Spend(100)) {Message="Dinheiro insuficiente. O rifle custa $100.";return false;}
            game.player.combat.UnlockRifle();Message="Rifle comprado! Pressione 3 e segure o botão direito para usar a luneta.";
            game.audioManager.Play("reward",counter.position);return true;
        }
        public bool BuyAmmo()
        {
            var game=GameManager.Instance;if(!IsOpen || !Near)return false;
            if(!game.player.combat.Owns(2)) {Message="Compre o rifle primeiro.";return false;}
            if(!game.money.Spend(35)) {Message="Dinheiro insuficiente. Munição: $35.";return false;}
            game.player.combat.AddAmmo(2,15);Message="15 cartuchos adicionados.";return true;
        }
        public void Close() {IsOpen=false;GameManager.SetCursor(true);}
    }
}

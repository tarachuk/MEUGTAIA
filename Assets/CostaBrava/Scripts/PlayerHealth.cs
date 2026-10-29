using System.Collections;
using UnityEngine;

namespace CostaBrava
{
    public class PlayerHealth : MonoBehaviour
    {
        public float maximum=100;
        public float Current { get; private set; }=100;
        public bool Dead { get; private set; }
        public float DamageFlash { get; private set; }
        public Vector3 LastAttacker { get; private set; }
        float invulnerableUntil;
        public void Damage(float amount, Vector3 attacker=default)
        {
            if (Dead || Time.time<invulnerableUntil || GameManager.Instance.Blocked) return;
            Current=Mathf.Max(0,Current-amount); DamageFlash=1; LastAttacker=attacker;
            GameManager.Instance.followCamera.Shake(.12f); GameManager.Instance.audioManager.Play("hurt",transform.position);
            if (Current<=0) StartCoroutine(Respawn());
        }
        void Update() { DamageFlash=Mathf.MoveTowards(DamageFlash,0,Time.unscaledDeltaTime*2); }
        public void Heal() { Current=maximum; }
        IEnumerator Respawn()
        {
            Dead=true; GameManager.Instance.player.ExitVehicle(true); if(GameManager.Instance.player.animator)GameManager.Instance.player.animator.SetTrigger("Die");Time.timeScale=.3f;
            yield return new WaitForSecondsRealtime(2.5f);
            Time.timeScale=1;
            var game=GameManager.Instance; game.player.Teleport(game.spawnPoint); Current=maximum; game.wanted.Clear();
            game.missions.OnPlayerDied(); game.money.Spend(Mathf.Min(50,game.money.Balance)); game.player.combat.Restock();
            if(game.player.animator) {game.player.animator.Rebind();game.player.animator.Update(0);}
            invulnerableUntil=Time.time+4; Dead=false;
            game.hud.Notify("De volta à praça", "Atendimento: $50 · Sua missão pode ser tentada novamente.");
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CostaBrava
{
    public class PlayerMelee : MonoBehaviour
    {
        public PlayerController player;
        public int Combo { get; private set; }
        public bool Kicking { get; private set; }
        public float AnimationProgress => attacking?Mathf.Clamp01((Time.time-started)/duration):0;
        public bool Attacking => attacking;
        bool attacking,hit;
        float started,duration,nextAttack,lastAttack=-10;
        void Update()
        {
            var game=GameManager.Instance;if(!game || game.Blocked || player.Vehicle)return;
            if(GameInput.Pressed(Key.F) || (!player.combat.Equipped && GameInput.FirePressed && Cursor.lockState==CursorLockMode.Locked))Attack(false);
            if(GameInput.Pressed(Key.G))Attack(true);
            if(!attacking)return;
            if(!hit && Time.time-started>=duration*.32f) {hit=true;ResolveHit();}
            if(Time.time-started>=duration)attacking=false;
        }
        public bool Attack(bool kick)
        {
            if(Time.time<nextAttack || player.Vehicle || GameManager.Instance.Blocked)return false;
            player.combat.Holster();
            Combo=kick?1:(Time.time-lastAttack<.95f?Combo%3+1:1);Kicking=kick;
            duration=kick?.65f:(Combo==3?.58f:.4f);started=lastAttack=Time.time;nextAttack=Time.time+duration;attacking=true;hit=false;
            GameManager.Instance.audioManager.Play("swing",player.Position,.5f);return true;
        }
        void ResolveHit()
        {
            Vector3 origin=player.Position+Vector3.up;
            float reach=Kicking?2.25f:1.8f;var struck=new HashSet<NPCController>();bool connected=false;
            foreach(var collider in Physics.OverlapSphere(origin+player.transform.forward*.8f,reach*.7f,~(1<<2),QueryTriggerInteraction.Ignore))
            {
                Vector3 direction=collider.bounds.center-origin;direction.y=0;
                if(Vector3.Dot(player.transform.forward,direction.normalized)<.2f || direction.magnitude>reach)continue;
                if(Physics.Linecast(origin,collider.bounds.center,1,QueryTriggerInteraction.Ignore) && collider.GetComponentInParent<NPCController>())continue;
                float damage=Kicking?35:(Combo==3?32:18);float push=Kicking?9:(Combo==3?7:3.5f);
                var npc=collider.GetComponentInParent<NPCController>();
                if(npc && !npc.Dead && struck.Add(npc)) {npc.ReceiveHit(damage,player.transform.forward,push);connected=true;}
                var prop=collider.GetComponentInParent<DestructibleProp>();if(prop) {prop.Damage(damage,player.transform.forward,collider.bounds.center);connected=true;}
            }
            if(connected) {GameManager.Instance.followCamera.Shake(Kicking?.08f:.04f);GameManager.Instance.audioManager.Play("impact",origin,.5f);EffectsPool.Instance.Impact(origin+player.transform.forward,Vector3.up,new Color(1,.8f,.45f),.09f);}
        }
    }
}

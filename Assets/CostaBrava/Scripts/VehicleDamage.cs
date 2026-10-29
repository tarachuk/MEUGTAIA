using System.Collections;
using UnityEngine;

namespace CostaBrava
{
    [RequireComponent(typeof(VehicleController))]
    public class VehicleDamage : MonoBehaviour
    {
        public float maximumHealth=240, explosionRadius=9;
        public bool Destroyed { get; private set; }
        public float Health { get; private set; }
        VehicleController car;
        MaterialPropertyBlock tint;
        void Awake() {car=GetComponent<VehicleController>();Health=maximumHealth;tint=new MaterialPropertyBlock();}
        public void Damage(float amount)
        {
            if(Destroyed)return;
            Health=Mathf.Max(0,Health-amount);
            if(Health<=0)Explode();
            else if(Health<maximumHealth*.35f && EffectsPool.Instance)EffectsPool.Instance.Dust(transform.position+Vector3.up*.7f,Color.gray,.2f);
        }
        public void Explode()
        {
            if(Destroyed)return;Destroyed=true;
            Vector3 center=transform.position;
            var game=GameManager.Instance;
            bool occupied=car.Driver;
            if(occupied)car.Driver.ExitVehicle(true);
            car.DisableVehicle();
            foreach(var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.GetPropertyBlock(tint);tint.SetColor("_BaseColor",new Color(.09f,.08f,.07f));renderer.SetPropertyBlock(tint);
            }
            if(EffectsPool.Instance)EffectsPool.Instance.Explosion(center);
            if(game)
            {
                float distance=Vector3.Distance(center,game.player.Position);
                if(distance<35)game.followCamera.Shake(Mathf.Lerp(.6f,.03f,distance/35));
                game.audioManager.Play("explosion",center,1);
                if(distance<explosionRadius)game.player.health.Damage(Mathf.Lerp(100,5,distance/explosionRadius),center);
                game.wanted.ReportCrime(.7f);NPCController.AlertAll(center,45);
            }
            // Each actor is affected once even when it owns several colliders.
            foreach(var npc in NPCController.All.ToArray())if(npc && npc.gameObject.activeInHierarchy && !npc.Dead)
            {
                float distance=Vector3.Distance(npc.transform.position,center);
                if(distance<explosionRadius)npc.ReceiveHit(Mathf.Lerp(160,20,distance/explosionRadius),npc.transform.position-center,8);
            }
            foreach(var prop in DestructibleProp.All.ToArray())if(prop && prop.gameObject.activeInHierarchy && Vector3.Distance(prop.transform.position,center)<explosionRadius)prop.Blast(center,180,explosionRadius,55);
            foreach(var vehicle in VehicleController.All.ToArray())if(vehicle && vehicle!=car && vehicle.IsUsable && Vector3.Distance(vehicle.transform.position,center)<explosionRadius)
            {
                var damage=vehicle.GetComponent<VehicleDamage>();if(damage)damage.StartCoroutine(damage.DelayedBlast(90));
            }
            car.Body.AddExplosionForce(2200,center-Vector3.up,5,.25f,ForceMode.Impulse);
            if(game)game.missions.OnVehicleDestroyed(car);
        }
        IEnumerator DelayedBlast(float amount) {yield return new WaitForSeconds(.2f);Damage(amount);}
    }
}

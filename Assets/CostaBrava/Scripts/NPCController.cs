using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CostaBrava
{
    public enum NPCType { Pedestrian, Hostile, Police }
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCController : MonoBehaviour
    {
        public static readonly List<NPCController> All=new List<NPCController>();
        public NPCType type;
        public Animator animator;
        public Transform muzzle;
        public Collider hitbox;
        public bool missionEnemy;
        public bool responder=true;
        public float maximumHealth=75;
        public bool Dead { get; private set; }
        public float Health { get; private set; }
        public bool SeesPlayer { get; private set; }
        NavMeshAgent agent;
        Vector3 home, fleeFrom;
        float nextDecision, nextShot, frightenedUntil, respawnAt, staggerUntil;
        Vector3 knockback;
        void Awake() {agent=GetComponent<NavMeshAgent>(); All.Add(this);Health=maximumHealth;home=transform.position;}
        void Start() {home=transform.position; nextDecision=Time.time+Random.value*3;}
        void Update()
        {
            var game=GameManager.Instance;if(!game || !game.Started || game.Paused || game.MenuOpen || game.player.health.Dead) return;
            if(Dead) {if((type==NPCType.Pedestrian || (type==NPCType.Police && !responder)) && Time.time>respawnAt) ResetNPC();return;}
            float distance=Vector3.Distance(transform.position,game.player.Position);
            if(!agent.isOnNavMesh) return;
            if(knockback.sqrMagnitude>.01f) {agent.Move(knockback*Time.deltaTime);knockback=Vector3.MoveTowards(knockback,Vector3.zero,Time.deltaTime*16);}
            if(distance>100) {agent.isStopped=true;return;}
            agent.isStopped=Time.time<staggerUntil;
            bool aggressive=type==NPCType.Hostile || (type==NPCType.Police && game.wanted.Level>0);
            SeesPlayer=aggressive && distance<40 && !Physics.Linecast(transform.position+Vector3.up*1.5f,game.player.Position+Vector3.up,1,QueryTriggerInteraction.Ignore);
            if(type==NPCType.Police && SeesPlayer) game.wanted.Seen();
            if(aggressive && distance<(type==NPCType.Police && responder?100:45))
            {
                agent.speed=type==NPCType.Police?4.6f:3.3f;
                if(Time.time>nextDecision) {nextDecision=Time.time+.35f;agent.SetDestination(game.player.Position);}
                if(SeesPlayer && distance<22)
                {
                    agent.isStopped=true; Vector3 dir=game.player.Position-transform.position;dir.y=0;
                    if(dir.sqrMagnitude>.01f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(dir),Time.deltaTime*8);
                    if(Time.time>nextShot && Time.time>staggerUntil)
                    {
                        nextShot=Time.time+(type==NPCType.Police?1.25f:1.7f)+Random.value*.4f;
                        Vector3 start=muzzle?muzzle.position:transform.position+Vector3.up*1.3f;
                        EffectsPool.Instance.Trace(start,game.player.Position+Vector3.up*1.2f);
                        EffectsPool.Instance.Impact(start,transform.forward,Color.yellow,.07f);
                        game.audioManager.Play("shot",start,.35f);
                        if(Random.value<(game.player.Sprinting || game.player.Vehicle?.35f:.65f)) game.player.health.Damage(type==NPCType.Police?8:12,transform.position);
                    }
                }
            }
            else if(Time.time>nextDecision)
            {
                nextDecision=Time.time+Random.Range(2,5);
                bool frightened=Time.time<frightenedUntil || (type==NPCType.Pedestrian && game.wanted.Level>0 && distance<20);
                agent.speed=frightened?5:1.4f;
                Vector3 target=frightened?transform.position+(transform.position-fleeFrom).normalized*15:home+Random.insideUnitSphere*18;
                if(type==NPCType.Hostile) target=home+Random.insideUnitSphere*4;
                target.y=home.y;
                if(NavMesh.SamplePosition(target,out var spot,20,NavMesh.AllAreas)) agent.SetDestination(spot.position);
            }
            if(type==NPCType.Pedestrian) foreach(var car in VehicleController.All)
            {
                if(car && Mathf.Abs(car.Speed)>5 && Vector3.Distance(car.transform.position,transform.position)<6) {Frighten(car.transform.position);break;}
            }
            if(animator) {animator.SetFloat("Speed",agent.velocity.magnitude,.15f,Time.deltaTime);animator.SetBool("Grounded",true);}
        }
        public void Frighten(Vector3 position) {fleeFrom=position;frightenedUntil=Time.time+6;nextDecision=0;}
        public static void AlertAll(Vector3 position,float radius) {foreach(var npc in All) if(npc && !npc.Dead && npc.type==NPCType.Pedestrian && Vector3.Distance(npc.transform.position,position)<radius) npc.Frighten(position);}
        public void Damage(float amount)
        {
            if(Dead) return;Health-=amount;staggerUntil=Time.time+.25f;
            GameManager.Instance.wanted.ReportCrime(type==NPCType.Police?1.3f:(type==NPCType.Pedestrian?1:.2f));
            AlertAll(transform.position,25);
            if(Health<=0)
            {
                Dead=true;SeesPlayer=false;agent.isStopped=true;agent.enabled=false;if(hitbox)hitbox.enabled=false;
                if(animator)animator.SetTrigger("Die");respawnAt=Time.time+35;
                if(type==NPCType.Hostile)GameManager.Instance.money.Add(20);
            }
        }
        public void ReceiveHit(float amount,Vector3 direction,float force)
        {
            if(Dead)return;direction.y=0;knockback=direction.normalized*force;
            if(agent.enabled && agent.isOnNavMesh)agent.Move(direction.normalized*Mathf.Min(.35f,force*.04f));
            Damage(amount);
        }
        public void ResetNPC()
        {
            Dead=false;Health=maximumHealth;knockback=Vector3.zero;transform.position=home;agent.enabled=true;
            if(agent.isOnNavMesh) {agent.Warp(home);agent.isStopped=false;}
            if(hitbox)hitbox.enabled=true;if(animator) {animator.Rebind();animator.Update(0);}
        }
        void OnDestroy() {All.Remove(this);}
    }
}

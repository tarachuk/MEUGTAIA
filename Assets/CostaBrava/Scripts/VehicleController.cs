using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace CostaBrava
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        public static readonly List<VehicleController> All=new List<VehicleController>();
        public PlayerController Driver { get; private set; }
        public bool IsPolice;
        public bool traffic;
        public Vector3[] route;
        public float maximumSpeed=24, acceleration=10, steering=75, cruiseSpeed=7;
        public Transform[] wheels;
        public Renderer[] brakeLights;
        public float suspensionHeight=.84f, suspensionSpring=30, suspensionDamping=3;
        Rigidbody body;
        AudioSource engine, siren;
        float throttle, turn, squealTime;
        bool handbrake;
        int waypoint,navigationSlot;
        public float Speed => body?Vector3.Dot(body.linearVelocity,transform.forward):0;
        public Rigidbody Body => body;
        public Vector2 DriveInput => new Vector2(turn,throttle);
        public bool GroundContact { get; private set; }
        public bool IsUsable { get; private set; }=true;
        PhysicsMaterial chassisMaterial;
        float nextDust;
        void Awake()
        {
            body=GetComponent<Rigidbody>(); body.mass=1100; body.centerOfMass=new Vector3(0,-.3f,0);
            body.interpolation=RigidbodyInterpolation.Interpolate; body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            // Steering owns the heading. Collision torque must never become persistent steering.
            body.constraints=RigidbodyConstraints.FreezeRotation;
            body.angularDamping=12;
            // Ground grip is applied by the arcade controller. Collider friction must not pin the chassis to sidewalks.
            var friction=new PhysicsMaterial("Arcade chassis") {dynamicFriction=.02f,staticFriction=.02f,frictionCombine=PhysicsMaterialCombine.Minimum};
            GetComponent<Collider>().material=friction;chassisMaterial=friction;
            All.Add(this);navigationSlot=Random.Range(0,20);
        }
        void Start()
        {
            engine=gameObject.AddComponent<AudioSource>(); engine.clip=GameManager.Instance.audioManager.Engine; engine.loop=true; engine.spatialBlend=1; engine.minDistance=3; engine.maxDistance=35; engine.volume=.12f; engine.Play();
            if(IsPolice) {siren=gameObject.AddComponent<AudioSource>();siren.clip=GameManager.Instance.audioManager.Siren;siren.loop=true;siren.spatialBlend=1;siren.volume=.12f;siren.maxDistance=55;siren.Play();}
        }
        public void TakeControl(PlayerController player) { if(!IsUsable)return;Driver=player;traffic=false;body.angularVelocity=Vector3.zero; }
        public void ReleaseControl() {Driver=null;throttle=turn=0;handbrake=false;}
        public void DisableVehicle() {IsUsable=false;traffic=false;ReleaseControl();if(engine)engine.Stop();if(siren)siren.Stop();body.angularVelocity=Vector3.zero;}
        void Update()
        {
            if (!GameManager.Instance || !GameManager.Instance.Started || !IsUsable) return;
            if (Driver)
            {
                if (GameManager.Instance.Blocked) {throttle=turn=0;return;}
                throttle=GameInput.Move.y; turn=GameInput.Move.x; handbrake=GameInput.Held(Key.Space);
            }
            else if(IsPolice && GameManager.Instance.wanted.Level>0)
            {
                // Use the navigation mesh to choose the next corner rather than driving through blocks.
                if(Time.frameCount%20==navigationSlot || route==null || route.Length==0)
                {
                    var path=new NavMeshPath(); if(NavMesh.CalculatePath(transform.position,GameManager.Instance.player.Position,NavMesh.AllAreas,path)) {route=path.corners;waypoint=Mathf.Min(1,route.Length-1);}
                }
                Navigate(12);
            }
            else if(traffic) Navigate(cruiseSpeed);
            else {throttle=turn=0;handbrake=false;}
            if(engine) { engine.pitch=.65f+Mathf.Abs(Speed)*.06f; engine.volume=Driver?.24f:.09f; }
            if(siren) siren.volume=GameManager.Instance.wanted.Level>0?.12f:0;
            if(wheels!=null) foreach(var wheel in wheels) if(wheel) wheel.Rotate(Vector3.right,Speed*Time.deltaTime*120,Space.Self);
            if(Driver && handbrake && Mathf.Abs(Speed)>8 && Time.time>squealTime)
            {
                squealTime=Time.time+.2f; GameManager.Instance.audioManager.Play("skid",transform.position,.4f);
                EffectsPool.Instance.Impact(transform.position-transform.forward*1.5f,Vector3.up,new Color(.55f,.55f,.52f),.35f);
                EffectsPool.Instance.Skid(transform.position-transform.forward*1.3f-transform.right*.7f,transform.forward);
                EffectsPool.Instance.Skid(transform.position-transform.forward*1.3f+transform.right*.7f,transform.forward);
            }
            if(Driver && Mathf.Abs(Speed)>18) GameManager.Instance.followCamera.Shake(.015f);
            if(GroundContact && Mathf.Abs(Speed)>6 && Time.time>nextDust)
            {
                nextDust=Time.time+.12f;
                EffectsPool.Instance.Dust(transform.position-transform.forward*1.6f-Vector3.up*.5f,new Color(.65f,.59f,.46f),Mathf.Abs(throttle)>.1f?.3f:.16f);
            }
        }
        void Navigate(float desiredSpeed)
        {
            if(route==null || route.Length==0) {throttle=0;return;}
            Vector3 delta=route[waypoint]-transform.position;delta.y=0;
            if(delta.magnitude<5) {waypoint=(waypoint+1)%route.Length;delta=route[waypoint]-transform.position;delta.y=0;}
            float angle=Vector3.SignedAngle(transform.forward,delta,Vector3.up);turn=Mathf.Clamp(angle/35,-1,1);
            float goal=desiredSpeed*Mathf.Lerp(1,.35f,Mathf.Abs(turn));
            if(Physics.SphereCast(transform.position+Vector3.up*.3f+transform.forward*2.5f,.7f,transform.forward,out var obstacle,5,~(1<<2),QueryTriggerInteraction.Ignore)) goal=0;
            throttle=Mathf.Clamp((goal-Speed)*.4f,-1,1);handbrake=false;
            if(!Driver && !IsPolice && Vector3.Distance(transform.position,GameManager.Instance.player.Position)>110 && Speed<.2f && Time.frameCount%300==0 && SpawnVisibility.IsHidden(route[0],3))
            { body.position=route[0]+Vector3.up*.7f; body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;waypoint=1; }
        }
        void FixedUpdate()
        {
            if(!GameManager.Instance || !GameManager.Instance.Started || GameManager.Instance.Paused || !IsUsable) return;
            body.angularVelocity=Vector3.zero;
            bool grounded=Physics.Raycast(transform.position+Vector3.up*.25f,Vector3.down,1.5f,1,QueryTriggerInteraction.Ignore);GroundContact=grounded;
            if(!grounded) return;
            // Four ground probes support the chassis, giving an inexpensive arcade suspension.
            for(int wheel=0;wheel<4;wheel++)
            {
                Vector3 probe=transform.position+transform.right*(wheel%2==0?-.65f:.65f)+transform.forward*(wheel<2?1.45f:-1.45f);
                if(Physics.Raycast(probe+Vector3.up*.3f,Vector3.down,out var ground,suspensionHeight+.4f,1,QueryTriggerInteraction.Ignore))
                {
                    float compression=suspensionHeight+.3f-ground.distance;
                    float lift=Mathf.Max(0,compression*suspensionSpring-body.GetPointVelocity(probe).y*suspensionDamping);
                    body.AddForceAtPosition(Vector3.up*lift,probe,ForceMode.Acceleration);
                }
            }
            float speed=Speed;
            Vector3 velocity=body.linearVelocity;
            float lateral=Vector3.Dot(velocity,transform.right);
            body.AddForce(-transform.right*lateral*(handbrake?2.5f:9),ForceMode.Acceleration);
            body.AddForce(-new Vector3(velocity.x,0,velocity.z)*(Driver?.28f:1),ForceMode.Acceleration);
            if(handbrake) body.AddForce(-new Vector3(velocity.x,0,velocity.z)*3.5f,ForceMode.Acceleration);
            else
            {
                float force=throttle*acceleration;
                if(throttle*speed<0) force*=2.2f;
                if((speed<maximumSpeed && force>0)||(speed>-8 && force<0)) body.AddForce(transform.forward*force,ForceMode.Acceleration);
            }
            if(Mathf.Abs(speed)>.15f) body.MoveRotation(body.rotation*Quaternion.Euler(0,turn*steering*Mathf.Clamp01(Mathf.Abs(speed)/4)*Mathf.Sign(speed)*Time.fixedDeltaTime,0));
            if(brakeLights!=null) foreach(var light in brakeLights) if(light) light.material.SetColor("_EmissionColor",Color.red*((handbrake||throttle*speed<0)?3:.15f));
            if(transform.position.y<-10) {body.position=new Vector3(0,.7f,-48);body.linearVelocity=Vector3.zero;}
        }
        void OnCollisionEnter(Collision collision)
        {
            body.angularVelocity=Vector3.zero;
            if(collision.relativeVelocity.magnitude<3 || !GameManager.Instance) return;
            var npc=collision.collider.GetComponentInParent<NPCController>();
            if(npc && Driver) {npc.Damage(collision.relativeVelocity.magnitude*12); GameManager.Instance.wanted.ReportCrime(1);}
            if(Driver) {GameManager.Instance.followCamera.Shake(Mathf.Min(.3f,collision.relativeVelocity.magnitude*.012f)); GameManager.Instance.audioManager.Play("impact",transform.position,.5f);}
        }
        void OnCollisionStay(Collision collision) {body.angularVelocity=Vector3.zero;}
        void OnDestroy() {All.Remove(this);if(chassisMaterial)Destroy(chassisMaterial);}
    }
}

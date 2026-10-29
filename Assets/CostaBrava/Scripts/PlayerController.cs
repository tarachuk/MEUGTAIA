using UnityEngine;
using UnityEngine.InputSystem;

namespace CostaBrava
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Transform visual;
        public Animator animator;
        public PlayerHealth health;
        public PlayerCombat combat;
        public CharacterController controller;
        public VehicleController Vehicle { get; private set; }
        public float Speed { get; private set; }
        public bool Sprinting { get; private set; }
        public bool Grounded => controller.isGrounded;
        public Vector3 Position => Vehicle ? Vehicle.transform.position : transform.position;
        public float walkSpeed=2.5f, runSpeed=4.6f, sprintSpeed=7.5f, jumpHeight=1.3f;
        float vertical, stepTime;
        void Awake() { controller=GetComponent<CharacterController>(); }
        void Update()
        {
            var game=GameManager.Instance;
            if (!game || game.Blocked) return;
            if (Vehicle) { if (GameInput.Pressed(Key.E)) ExitVehicle(); return; }
            var input=GameInput.Move;
            Sprinting=GameInput.Held(Key.LeftShift) && input.y>0 && !combat.Aiming;
            float targetSpeed=Sprinting?sprintSpeed:(GameInput.Held(Key.LeftCtrl)?walkSpeed:runSpeed);
            Speed=Mathf.MoveTowards(Speed,input.magnitude*targetSpeed,15*Time.deltaTime);
            Vector3 forward=game.followCamera.transform.forward; forward.y=0; forward.Normalize();
            Vector3 right=game.followCamera.transform.right; right.y=0; right.Normalize();
            Vector3 motion=(forward*input.y+right*input.x).normalized;
            Vector3 facing=combat.Aiming?forward:motion;
            if (facing.sqrMagnitude>.01f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(facing),Time.deltaTime*12);
            if (controller.isGrounded && vertical<0) vertical=-2;
            if (controller.isGrounded && GameInput.Pressed(Key.Space)) { vertical=Mathf.Sqrt(jumpHeight*40); game.audioManager.Play("jump",transform.position); }
            vertical-=20*Time.deltaTime;
            controller.Move((motion*Speed+Vector3.up*vertical)*Time.deltaTime);
            if (animator) { animator.SetFloat("Speed",Speed,.15f,Time.deltaTime); animator.SetBool("Grounded",controller.isGrounded); animator.SetFloat("Vertical",vertical); }
            if (Speed>.2f && controller.isGrounded && Time.time>stepTime) { stepTime=Time.time+(Sprinting?.25f:.4f); game.audioManager.Footstep(transform.position); }
            if (transform.position.y<-5) health.Damage(200);
            if (GameInput.Pressed(Key.E)) TryEnterVehicle();
        }
        void TryEnterVehicle()
        {
            var game=GameManager.Instance;
            if (game.shop.Near || (game.weaponShop && game.weaponShop.Near) || game.missions.CanInteract) return;
            var car=NearestVehicle();
            if (car && Vector3.Distance(Position,car.transform.position)<4.2f && Mathf.Abs(car.Speed)<3) EnterVehicle(car);
        }
        public VehicleController NearestVehicle()
        {
            VehicleController best=null; float distance=4.2f;
            foreach(var car in VehicleController.All) { if (!car || !car.IsUsable || car.Driver || car.IsPolice) continue; float d=Vector3.Distance(Position,car.transform.position); if (d<distance) {distance=d;best=car;} }
            return best;
        }
        public void EnterVehicle(VehicleController car)
        {
            if(!car || !car.IsUsable)return;
            Vehicle=car; car.TakeControl(this); controller.enabled=false; visual.gameObject.SetActive(false); combat.SetWeaponVisible(false);
            if (GameManager.Instance.wanted.NearPolice(Position,30)) GameManager.Instance.wanted.ReportCrime(1);
            GameManager.Instance.hud.Notify("Ao volante", "W/S acelerar e frear · A/D direção · Espaço freio de mão · E sair");
        }
        public void ExitVehicle(bool force=false)
        {
            if (!Vehicle) return;
            if (!force && Mathf.Abs(Vehicle.Speed)>3) { GameManager.Instance.hud.Toast("Pare o carro antes de sair."); return; }
            var car=Vehicle;
            Vector3 exit=car.transform.position-car.transform.right*2.3f;
            if (Physics.CheckCapsule(exit+Vector3.up*.5f,exit+Vector3.up*1.5f,.35f,1<<0,QueryTriggerInteraction.Ignore)) exit=car.transform.position+car.transform.right*2.3f;
            if (!force && Physics.CheckCapsule(exit+Vector3.up*.5f,exit+Vector3.up*1.5f,.35f,1<<0,QueryTriggerInteraction.Ignore)) { GameManager.Instance.hud.Toast("A saída está bloqueada."); return; }
            car.ReleaseControl(); Vehicle=null; transform.position=new Vector3(exit.x,.6f,exit.z); vertical=0; Speed=0;
            visual.gameObject.SetActive(true); controller.enabled=true; combat.SetWeaponVisible(true);
            if(force)combat.CancelReload();
        }
        public void Teleport(Vector3 position) { controller.enabled=false; transform.position=position; controller.enabled=true; vertical=0; }
    }
}

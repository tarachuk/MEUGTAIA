using UnityEngine;

namespace CostaBrava
{
    [RequireComponent(typeof(Camera))]
    public class ThirdPersonCamera : MonoBehaviour
    {
        public PlayerController target;
        public LayerMask obstructionMask=1;
        public float sensitivity=.12f;
        public float ShakeAmplitude => shake;
        public bool ScopeActive { get; private set; }
        float yaw=0, pitch=20, shake, recoil;
        Vector3 pivot, pivotVelocity;
        Camera cam;
        int normalMask;
        void Awake() { cam=GetComponent<Camera>();normalMask=cam.cullingMask; }
        void Start() { pivot=target.Position+Vector3.up*1.6f; }
        public void Kick(float intensity) { recoil+=intensity; shake=Mathf.Max(shake,intensity*.02f); }
        public void Shake(float intensity) { shake=Mathf.Max(shake,intensity); }
        void LateUpdate()
        {
            if (!target || !GameManager.Instance) return;
            var game=GameManager.Instance;
            bool aiming=target.combat.Aiming && !target.Vehicle;
            ScopeActive=target.combat.Scoped;
            if (!game.Blocked && Cursor.lockState==CursorLockMode.Locked)
            {
                var look=GameInput.Look; float factor=target.combat.Scoped?.22f:(aiming?.65f:1);
                yaw+=look.x*sensitivity*factor; pitch=Mathf.Clamp(pitch-look.y*sensitivity*factor,-25,65);
            }
            recoil=Mathf.Lerp(recoil,0,Time.unscaledDeltaTime*9); shake=Mathf.MoveTowards(shake,0,Time.unscaledDeltaTime*.8f);
            pivot=Vector3.SmoothDamp(pivot,target.Position+Vector3.up*(target.Vehicle?1.5f:1.55f),ref pivotVelocity,.065f,Mathf.Infinity,Time.unscaledDeltaTime);
            var rotation=Quaternion.Euler(pitch-recoil,yaw,0);
            float distance=target.Vehicle?7.3f:(aiming?2.5f:4.8f);
            Vector3 origin=pivot+rotation*Vector3.right*(aiming?.6f:.35f);
            if (Physics.SphereCast(origin,.22f,rotation*Vector3.back,out var hit,distance,obstructionMask,QueryTriggerInteraction.Ignore)) distance=Mathf.Max(.35f,hit.distance-.1f);
            transform.position=origin+rotation*Vector3.back*distance+Random.insideUnitSphere*shake;
            transform.rotation=rotation;
            cam.cullingMask=target.combat.Scoped?normalMask&~(1<<2):normalMask;
            if(target.combat.Scoped)transform.position=pivot+Vector3.up*.1f+rotation*Vector3.forward*.15f;
            float fov=target.combat.Scoped?target.combat.Weapon.scopeFov:(aiming?49:(target.Vehicle?60+Mathf.Abs(target.Vehicle.Speed)*.65f+Mathf.Max(0,target.Vehicle.DriveInput.y)*2:(target.Sprinting?68:60)));
            cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,fov,Time.unscaledDeltaTime*6);
        }
    }
}

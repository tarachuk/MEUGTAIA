using UnityEngine;
using UnityEngine.InputSystem;

namespace CostaBrava
{
    public class PlayerCombat : MonoBehaviour
    {
        public WeaponDefinition[] weapons;
        public GameObject[] models;
        public Transform muzzle;
        public PlayerController player;
        public int Selected { get; private set; }
        public bool Equipped { get; private set; }
        public bool Aiming => Equipped && GameInput.Aim && !player.Vehicle && !GameManager.Instance.Blocked;
        public bool Reloading => reloadEnd>0;
        public bool Scoped => Aiming && Weapon.scoped;
        bool riflePurchased;
        public float HitMarker { get; private set; }
        public WeaponDefinition Weapon => weapons[Selected];
        int[] magazines, reserves;
        float nextShot, reloadEnd;
        public int Magazine => magazines!=null?magazines[Selected]:0;
        public int Reserve => reserves!=null?reserves[Selected]:0;
        void Awake() { magazines=new int[weapons.Length]; reserves=new int[weapons.Length]; Restock(); SetWeaponVisible(true); }
        public bool Owns(int index) => index>=0 && index<weapons.Length && (index<2 || riflePurchased);
        public void Restock() { for(int i=0;i<weapons.Length;i++)if(Owns(i)) {magazines[i]=weapons[i].magazineSize; reserves[i]=Mathf.Max(reserves[i],weapons[i].startingReserve);} reloadEnd=0; }
        public void AddAmmo(int weapon,int amount) { if(Owns(weapon))reserves[weapon]+=amount; }
        internal void UnlockRifle() {if(weapons.Length<3)return;riflePurchased=true;magazines[2]=weapons[2].magazineSize;reserves[2]=weapons[2].startingReserve;Select(2);}
        public void CancelReload() {reloadEnd=0;}
        public void Holster() {Equipped=false;CancelReload();SetWeaponVisible(true);}
        public void SetWeaponVisible(bool visible) { for(int i=0;i<models.Length;i++) if(models[i]) models[i].SetActive(visible && Equipped && i==Selected); }
        void Update()
        {
            HitMarker=Mathf.MoveTowards(HitMarker,0,Time.deltaTime*4);
            if (reloadEnd>0 && Time.time>=reloadEnd) { int count=Mathf.Min(Weapon.magazineSize-magazines[Selected],reserves[Selected]); magazines[Selected]+=count; reserves[Selected]-=count; reloadEnd=0; }
            var game=GameManager.Instance; if (!game || game.Blocked || player.Vehicle) return;
            if (GameInput.Pressed(Key.Digit1)) Select(0);
            if (GameInput.Pressed(Key.Digit2)) Select(1);
            if (GameInput.Pressed(Key.Digit3)) Select(2);
            if (GameInput.Pressed(Key.Q)) { Equipped=!Equipped; SetWeaponVisible(true); }
            if (GameInput.Pressed(Key.R)) Reload();
            if (Aiming) NPCController.AlertAll(player.Position,15);
            if (Equipped && (Weapon.automatic?GameInput.Fire:GameInput.FirePressed) && Cursor.lockState==CursorLockMode.Locked) Shoot();
        }
        public bool Select(int index)
        {
            if(!Owns(index)) {GameManager.Instance.hud.Toast("Rifle bloqueado: compre por $100 na loja FAROL, no bairro leste.");return false;}
            Selected=index;Equipped=true;reloadEnd=0;SetWeaponVisible(true);return true;
        }
        public void Reload() { if (!Equipped || Reloading || Magazine==Weapon.magazineSize || Reserve<=0) return; reloadEnd=Time.time+Weapon.reloadTime; GameManager.Instance.audioManager.Play("reload",transform.position); }
        public void Shoot()
        {
            if (!Equipped || !Owns(Selected) || player.Vehicle || GameManager.Instance.Blocked || Reloading || Time.time<nextShot) return;
            if (Magazine<=0) { Reload(); return; }
            magazines[Selected]--; nextShot=Time.time+Weapon.fireInterval;
            var game=GameManager.Instance; game.wanted.ReportCrime(.35f); NPCController.AlertAll(player.Position,35);
            game.audioManager.Play("shot",transform.position,Weapon.automatic?.65f:1);
            game.followCamera.Kick(Weapon.recoil);
            var ray=Camera.main.ViewportPointToRay(new Vector3(.5f,.5f));
            Vector3 end=ray.GetPoint(Weapon.range);
            // Layer 2 contains the player and cosmetic markers, excluded from ballistics.
            if (Physics.Raycast(ray,out var hit,Weapon.range,~(1<<2),QueryTriggerInteraction.Ignore))
            {
                end=hit.point;
                var npc=hit.collider.GetComponentInParent<NPCController>();
                if (npc) { npc.Damage(Weapon.damage); HitMarker=1; game.audioManager.Play("hit",end); }
                var vehicle=hit.collider.GetComponentInParent<VehicleDamage>();if(vehicle) {vehicle.Damage(Weapon.damage);HitMarker=1;}
                var prop=hit.collider.GetComponentInParent<DestructibleProp>();if(prop) {prop.Damage(Weapon.damage,ray.direction,hit.point);HitMarker=1;}
                if (hit.rigidbody) hit.rigidbody.AddForceAtPosition(ray.direction*18,hit.point,ForceMode.Impulse);
                var surface=hit.collider.GetComponent<ImpactSurface>();
                EffectsPool.Instance.Impact(end,hit.normal,npc?new Color(1,.65f,.25f):(surface?surface.color:new Color(.72f,.68f,.56f)));
            }
            EffectsPool.Instance.Trace(muzzle?muzzle.position:transform.position+Vector3.up*1.4f,end);
            EffectsPool.Instance.Impact(muzzle?muzzle.position:transform.position+Vector3.up*1.4f,transform.forward,new Color(1,.83f,.26f),.08f);
        }
    }
}

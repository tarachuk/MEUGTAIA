using UnityEngine;
using System.Collections.Generic;
namespace CostaBrava
{
    // Complements the CC0 locomotion clips with smooth aim, jump, fall, landing and turn poses.
    public class CharacterPose : MonoBehaviour
    {
        public PlayerController player;
        Transform leftArm,rightArm,leftLeg,rightLeg,torso;
        float weaponBlend,airBlend,lastYaw,lean,landing;
        bool wasGrounded;
        PlayerMelee melee;
        readonly Dictionary<Transform,Quaternion> applied=new Dictionary<Transform,Quaternion>();
        void Start()
        {
            foreach(var bone in player.visual.GetComponentsInChildren<Transform>())
            {
                if(bone.name=="arm-left")leftArm=bone;if(bone.name=="arm-right")rightArm=bone;
                if(bone.name=="leg-left")leftLeg=bone;if(bone.name=="leg-right")rightLeg=bone;if(bone.name=="torso")torso=bone;
            }
            lastYaw=player.transform.eulerAngles.y;wasGrounded=true;melee=player.GetComponent<PlayerMelee>();
        }
        void Update()
        {
            // Remove last frame's offsets before Animator evaluates, including bones with constant clips.
            foreach(var pose in applied)if(pose.Key)pose.Key.localRotation*=Quaternion.Inverse(pose.Value);
            applied.Clear();
        }
        void LateUpdate()
        {
            if(!player.visual.gameObject.activeSelf || player.health.Dead)return;
            float dt=Time.deltaTime;if(dt<=0)return;
            bool grounded=player.Grounded;
            if(grounded && !wasGrounded)landing=1;wasGrounded=grounded;landing=Mathf.MoveTowards(landing,0,dt*6);
            airBlend=Mathf.MoveTowards(airBlend,grounded?0:1,dt*8);
            weaponBlend=Mathf.MoveTowards(weaponBlend,player.combat.Equipped?1:0,dt*7);
            float yaw=player.transform.eulerAngles.y;float turn=Mathf.DeltaAngle(lastYaw,yaw);lastYaw=yaw;
            lean=Mathf.Lerp(lean,Mathf.Clamp(-turn*1.8f,-12,12)*Mathf.Clamp01(player.Speed/4),dt*8);
            // Arms share a consistent forward axis with the weapon grip.
            float strike=melee && melee.Attacking?Mathf.Sin(melee.AnimationProgress*Mathf.PI):0;
            bool kick=melee && melee.Kicking;
            Offset(leftArm,new Vector3(-83*weaponBlend-18*airBlend-(kick?0:105*strike),0,12*airBlend));
            Offset(rightArm,new Vector3(-65*(player.combat.Selected>=1?weaponBlend:0)-18*airBlend-(kick?20:((melee && melee.Combo==2)?95:20))*strike,0,-12*airBlend));
            Offset(leftLeg,new Vector3(-25*airBlend-(kick?95*strike:0),0,0));Offset(rightLeg,new Vector3(18*airBlend,0,0));
            Offset(torso,new Vector3(landing*10,0,lean));
        }
        void Offset(Transform bone,Vector3 angles) {if(bone) {var rotation=Quaternion.Euler(angles);bone.localRotation*=rotation;applied[bone]=rotation;}}
    }
}

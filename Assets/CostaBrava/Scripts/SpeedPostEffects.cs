using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CostaBrava
{
    public class SpeedPostEffects : MonoBehaviour
    {
        public float Intensity => blur?blur.intensity.value:0;
        MotionBlur blur;
        Volume volume;
        VolumeProfile profile;
        void Start()
        {
            volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=40;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();blur=profile.Add<MotionBlur>(true);blur.intensity.value=0;volume.sharedProfile=profile;
            var camera=GetComponent<Camera>();camera.depthTextureMode|=DepthTextureMode.Depth|DepthTextureMode.MotionVectors;
        }
        void Update()
        {
            if(!blur || !GameManager.Instance)return;
            var game=GameManager.Instance;var car=game.player.Vehicle;float value=0;
            if(car && !game.Blocked)value=Mathf.InverseLerp(6,24,Mathf.Abs(car.Speed))*.4f*Mathf.Lerp(.4f,1,Mathf.Max(0,car.DriveInput.y));
            blur.intensity.value=Mathf.MoveTowards(blur.intensity.value,value,Time.unscaledDeltaTime);
        }
        void OnDestroy() {if(profile)Destroy(profile);}
    }
}

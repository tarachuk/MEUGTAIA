using UnityEngine;
namespace CostaBrava
{
    public class AutomaticLights : MonoBehaviour
    {
        public Light[] lights;
        public Renderer fixture;
        public bool vehicle;
        VehicleController car;
        MaterialPropertyBlock block;
        float next;
        void Start()
        {
            car=GetComponent<VehicleController>();block=new MaterialPropertyBlock();
            if(vehicle && (lights==null || lights.Length==0))
            {
                lights=new Light[2];for(int i=0;i<2;i++)
                {
                    var light=new GameObject("Farol automático").AddComponent<Light>();light.transform.SetParent(transform,false);light.transform.localPosition=new Vector3(i==0?-.65f:.65f,.15f,1.95f);light.transform.localRotation=Quaternion.Euler(7,0,0);
                    light.type=LightType.Spot;light.range=32;light.spotAngle=62;light.innerSpotAngle=35;light.intensity=5;light.color=new Color(1,.91f,.7f);light.shadows=LightShadows.None;lights[i]=light;
                }
            }
            Refresh();
        }
        void Update() {if(Time.time>=next) {next=Time.time+.25f;Refresh();}}
        public void Refresh()
        {
            bool night=DayNightCycle.Instance && DayNightCycle.Instance.IsNight;
            bool working=!vehicle || !car || car.IsUsable;
            float distance=GameManager.Instance?Vector3.Distance(transform.position,GameManager.Instance.player.Position):0;
            if(lights!=null)foreach(var light in lights)if(light)light.enabled=night && working && distance<(vehicle?100:65);
            if(fixture)
            {
                fixture.GetPropertyBlock(block);block.SetColor("_BaseColor",night?new Color(1,.82f,.43f)*2:new Color(.5f,.52f,.49f));block.SetColor("_EmissionColor",night?new Color(1,.65f,.25f)*3:Color.black);fixture.SetPropertyBlock(block);
            }
        }
    }
}

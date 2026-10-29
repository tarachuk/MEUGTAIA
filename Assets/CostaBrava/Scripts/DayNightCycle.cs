using UnityEngine;
using UnityEngine.InputSystem;
namespace CostaBrava
{
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance { get; private set; }
        public Light sun;
        [Range(0,24)] public float hour=15;
        public float cycleSeconds=720;
        public bool IsNight => hour>=18.5f || hour<6;
        public string Clock => Mathf.FloorToInt(hour).ToString("00")+":"+Mathf.FloorToInt(hour%1*60).ToString("00");
        Material sky;
        Light moon;
        void Awake()
        {
            Instance=this;if(RenderSettings.skybox) {sky=new Material(RenderSettings.skybox);RenderSettings.skybox=sky;}
            moon=new GameObject("Luar").AddComponent<Light>();moon.transform.SetParent(transform);moon.type=LightType.Directional;moon.color=new Color(.4f,.57f,1);moon.shadows=LightShadows.None;Apply();
        }
        void Update()
        {
            var game=GameManager.Instance;if(!game || !game.Started || game.Paused)return;
            if(!game.Blocked && GameInput.Pressed(Key.T))SetHour(hour+3);
            hour=Mathf.Repeat(hour+Time.deltaTime*24/Mathf.Max(60,cycleSeconds),24);Apply();
        }
        public void SetHour(float value) {hour=Mathf.Repeat(value,24);Apply();}
        void Apply()
        {
            float elevation=Mathf.Sin((hour-6)/24*Mathf.PI*2);
            float daylight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.15f,.3f,elevation));
            if(sun) {sun.transform.rotation=Quaternion.Euler(hour/24*360-90,-35,0);sun.intensity=1.7f*daylight;sun.color=Color.Lerp(new Color(1,.51f,.28f),new Color(1,.92f,.79f),Mathf.Clamp01(elevation*2));}
            if(moon) {moon.transform.rotation=Quaternion.Euler(hour/24*360+90,-35,0);moon.intensity=(1-daylight)*.4f;}
            RenderSettings.ambientLight=Color.Lerp(new Color(.12f,.17f,.29f),new Color(.65f,.73f,.8f),daylight);
            RenderSettings.fogColor=Color.Lerp(new Color(.035f,.055f,.105f),new Color(.53f,.75f,.82f),daylight);
            if(sky && sky.HasProperty("_Exposure"))sky.SetFloat("_Exposure",Mathf.Lerp(.12f,1.15f,daylight));
        }
        void OnDestroy() {if(Instance==this)Instance=null;if(sky)Destroy(sky);}
    }
}

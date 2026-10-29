using UnityEngine;

namespace CostaBrava
{
    public class EffectsPool : MonoBehaviour
    {
        public static EffectsPool Instance { get; private set; }
        public int DustEmissions { get; private set; }
        ParticleSystem[] particles=new ParticleSystem[32];
        LineRenderer[] lines=new LineRenderer[40];
        float[] lineUntil=new float[40];
        GameObject[] skids=new GameObject[100];
        int particleIndex,lineIndex,skidIndex;
        Material particleMaterial,lineMaterial,skidMaterial;
        void Awake()
        {
            Instance=this;particleMaterial=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")??Shader.Find("Sprites/Default"));
            lineMaterial=new Material(Shader.Find("Sprites/Default"));skidMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));skidMaterial.color=new Color(.13f,.15f,.16f);
            for(int i=0;i<particles.Length;i++)
            {
                var obj=new GameObject("Impact pool "+i);obj.transform.SetParent(transform);var p=obj.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=p.main;main.playOnAwake=false;main.loop=false;main.duration=.6f;main.startLifetime=.4f;main.startSpeed=2;main.startSize=.06f;main.gravityModifier=1;main.maxParticles=96;main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=p.emission;emission.enabled=false;var shape=p.shape;shape.enabled=false;
                p.GetComponent<ParticleSystemRenderer>().sharedMaterial=particleMaterial;particles[i]=p;p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            for(int i=0;i<lines.Length;i++)
            {
                var obj=new GameObject("Tracer pool "+i);obj.transform.SetParent(transform);var l=obj.AddComponent<LineRenderer>();l.sharedMaterial=lineMaterial;l.positionCount=2;l.startWidth=.025f;l.endWidth=.01f;l.startColor=new Color(1,.84f,.35f);l.endColor=new Color(1,.95f,.7f);l.enabled=false;lines[i]=l;
            }
            for(int i=0;i<skids.Length;i++)
            {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Quad);obj.name="Skid pool "+i;obj.transform.SetParent(transform);Destroy(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=skidMaterial;obj.layer=2;obj.SetActive(false);skids[i]=obj;
            }
        }
        public void Impact(Vector3 point,Vector3 normal,Color color,float size=.13f)
        {
            var p=particles[particleIndex++%particles.Length];p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);p.transform.position=point+normal*.03f;p.Play();
            for(int n=0;n<12;n++) {var e=new ParticleSystem.EmitParams {position=point+normal*.03f,velocity=normal*1.7f+Random.insideUnitSphere*2,startColor=color,startSize=size*Random.Range(.3f,1),startLifetime=Random.Range(.1f,.45f)};p.Emit(e,1);}
        }
        public void Trace(Vector3 start,Vector3 end)
        {
            int i=lineIndex++%lines.Length;lines[i].SetPosition(0,start);lines[i].SetPosition(1,end);lines[i].enabled=true;lineUntil[i]=Time.time+.055f;
        }
        public void Skid(Vector3 point,Vector3 forward)
        {
            var obj=skids[skidIndex++%skids.Length];obj.transform.position=new Vector3(point.x,.015f,point.z);obj.transform.rotation=Quaternion.LookRotation(Vector3.up,forward);obj.transform.localScale=new Vector3(.16f,.5f,1);obj.SetActive(true);
        }
        public void Dust(Vector3 point,Color color,float size=.3f)
        {
            DustEmissions++;
            var p=particles[particleIndex++%particles.Length];p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);p.transform.position=point;p.Play();
            color.a=.35f;
            for(int i=0;i<6;i++)p.Emit(new ParticleSystem.EmitParams {position=point+Random.insideUnitSphere*.25f,velocity=Vector3.up*.5f+Random.insideUnitSphere*.5f,startColor=color,startSize=size,startLifetime=.65f},1);
        }
        public void Explosion(Vector3 point)
        {
            var p=particles[particleIndex++%particles.Length];p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);p.transform.position=point;p.Play();
            for(int i=0;i<72;i++)p.Emit(new ParticleSystem.EmitParams {position=point+Random.insideUnitSphere*.7f,velocity=Random.insideUnitSphere*9+Vector3.up*3,startColor=Color.Lerp(new Color(1,.18f,.02f),new Color(1,.85f,.2f),Random.value),startSize=Random.Range(.2f,.7f),startLifetime=Random.Range(.3f,1.1f)},1);
            Dust(point+Vector3.up,new Color(.16f,.15f,.14f),2);
        }
        void Update() {for(int i=0;i<lines.Length;i++)if(lines[i].enabled && Time.time>lineUntil[i])lines[i].enabled=false;}
        void OnDestroy() {if(Instance==this)Instance=null;Destroy(particleMaterial);Destroy(lineMaterial);Destroy(skidMaterial);}
    }
}

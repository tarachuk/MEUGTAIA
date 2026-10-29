using UnityEngine;
using UnityEngine.AI;

namespace CostaBrava
{
    public class WantedSystem : MonoBehaviour
    {
        public GameObject policePrefab, policeCarPrefab;
        public int Level { get; private set; }
        public float EscapeProgress { get; private set; }
        public float escapeSeconds=18;
        public bool spawnResponders=true;
        float heat,lastSeen,nextSpawn,nextCar;
        public Vector3 LastSpawnPosition { get; private set; }
        public int SpawnCount { get; private set; }
        public bool Searching => Level>0 && Time.time-lastSeen>3;
        public void ReportCrime(float severity)
        {
            int previous=Level;heat=Mathf.Min(3,heat+severity);Level=Mathf.CeilToInt(heat);lastSeen=Time.time;
            if(previous==0) {nextSpawn=Time.time+2.5f;nextCar=Time.time+7;}
            if(previous<Level) {GameManager.Instance.hud.Toast("ALERTA POLICIAL · "+Level+" estrela(s)");GameManager.Instance.audioManager.Play("alert",GameManager.Instance.player.Position);}
        }
        public void Seen() {lastSeen=Time.time;}
        public void SetLevel(int level) {heat=level;Level=level;lastSeen=Time.time;nextSpawn=Time.time+2.5f;nextCar=Time.time+7;}
        public bool NearPolice(Vector3 position,float radius) {foreach(var npc in NPCController.All) if(npc && npc.type==NPCType.Police && !npc.Dead && Vector3.Distance(npc.transform.position,position)<radius)return true;return false;}
        void Update()
        {
            var game=GameManager.Instance;if(!game || game.Blocked || Level==0) return;
            EscapeProgress=Mathf.Clamp01((Time.time-lastSeen)/escapeSeconds);
            if(EscapeProgress>=1) {Clear();game.hud.Toast("Você despistou a polícia.");return;}
            if(!spawnResponders)return;
            int active=0;
            foreach(var npc in NPCController.All) if(npc && npc.type==NPCType.Police && !npc.Dead)active++;
            if(active<2+Level && Time.time>nextSpawn && !Searching)
            {
                nextSpawn=Time.time+1.5f;
                if(TryFindFootSpawn(out var position)) {Instantiate(policePrefab,position,Quaternion.identity);LastSpawnPosition=position;SpawnCount++;nextSpawn=Time.time+7;}
            }
            if(Level>=2 && Time.time>nextCar && !Searching)
            {
                bool exists=false;foreach(var car in VehicleController.All) if(car && car.IsPolice)exists=true;
                nextCar=Time.time+2;
                if(!exists && TryFindCarSpawn(out var position,out var rotation)) {Instantiate(policeCarPrefab,position,rotation);LastSpawnPosition=position;SpawnCount++;nextCar=Time.time+20;}
            }
            // Retire dead officers and pursuers far behind the player to bound population.
            for(int i=NPCController.All.Count-1;i>=0;i--)
            {
                var npc=NPCController.All[i];if(npc && npc.type==NPCType.Police && npc.responder && (npc.Dead || Vector3.Distance(npc.transform.position,game.player.Position)>90))Destroy(npc.gameObject);
            }
        }
        public bool TryFindFootSpawn(out Vector3 position)
        {
            position=default;var player=GameManager.Instance.player.Position;
            for(int tries=0;tries<36;tries++)
            {
                Vector2 ring=Random.insideUnitCircle.normalized*Random.Range(45f,72f);var candidate=player+new Vector3(ring.x,0,ring.y);
                if(!NavMesh.SamplePosition(candidate,out var spot,6,NavMesh.AllAreas))continue;
                float distance=Vector3.Distance(spot.position,player);if(distance<38 || distance>85 || !SpawnVisibility.IsHidden(spot.position,1.3f))continue;
                if(Physics.CheckCapsule(spot.position+Vector3.up*.5f,spot.position+Vector3.up*1.4f,.3f,1,QueryTriggerInteraction.Ignore))continue;
                if(Physics.CheckSphere(spot.position+Vector3.up,2,(1<<8)|(1<<9),QueryTriggerInteraction.Ignore))continue;
                var path=new NavMeshPath();if(!NavMesh.CalculatePath(spot.position,player,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete)continue;
                position=spot.position;return true;
            }
            return false;
        }
        public bool TryFindCarSpawn(out Vector3 position,out Quaternion rotation)
        {
            position=default;rotation=Quaternion.identity;var player=GameManager.Instance.player.Position;
            for(int i=0;i<40;i++)
            {
                bool vertical=Random.value>.5f;float[] xs={-66,-22,22,66,110};float[] zs={-48,0,48};
                Vector3 p=vertical?new Vector3(xs[Random.Range(0,xs.Length)]+2,0,Random.Range(-40f,40f)):new Vector3(Random.Range(-60f,106f),0,zs[Random.Range(0,zs.Length)]+2);
                float d=Vector3.Distance(p,player);if(d<50 || d>105 || !SpawnVisibility.IsHidden(p,3))continue;
                if(Physics.CheckBox(p+Vector3.up*.8f,new Vector3(1,.45f,2.3f),Quaternion.identity,(1<<0)|(1<<8)|(1<<9),QueryTriggerInteraction.Ignore))continue;
                position=p+Vector3.up*.9f;rotation=Quaternion.Euler(0,vertical?(player.z>p.z?0:180):(player.x>p.x?90:270),0);return true;
            }
            return false;
        }
        public void Clear()
        {
            heat=0;Level=0;EscapeProgress=0;
            for(int i=NPCController.All.Count-1;i>=0;i--) if(NPCController.All[i] && NPCController.All[i].type==NPCType.Police && NPCController.All[i].responder) Destroy(NPCController.All[i].gameObject);
            for(int i=VehicleController.All.Count-1;i>=0;i--) if(VehicleController.All[i] && VehicleController.All[i].IsPolice)Destroy(VehicleController.All[i].gameObject);
        }
    }
}

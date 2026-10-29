using UnityEngine;
using UnityEngine.InputSystem;

namespace CostaBrava
{
    public class MissionSystem : MonoBehaviour
    {
        public Transform[] starts;
        public Transform delivery, yard, package, safe;
        public VehicleController deliveryVan;
        public GameObject replacementVanPrefab;
        public NPCController[] enemies;
        public GameObject packageVisual;
        public int Active { get; private set; }=-1;
        public int Stage { get; private set; }
        public bool[] Completed { get; private set; }=new bool[3];
        public string Objective { get; private set; }="Encontre um marcador dourado e pressione E para iniciar.";
        public string[] Names={"01 / Entrega expressa","02 / Carga perdida","03 / Última saída"};
        public int CompleteCount { get {int count=0;foreach(bool complete in Completed)if(complete)count++;return count;} }
        public bool CanInteract => Active==-1 && NearestStart()>=0;
        void Start() {SetEnemiesActive(false);}
        void SetEnemiesActive(bool active)
        {
            foreach(var enemy in enemies)if(enemy) {enemy.gameObject.SetActive(active);if(active)enemy.ResetNPC();}
        }
        public Vector3 Target
        {
            get
            {
                if(Active==0) return Stage==0?deliveryVan.transform.position:delivery.position;
                if(Active==1) return Stage==0?yard.position:(Stage==1?package.position:safe.position);
                if(Active==2) return safe.position;
                int i=NearestStart();if(i>=0)return starts[i].position;
                for(int n=0;n<3;n++)if(!Completed[n])return starts[n].position;
                return safe.position;
            }
        }
        int NearestStart()
        {
            if(!GameManager.Instance || GameManager.Instance.player.Vehicle)return -1;
            for(int i=0;i<starts.Length;i++)if(!Completed[i] && Vector3.Distance(GameManager.Instance.player.Position,starts[i].position)<3)return i;
            return -1;
        }
        void Update()
        {
            var game=GameManager.Instance;if(!game || game.Blocked)return;
            for(int i=0;i<starts.Length;i++) starts[i].gameObject.SetActive(!Completed[i] && Active<0);
            packageVisual.SetActive(Active==1 && Stage==1);
            if(Active<0) {int i=NearestStart();if(i>=0 && GameInput.Pressed(Key.E))StartMission(i);return;}
            if(Active==0)
            {
                if(Stage==0 && game.player.Vehicle==deliveryVan) {Stage=1;SetObjective("Leve a van até o Café Maré. Estacione no marcador azul.");}
                if(Stage==1 && game.player.Vehicle==deliveryVan && Vector3.Distance(game.player.Position,delivery.position)<5 && Mathf.Abs(deliveryVan.Speed)<2) Complete(300);
            }
            else if(Active==1)
            {
                if(Stage==0) {int alive=0;foreach(var enemy in enemies)if(!enemy.Dead)alive++;Objective="Elimine os guardas no pátio do porto ("+(enemies.Length-alive)+"/"+enemies.Length+").";if(alive==0) {Stage=1;SetObjective("Pegue a carga laranja no pátio do porto.");}}
                if(Stage==1 && !game.player.Vehicle && Vector3.Distance(game.player.Position,package.position)<2.5f) {Stage=2;game.wanted.ReportCrime(1);SetObjective("Carga recuperada! Despiste a polícia e volte ao abrigo verde.");game.audioManager.Play("reward",game.player.Position);}
                if(Stage==2 && game.wanted.Level==0 && Vector3.Distance(game.player.Position,safe.position)<4) Complete(500);
            }
            else if(Active==2)
            {
                if(Stage==0 && game.wanted.Level==0) {Stage=1;SetObjective("Sem perseguidores. Vá até o abrigo verde.");}
                if(Stage==1 && game.wanted.Level==0 && Vector3.Distance(game.player.Position,safe.position)<4) Complete(400);
            }
        }
        public void StartMission(int index)
        {
            if(Active>=0 || Completed[index])return;
            if(index==0 && !deliveryVan.IsUsable)
            {
                if(!replacementVanPrefab)return;
                bool placed=false;
                foreach(var spot in new[]{new Vector3(-42,.9f,-42),new Vector3(-42,.9f,-48),new Vector3(-34,.9f,-48),new Vector3(-50,.9f,-48)})
                {
                    if(Physics.CheckBox(spot,new Vector3(1,.45f,2.3f),Quaternion.Euler(0,90,0),(1<<0)|(1<<8)|(1<<9),QueryTriggerInteraction.Ignore))continue;
                    deliveryVan=Instantiate(replacementVanPrefab,spot,Quaternion.Euler(0,90,0)).GetComponent<VehicleController>();placed=true;break;
                }
                if(!placed) {GameManager.Instance.hud.Toast("Libere uma vaga na rua da garagem para receber a van de serviço.");return;}
            }
            Active=index;Stage=0;
            if(index==0)SetObjective("Entre na van amarela da garagem (E).");
            if(index==1) {SetEnemiesActive(true);SetObjective("Vá ao pátio do porto e elimine os três guardas.");}
            if(index==2) {GameManager.Instance.wanted.SetLevel(2);SetObjective("Fuja! Fique 18 segundos fora da visão policial.");}
            GameManager.Instance.hud.Notify(Names[index],Objective);
        }
        void SetObjective(string text) {Objective=text;GameManager.Instance.hud.Toast(text);}
        void Complete(int reward)
        {
            if(Active==1)SetEnemiesActive(false);
            Completed[Active]=true;Active=-1;Stage=0;GameManager.Instance.money.Add(reward);
            Objective=CompleteCount==3?"Costa Brava é sua! Continue explorando e aproveite as lojas.":"Missão concluída. Encontre outro marcador dourado.";
            GameManager.Instance.hud.Notify("MISSÃO CONCLUÍDA", "+$"+reward+" · "+CompleteCount+"/3 missões"+(CompleteCount==3?" · CAMPANHA COMPLETA":""));
        }
        public void OnPlayerDied() {SetEnemiesActive(false);Active=-1;Stage=0;Objective="Tente novamente nos marcadores dourados. Seu progresso concluído foi mantido.";}
        public void OnVehicleDestroyed(VehicleController car)
        {
            if(car!=deliveryVan || Active!=0)return;
            Active=-1;Stage=0;Objective="A van foi destruída. Volte ao marcador para tentar com outra van.";GameManager.Instance.hud.Notify("ENTREGA INTERROMPIDA",Objective);
        }
    }
}

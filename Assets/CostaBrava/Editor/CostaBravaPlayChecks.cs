using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CostaBrava.Editor
{
    // Integration checks use a temporary virtual keyboard; they never change the saved scene.
    [InitializeOnLoad]
    public static partial class CostaBravaPlayChecks
    {
        static IEnumerator steps;
        static double next;
        static Keyboard keyboard;
        static KeyboardState heldKeys;
        static readonly List<string> report=new List<string>();
        static readonly List<string> runtimeErrors=new List<string>();
        static bool requested;
        static InputSettings.BackgroundBehavior previousBackground;
        static CostaBravaPlayChecks() {EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=ModeChanged;}
        [MenuItem("Tools/Costa Brava/Testar ciclo de jogo")]
        public static void Run() {File.WriteAllText("Library/CostaBravaRunChecks.flag","run");}
        static void Tick()
        {
            if(!EditorApplication.isPlaying && !EditorApplication.isCompiling && File.Exists("Library/CostaBravaRunChecks.flag"))
            {File.Delete("Library/CostaBravaRunChecks.flag");requested=true;report.Clear();runtimeErrors.Clear();Application.logMessageReceived-=Capture;Application.logMessageReceived+=Capture;EditorApplication.isPlaying=true;return;}
            if(keyboard!=null)keyboard.MakeCurrent();
            if(steps==null || EditorApplication.timeSinceStartup<next)return;
            try
            {
                if(!steps.MoveNext()) {Finish();return;}
                next=EditorApplication.timeSinceStartup+(steps.Current is float duration?duration:.1f);
            }
            catch(Exception e) {report.Add("FAIL · "+e);Finish();}
        }
        static void ModeChanged(PlayModeStateChange change)
        {
            if(change==PlayModeStateChange.EnteredPlayMode && requested)
            {
                requested=false;Application.logMessageReceived-=Capture;Application.logMessageReceived+=Capture;
                previousBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                keyboard=InputSystem.AddDevice<Keyboard>("CostaBrava test keyboard");heldKeys=new KeyboardState();InputSystem.onBeforeUpdate+=InjectInput;InputSystem.onAfterUpdate+=UseTestKeyboard;steps=Checks();next=EditorApplication.timeSinceStartup+1;
            }
            if(change==PlayModeStateChange.ExitingPlayMode && keyboard!=null) {InputSystem.onBeforeUpdate-=InjectInput;InputSystem.onAfterUpdate-=UseTestKeyboard;InputSystem.RemoveDevice(keyboard);keyboard=null;steps=null;Application.logMessageReceived-=Capture;}
        }
        static void Capture(string message,string trace,LogType type) {if(type==LogType.Exception || type==LogType.Error)runtimeErrors.Add(message);}
        static void Keys(params Key[] keys) {heldKeys=new KeyboardState(keys);InputSystem.QueueStateEvent(keyboard,heldKeys);}
        static void InjectInput() {if(keyboard!=null) {if(!keyboard.enabled)InputSystem.EnableDevice(keyboard);InputSystem.QueueStateEvent(keyboard,heldKeys);}if(testMouse!=null) {if(!testMouse.enabled)InputSystem.EnableDevice(testMouse);InputSystem.QueueStateEvent(testMouse,mouseState);}}
        static void UseTestKeyboard() {if(keyboard!=null)keyboard.MakeCurrent();if(testMouse!=null)testMouse.MakeCurrent();}
        static void Assert(bool condition,string label) {report.Add((condition?"PASS":"FAIL")+" · "+label);if(!condition)throw new Exception(label);}
        static IEnumerator Checks()
        {
            var game=GameManager.Instance;Assert(game!=null,"GameManager inicializa no Play");game.Begin();
            yield return .5f;
            VehicleController traffic=null;foreach(var car in VehicleController.All)if(car.traffic) {traffic=car;break;}
            Vector3 trafficStart=traffic?traffic.transform.position:Vector3.zero;
            Vector3 original=game.player.Position;Keys(Key.W,Key.LeftShift);yield return 1.5f;Keys();yield return .2f;
            Assert(Vector3.Distance(original,game.player.Position)>4,"WASD e sprint deslocam o CharacterController");
            float groundedY=game.player.transform.position.y;Keys(Key.Space);yield return .25f;Keys();
            Assert(game.player.transform.position.y>groundedY+.25f,"Pulo eleva o personagem");yield return 1.2f;
            Assert(game.player.Grounded,"Personagem aterrissa no chão");
            Assert(traffic && Vector3.Distance(trafficStart,traffic.transform.position)>3,"Trânsito segue sua rota independentemente do jogador");
            var missions=game.missions;game.player.Teleport(missions.starts[0].position);yield return .2f;Keys(Key.E);yield return .2f;Keys();yield return .2f;
            Assert(missions.Active==0,"Interação E inicia entrega");
            game.player.Teleport(missions.deliveryVan.transform.position-missions.deliveryVan.transform.right*2.5f);yield return .2f;Keys(Key.E);yield return .2f;Keys();yield return .2f;
            Assert(game.player.Vehicle==missions.deliveryVan && !game.player.controller.enabled,"E entra na van e troca o controle");
            var steeringCheck=CheckSteeringAndCollision(missions.deliveryVan);while(steeringCheck.MoveNext())yield return steeringCheck.Current;
            original=missions.deliveryVan.transform.position;Keys(Key.W);yield return 2f;
            report.Add("INFO · held input "+GameInput.Move+" · runtime input "+missions.deliveryVan.DriveInput+" · grounded "+missions.deliveryVan.GroundContact+" · driver "+(missions.deliveryVan.Driver!=null)+" · speed "+missions.deliveryVan.Speed);
            Assert(game.followCamera.GetComponent<SpeedPostEffects>().Intensity>.05f && Camera.main.fieldOfView>65 && EffectsPool.Instance.DustEmissions>0,"Aceleração produz motion blur, FOV dinâmico e poeira");
            foreach(var contact in Physics.OverlapBox(missions.deliveryVan.transform.position,new Vector3(.88f,.43f,1.9f),missions.deliveryVan.transform.rotation,1))report.Add("INFO · contato: "+contact.name);
            Keys();yield return .2f;
            report.Add("INFO · van de "+original+" para "+missions.deliveryVan.transform.position+" · velocidade "+missions.deliveryVan.Speed+" · input "+GameInput.Move+" · blocked "+game.Blocked);
            Assert(Vector3.Distance(original,missions.deliveryVan.transform.position)>2,"Aceleração move o Rigidbody do veículo");
            var body=missions.deliveryVan.Body;body.linearVelocity=Vector3.zero;body.position=missions.delivery.position+Vector3.up*.65f;yield return .6f;
            Assert(missions.Completed[0] && game.money.Balance>=500,"Entrega conclui e paga recompensa");
            game.player.ExitVehicle(true);yield return .2f;Assert(!game.player.Vehicle && game.player.controller.enabled,"Saída restaura personagem e câmera");
            int balance=game.money.Balance,ammo=game.player.combat.Reserve;game.shop.Buy(0);Assert(game.money.Balance==balance-50 && game.player.combat.Reserve==ammo+36,"Loja desconta dinheiro e adiciona munição");
            Keys(Key.Digit1);yield return .2f;Keys();yield return .2f;int magazine=game.player.combat.Magazine;game.player.combat.Shoot();yield return .2f;
            Assert(game.player.combat.Magazine==magazine-1 && game.wanted.Level>0,"Disparo consome munição e gera wanted");
            game.player.combat.Reload();yield return 2.8f;Assert(game.player.combat.Magazine==game.player.combat.Weapon.magazineSize,"Recarga transfere munição para carregador");
            bool responding=false;foreach(var npc in NPCController.All)if(npc.type==NPCType.Police && npc.responder)responding=true;
            Assert(responding,"Crime faz a polícia gerar reforços com NavMesh");
            game.wanted.Clear();game.player.Teleport(game.spawnPoint);missions.StartMission(1);yield return .2f;
            foreach(var enemy in missions.enemies)enemy.Damage(200);yield return .5f;Assert(missions.Stage==1,"Confronto avança após derrotar todos os guardas");
            game.player.Teleport(missions.package.position);yield return .3f;Assert(missions.Stage==2,"Carga é recolhida ao alcançar o pickup");
            game.wanted.Clear();game.player.Teleport(missions.safe.position);yield return .4f;Assert(missions.Completed[1],"Carga entregue no abrigo conclui confronto");
            game.wanted.spawnResponders=false;missions.StartMission(2);Assert(game.wanted.Level==2,"Fuga começa com duas estrelas");
            game.player.Teleport(new Vector3(78,.3f,-58));
            // Suppress additional spawns only for this timer check, keeping actual LOS logic active.
            game.wanted.escapeSeconds=.8f;
            yield return 1.1f;
            Assert(game.wanted.Level==0,"Wanted termina depois do tempo fora de visão");game.wanted.escapeSeconds=18;game.wanted.spawnResponders=true;
            game.player.Teleport(missions.safe.position);yield return .4f;Assert(missions.Completed[2] && missions.CompleteCount==3,"As três missões chegam ao fim com exploração liberada");
            game.player.health.Damage(200);Assert(game.player.health.Dead,"Dano letal ativa morte e slow motion");yield return 3.1f;
            Assert(!game.player.health.Dead && game.player.health.Current==100 && Vector3.Distance(game.player.Position,game.spawnPoint)<1,"Respawn restaura vida no ponto seguro");
            game.Pause(true);Assert(Time.timeScale==0,"Pause congela o jogo");game.Pause(false);
            var expansion=ExpansionChecks();while(expansion.MoveNext())yield return expansion.Current;
            Assert(runtimeErrors.Count==0,"Nenhum erro ou exceção durante o ciclo de jogo");
        }
        static void Finish()
        {
            if(runtimeErrors.Count>0)report.AddRange(runtimeErrors.ConvertAll(x=>"RUNTIME ERROR · "+x));
            File.WriteAllLines("Documentation/PlayChecks.txt",report);Debug.Log("Costa Brava Play checks:\n"+string.Join("\n",report));
            steps=null;InputSystem.onBeforeUpdate-=InjectInput;InputSystem.onAfterUpdate-=UseTestKeyboard;if(keyboard!=null) {InputSystem.RemoveDevice(keyboard);keyboard=null;}if(testMouse!=null){InputSystem.RemoveDevice(testMouse);testMouse=null;}InputSystem.settings.backgroundBehavior=previousBackground;Application.logMessageReceived-=Capture;EditorApplication.isPlaying=false;
        }
    }
}

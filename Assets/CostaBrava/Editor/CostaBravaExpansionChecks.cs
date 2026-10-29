using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CostaBrava.Editor
{
    public static partial class CostaBravaPlayChecks
    {
        static Mouse testMouse;
        static MouseState mouseState;
        static IEnumerator CheckSteeringAndCollision(VehicleController car)
        {
            car.Body.position=new Vector3(0,.9f,-48);car.Body.rotation=Quaternion.identity;car.Body.linearVelocity=Vector3.zero;yield return .25f;
            float yaw=car.transform.eulerAngles.y;Keys(Key.W,Key.D);yield return 1f;Keys();yield return .15f;
            Assert(Mathf.Abs(Mathf.DeltaAngle(yaw,car.transform.eulerAngles.y))>5,"Direção arcade continua funcionando com rotação física bloqueada");
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="Teste / impacto";obstacle.transform.position=car.transform.position+car.transform.forward*3;obstacle.transform.localScale=Vector3.one*1.5f;obstacle.AddComponent<Rigidbody>().mass=50;Physics.SyncTransforms();
            yaw=car.transform.eulerAngles.y;car.Body.linearVelocity=car.transform.forward*9;car.Body.AddTorque(Vector3.up*9000,ForceMode.Impulse);yield return .65f;
            Assert(Mathf.Abs(car.Body.angularVelocity.y)<.01f && Mathf.Abs(Mathf.DeltaAngle(yaw,car.transform.eulerAngles.y))<1,"Impacto e torque não deixam o carro girando sozinho");
            Object.Destroy(obstacle);car.Body.linearVelocity=Vector3.zero;car.Body.position=new Vector3(-42,.9f,-42);car.Body.rotation=Quaternion.Euler(0,90,0);yield return .2f;
        }
        static IEnumerator ExpansionChecks()
        {
            var game=GameManager.Instance;game.wanted.Clear();game.wanted.spawnResponders=false;
            Assert(game.player.combat.weapons.Length==3 && !game.player.combat.Owns(2),"Terceira arma começa bloqueada e sem compra gratuita");
            Assert(!game.player.combat.Select(2) && !game.weaponShop.BuyRifle(),"Tecla 3 e compra fora da loja não concedem rifle");
            foreach(var enemy in game.missions.enemies)Assert(!enemy.gameObject.activeSelf,"Guardas da missão desaparecem após conclusão");
            game.player.Teleport(game.weaponShop.counter.position+Vector3.back*1.5f);yield return .3f;
            Keys(Key.E);yield return .2f;Keys();yield return .2f;Assert(game.weaponShop.IsOpen,"E abre FAROL no novo quarteirão");
            int balance=game.money.Balance;Assert(game.weaponShop.BuyRifle() && game.money.Balance==balance-100 && game.player.combat.Owns(2),"Compra exclusiva desconta exatamente $100 e libera espaço 3");
            balance=game.money.Balance;Assert(!game.weaponShop.BuyRifle() && game.money.Balance==balance,"Compra duplicada não cobra novamente");game.weaponShop.Close();
            testMouse=InputSystem.AddDevice<Mouse>("CostaBrava test mouse");mouseState=new MouseState {buttons=2};yield return .6f;
            report.Add("INFO · luneta: slot "+game.player.combat.Selected+" equipado "+game.player.combat.Equipped+" câmera "+game.followCamera.ScopeActive+" FOV "+Camera.main.fieldOfView+" bloqueado "+game.Blocked);
            Assert(game.player.combat.Selected==2 && game.followCamera.ScopeActive && Camera.main.fieldOfView<23,"Rifle equipado usa luneta com zoom e sensibilidade próprios");
            mouseState=new MouseState();yield return .2f;
            var dummy=GameObject.CreatePrimitive(PrimitiveType.Cube);dummy.name="Teste / alvo destrutível";dummy.transform.position=new Vector3(-8,12,-28);dummy.transform.localScale=Vector3.one*2;var prop=dummy.AddComponent<DestructibleProp>();
            Camera.main.transform.SetPositionAndRotation(new Vector3(-8,12,-40),Quaternion.identity);Physics.SyncTransforms();int magazine=game.player.combat.Magazine;game.player.combat.Shoot();
            Assert(prop.Destroyed && game.player.combat.Magazine==magazine-1,"Raycast do rifle causa dano real em prop e consome cartucho");Object.Destroy(dummy);yield return 1f;
            var melee=game.player.GetComponent<PlayerMelee>();game.player.Teleport(new Vector3(-8,.2f,-34));game.player.transform.rotation=Quaternion.identity;
            var target=Object.Instantiate(game.wanted.policePrefab,new Vector3(-8,.2f,-32.8f),Quaternion.identity).GetComponent<NPCController>();target.type=NPCType.Pedestrian;target.maximumHealth=1000;target.ResetNPC();target.enabled=false;Physics.SyncTransforms();
            Vector3 start=target.transform.position;float health=target.Health;
            Assert(melee.Attack(false),"Primeiro soco inicia combate corpo a corpo");yield return .45f;
            Assert(target.Health<health && Vector3.Distance(start,target.transform.position)>.05f,"Soco aplica dano e knockback");
            melee.Attack(false);yield return .45f;Assert(melee.Combo==2,"Segundo soco continua combo");
            melee.Attack(false);yield return .6f;Assert(melee.Combo==3,"Terceiro golpe finaliza combo de três");
            health=target.Health;melee.Attack(true);yield return .75f;Assert(target.Health<health && melee.Kicking,"Chute causa dano e empurrão");Object.Destroy(target.gameObject);
            var camera=Camera.main;var savedPosition=camera.transform.position;var savedRotation=camera.transform.rotation;
            camera.transform.SetPositionAndRotation(new Vector3(0,50,-40),Quaternion.identity);
            Assert(!SpawnVisibility.IsHidden(new Vector3(0,50,-20),1),"Polícia rejeita candidato visível em frente à câmera");
            Assert(SpawnVisibility.IsHidden(new Vector3(0,50,-60),1),"Candidato atrás da câmera pode ser considerado oculto");camera.transform.SetPositionAndRotation(savedPosition,savedRotation);
            int found=0;for(int i=0;i<12;i++)if(game.wanted.TryFindFootSpawn(out var point)) {found++;Assert(Vector3.Distance(point,game.player.Position)>=38 && SpawnVisibility.IsHidden(point,1.3f),"Candidato policial fica distante e fora da visão");}
            Assert(found>0,"Sistema encontra posições ocultas alcançáveis para reforços");
            var clone=Object.Instantiate(game.wanted.policeCarPrefab,new Vector3(88,.9f,18),Quaternion.identity).GetComponent<VehicleController>();clone.IsPolice=false;clone.traffic=false;
            var crate=GameObject.CreatePrimitive(PrimitiveType.Cube);crate.name="Teste / caixa explosão";crate.transform.position=new Vector3(90,1,18);crate.AddComponent<Rigidbody>().mass=12;var destructible=crate.AddComponent<DestructibleProp>();
            game.player.Teleport(new Vector3(88,.2f,3));game.player.combat.Select(2);yield return .3f;
            for(int i=0;i<3;i++)
            {
                var c=Camera.main;c.transform.position=clone.transform.position+Vector3.back*10;c.transform.LookAt(clone.transform.position);Physics.SyncTransforms();game.player.combat.Shoot();
                if(i==2)Assert(game.followCamera.ShakeAmplitude>.1f,"Explosão próxima provoca camera shake");yield return 1f;
            }
            Assert(clone.GetComponent<VehicleDamage>().Destroyed && !clone.IsUsable,"Tiros suficientes explodem e inutilizam permanentemente o carro");
            Assert(destructible.Destroyed && (destructible.Body.linearVelocity.magnitude>1 || Vector3.Distance(crate.transform.position,new Vector3(90,1,18))>.2f),"Explosão destrói e lança props próximos");
            game.player.EnterVehicle(clone);Assert(!game.player.Vehicle,"Carro destruído rejeita nova entrada");
            Object.Destroy(crate);Object.Destroy(clone.gameObject);
            var clock=game.GetComponent<DayNightCycle>();clock.SetHour(23);yield return .4f;
            bool street=false,headlights=false;foreach(var lamp in Object.FindObjectsByType<AutomaticLights>(FindObjectsSortMode.None))if(lamp.lights!=null)foreach(var light in lamp.lights)if(light && light.enabled) {if(lamp.vehicle)headlights=true;else street=true;}
            Assert(clock.IsNight && street && headlights,"Noite liga postes e faróis automaticamente");
            clock.SetHour(12);yield return .4f;bool allOff=true;foreach(var lamp in Object.FindObjectsByType<AutomaticLights>(FindObjectsSortMode.None))if(lamp.lights!=null)foreach(var light in lamp.lights)if(light && light.enabled)allOff=false;
            Assert(!clock.IsNight && allOff,"Dia apaga postes e faróis");
            var map=game.GetComponent<MiniMap>();Vector2 objective=map.MapPoint(new Vector3(1000,0,1000));Assert(Mathf.Abs(objective.x)<=.46f && Mathf.Abs(objective.y)<=.46f,"Minimapa mantém objetivo distante dentro da borda");
            var wreck=game.missions.deliveryVan;wreck.Body.position=new Vector3(-42,.9f,-42);wreck.Body.linearVelocity=Vector3.zero;Physics.SyncTransforms();wreck.GetComponent<VehicleDamage>().Explode();game.missions.Completed[0]=false;game.missions.StartMission(0);
            Assert(game.missions.Active==0 && game.missions.deliveryVan!=wreck && game.missions.deliveryVan.IsUsable && Vector3.Distance(game.missions.deliveryVan.transform.position,wreck.transform.position)>3,"Entrega pode ser retomada com van nova fora dos destroços");
            clock.SetHour(15);game.wanted.Clear();game.wanted.spawnResponders=true;
        }
    }
}

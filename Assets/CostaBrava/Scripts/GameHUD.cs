using UnityEngine;

namespace CostaBrava
{
    public class GameHUD : MonoBehaviour
    {
        GUIStyle style;
        Texture2D pixel;
        Texture2D scopeMask;
        MiniMap miniMap;
        string notification,subtitle,toast;
        float notifyUntil,toastUntil;
        readonly Color dark=new Color(.025f,.07f,.095f,.94f), gold=new Color(1,.75f,.3f), mint=new Color(.33f,.9f,.72f);
        public void Notify(string title,string description) {notification=title;subtitle=description;notifyUntil=Time.unscaledTime+4.5f;}
        public void Toast(string message) {toast=message;toastUntil=Time.unscaledTime+3;}
        void Box(Rect rect,Color color) {GUI.color=color;GUI.DrawTexture(rect,pixel);GUI.color=Color.white;}
        void Text(Rect rect,string text,int size=22,Color? color=null,FontStyle weight=FontStyle.Normal,TextAnchor alignment=TextAnchor.UpperLeft)
        {
            style.fontSize=size;style.fontStyle=weight;style.normal.textColor=color??Color.white;style.alignment=alignment;style.wordWrap=true;GUI.Label(rect,text,style);
        }
        bool Button(Rect rect,string text,Color? tint=null)
        {
            bool hover=rect.Contains(Event.current.mousePosition);Box(rect,hover?(tint??mint):new Color(.12f,.22f,.26f));Text(rect,text,23,hover?dark:Color.white,FontStyle.Bold,TextAnchor.MiddleCenter);return GUI.Button(rect,GUIContent.none,GUIStyle.none);
        }
        void OnGUI()
        {
            var game=GameManager.Instance;if(!game || !game.player)return;
            if(pixel==null) {pixel=Texture2D.whiteTexture;style=new GUIStyle(GUI.skin.label);}
            float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1920*scale)/2,(Screen.height-1080*scale)/2),Quaternion.identity,Vector3.one*scale);
            if(!game.Started) {Title(game);return;}
            var player=game.player;var combat=player.combat;
            if(combat.Scoped)Scope();
            Box(new Rect(36,32,330,108),dark);Box(new Rect(36,32,4,108),mint);
            Text(new Rect(56,47,280,30),"COSTA BRAVA",23,mint,FontStyle.Bold);
            Text(new Rect(56,88,260,35),"$"+game.money.Balance+"    VIDA "+Mathf.CeilToInt(player.health.Current),23);
            Box(new Rect(56,124,285,4),new Color(.2f,.3f,.32f));Box(new Rect(56,124,285*player.health.Current/100,4),mint);
            Box(new Rect(1534,32,350,112),dark);
            Text(new Rect(1558,48,300,33),"PROCURADO  "+new string('★',game.wanted.Level)+new string('☆',3-game.wanted.Level),24,gold,FontStyle.Bold);
            Text(new Rect(1558,88,300,25),game.wanted.Level==0?"SEM ALERTAS":(game.wanted.Searching?"BUSCANDO · DESPISTE A POLÍCIA":"PERSEGUIÇÃO ATIVA"),16);
            if(game.wanted.Level>0)Box(new Rect(1558,127,300*game.wanted.EscapeProgress,4),mint);
            Box(new Rect(36,170,480,114),dark);Text(new Rect(56,184,440,25),game.missions.Active<0?"EXPLORAÇÃO LIVRE · "+game.missions.CompleteCount+"/3":game.missions.Names[game.missions.Active],16,gold,FontStyle.Bold);
            Text(new Rect(56,218,435,60),game.missions.Objective,21);
            Box(new Rect(1534,948,350,96),dark);
            Text(new Rect(1558,960,300,32),player.Vehicle?"VELOCIDADE":(combat.Equipped?combat.Weapon.displayName:"MÃOS LIVRES"),18,mint,FontStyle.Bold);
            Text(new Rect(1558,997,300,35),player.Vehicle?Mathf.RoundToInt(Mathf.Abs(player.Vehicle.Speed)*3.6f)+" km/h":(combat.Equipped?(combat.Reloading?"RECARREGANDO…":combat.Magazine+" / "+combat.Reserve):(combat.Owns(2)?"1 pistola · 2 carabina · 3 rifle":"1 pistola · 2 carabina · 3 bloqueado")),combat.Equipped||player.Vehicle?25:16);
            if(!miniMap)miniMap=GetComponent<MiniMap>();
            if(miniMap)
            {
                Box(new Rect(1534,169,350,380),dark);miniMap.Draw(new Rect(1550,185,318,318));
                Text(new Rect(1550,514,320,28),(DayNightCycle.Instance?DayNightCycle.Instance.Clock+" · ":"")+"NORTE ↑  /  FAROL W",18,mint);
            }
            Box(new Rect(36,980,1430,64),dark);
            Text(new Rect(56,996,1390,44),player.Vehicle?"W/S acelerar / frear    A/D direção    Espaço freio de mão    E sair    T avançar 3h    Esc pausa":"WASD mover  Shift correr  Espaço pular  E usar  1/2/3 armas  Q guardar  RMB mira  LMB tiro/soco  F soco  G chute  R recarga  T +3h  Esc",17,new Color(.8f,.87f,.88f));
            if(combat.Equipped && !combat.Scoped && !player.Vehicle && !game.Blocked)Crosshair(combat);
            var melee=player.GetComponent<PlayerMelee>();if(melee && melee.Attacking)Text(new Rect(760,610,400,40),melee.Kicking?"CHUTE":"COMBO "+melee.Combo+" / 3",22,gold,FontStyle.Bold,TextAnchor.MiddleCenter);
            if(!game.Blocked) {Waypoint(game);Interaction(game);}
            if(Time.unscaledTime<toastUntil) {Box(new Rect(460,876,1000,58),dark);Text(new Rect(480,884,960,40),toast,22,gold,FontStyle.Bold,TextAnchor.MiddleCenter);}
            if(Time.unscaledTime<notifyUntil) {Box(new Rect(570,55,780,118),dark);Box(new Rect(570,55,780,4),gold);Text(new Rect(590,76,740,40),notification,30,gold,FontStyle.Bold,TextAnchor.MiddleCenter);Text(new Rect(590,120,740,42),subtitle,20,Color.white,FontStyle.Normal,TextAnchor.MiddleCenter);}
            if(player.health.DamageFlash>0)Box(new Rect(0,0,1920,1080),new Color(.75f,.1f,.05f,player.health.DamageFlash*.15f));
            if(player.health.Dead) {Box(new Rect(0,0,1920,1080),new Color(.025f,.035f,.05f,.8f));Text(new Rect(460,435,1000,90),"VOCÊ CAIU",64,gold,FontStyle.Bold,TextAnchor.MiddleCenter);Text(new Rect(460,545,1000,60),"Voltando para a Praça do Sol…",26,Color.white,FontStyle.Normal,TextAnchor.MiddleCenter);}
            if(game.Paused)PauseMenu(game);
            if(game.shop.IsOpen)Shop(game);
            if(game.weaponShop && game.weaponShop.IsOpen)GunShop(game);
            GUI.matrix=Matrix4x4.identity;
        }
        void Title(GameManager game)
        {
            Box(new Rect(0,0,1920,1080),new Color(.025f,.07f,.095f,.72f));Box(new Rect(80,110,770,860),dark);Box(new Rect(80,110,8,860),mint);
            Text(new Rect(130,165,680,35),"UMA CIDADE PEQUENA. GRANDES ESCOLHAS.",19,mint,FontStyle.Bold);
            Text(new Rect(124,225,700,185),"COSTA\nBRAVA",84,Color.white,FontStyle.Bold);
            Text(new Rect(130,457,630,100),"Um protótipo de ação em mundo aberto.\nA cidade está acordada. O resto é com você.",28);
            Text(new Rect(130,600,630,155),"DIRIJA · EXPLORE · DESPISTE\n\n3 missões · 8 quarteirões · dia e noite\nSuprimentos na VIVA. Rifle com luneta na FAROL.",21,new Color(.75f,.83f,.85f));
            if(Button(new Rect(130,793,630,74),"ENTRAR NA CIDADE  [ENTER]",mint))game.Begin();
            Text(new Rect(130,905,630,35),"WASD mover · Mouse câmera · E interagir · Esc pausa",17,new Color(.6f,.73f,.76f));
            Text(new Rect(1080,800,680,110),"PRAÇA DO SOL\nCOSTA BRAVA / DISTRITO 01",30,gold,FontStyle.Bold);
            GUI.matrix=Matrix4x4.identity;
        }
        void Crosshair(PlayerCombat combat)
        {
            Color color=combat.HitMarker>0?gold:Color.white;float gap=combat.Aiming?5:10;
            Box(new Rect(960-gap-8,539,8,2),color);Box(new Rect(960+gap,539,8,2),color);Box(new Rect(959,540-gap-8,2,8),color);Box(new Rect(959,540+gap,2,8),color);
            if(combat.HitMarker>0)Text(new Rect(925,505,70,70),"×",46,gold,FontStyle.Bold,TextAnchor.MiddleCenter);
        }
        void Waypoint(GameManager game)
        {
            var camera=Camera.main;if(!camera)return;
            Vector3 world=game.missions.Target+Vector3.up*2.5f;Vector3 p=camera.WorldToViewportPoint(world);
            float distance=Vector3.Distance(game.player.Position,game.missions.Target);
            if(distance<4)return;
            if(p.z>0 && p.x>.1f && p.x<.9f && p.y>.15f && p.y<.85f)
            {float x=p.x*1920,y=(1-p.y)*1080;Text(new Rect(x-80,y-25,160,60),"◆\n"+Mathf.RoundToInt(distance)+" m",20,gold,FontStyle.Bold,TextAnchor.MiddleCenter);}
            else {Box(new Rect(36,299,300,42),dark);Text(new Rect(56,305,270,30),"OBJETIVO · "+Mathf.RoundToInt(distance)+" m "+(Vector3.Dot(camera.transform.right,world-game.player.Position)>0?"→":"←"),19,gold,FontStyle.Bold);}
        }
        void Interaction(GameManager game)
        {
            string text="";
            if(game.player.Vehicle)text=Mathf.Abs(game.player.Vehicle.Speed)<3?"E / Sair do veículo":"Pare para sair do veículo";
            else if(game.shop.Near)text="E / VIVA · Loja de suprimentos";
            else if(game.weaponShop && game.weaponShop.Near)text="E / FAROL · Loja de armas · Rifle $100";
            else if(game.missions.CanInteract)text="E / Iniciar missão";
            else if(game.player.NearestVehicle())text="E / Entrar no veículo";
            if(text!="") {Box(new Rect(650,788,620,60),dark);Text(new Rect(670,796,580,42),text,24,mint,FontStyle.Bold,TextAnchor.MiddleCenter);}
        }
        void PauseMenu(GameManager game)
        {
            Box(new Rect(0,0,1920,1080),new Color(0,.025f,.04f,.75f));Box(new Rect(670,255,580,560),dark);
            Text(new Rect(700,298,520,65),"PAUSA",44,mint,FontStyle.Bold,TextAnchor.MiddleCenter);
            if(Button(new Rect(720,400,480,70),"CONTINUAR"))game.Pause(false);
            if(Button(new Rect(720,510,480,70),"REINICIAR A CIDADE"))game.Restart();
            if(Button(new Rect(720,620,480,70),"SAIR"))game.Quit();
            Text(new Rect(720,739,480,45),"O progresso desta sessão não é salvo.",18,new Color(.65f,.75f,.78f),FontStyle.Normal,TextAnchor.MiddleCenter);
        }
        void Shop(GameManager game)
        {
            Box(new Rect(0,0,1920,1080),new Color(0,.025f,.04f,.75f));Box(new Rect(590,210,740,680),dark);Box(new Rect(590,210,740,5),mint);
            Text(new Rect(640,248,640,55),"VIVA / SUPRIMENTOS",36,mint,FontStyle.Bold);Text(new Rect(640,319,640,40),"SALDO $"+game.money.Balance,25,gold);
            if(Button(new Rect(640,390,640,70),"PISTOLA · 36 MUNIÇÕES                         $50"))game.shop.Buy(0);
            if(Button(new Rect(640,480,640,70),"CARABINA · 90 MUNIÇÕES                    $100"))game.shop.Buy(1);
            if(Button(new Rect(640,570,640,70),"RECUPERAR VIDA                                 $75"))game.shop.Buy(2);
            Text(new Rect(640,680,640,60),game.shop.Message,22,gold);
            if(Button(new Rect(640,773,640,65),"VOLTAR À CIDADE [ESC]"))game.shop.Close();
        }
        void GunShop(GameManager game)
        {
            Box(new Rect(0,0,1920,1080),new Color(0,.025f,.04f,.8f));Box(new Rect(590,210,740,680),dark);Box(new Rect(590,210,740,5),gold);
            Text(new Rect(640,248,640,55),"FAROL / ARMAS",38,gold,FontStyle.Bold);Text(new Rect(640,319,640,45),"SALDO $"+game.money.Balance,25,mint);
            if(Button(new Rect(640,410,640,80),game.player.combat.Owns(2)?"RIFLE FAROL · ADQUIRIDO":"RIFLE COM LUNETA · ESPAÇO 3                 $100",gold))game.weaponShop.BuyRifle();
            if(Button(new Rect(640,525,640,70),"MUNIÇÃO DO RIFLE · 15 CARTUCHOS            $35"))game.weaponShop.BuyAmmo();
            Text(new Rect(640,645,640,90),game.weaponShop.Message,23,gold);
            if(Button(new Rect(640,773,640,65),"VOLTAR À CIDADE [ESC]"))game.weaponShop.Close();
        }
        void Scope()
        {
            if(!scopeMask)
            {
                scopeMask=new Texture2D(256,256,TextureFormat.RGBA32,false);var colors=new Color[256*256];
                for(int y=0;y<256;y++)for(int x=0;x<256;x++) {float d=Vector2.Distance(new Vector2(x,y),new Vector2(127.5f,127.5f));colors[y*256+x]=d>123?Color.black:(d>119?new Color(.05f,.08f,.08f,.8f):Color.clear);}
                scopeMask.SetPixels(colors);scopeMask.Apply();
            }
            Box(new Rect(0,0,500,1080),Color.black);Box(new Rect(1420,0,500,1080),Color.black);Box(new Rect(500,0,920,80),Color.black);Box(new Rect(500,1000,920,80),Color.black);
            GUI.DrawTexture(new Rect(500,80,920,920),scopeMask);
            Box(new Rect(620,539,680,2),new Color(0,0,0,.8f));Box(new Rect(959,200,2,680),new Color(0,0,0,.8f));
            Box(new Rect(958,538,4,4),new Color(1,.2f,.06f));
            for(int n=-3;n<=3;n++)if(n!=0){Box(new Rect(960+n*55,534,1,12),Color.black);Box(new Rect(954,540+n*55,12,1),Color.black);}
            Text(new Rect(760,885,400,40),"FAROL / 4x",20,mint,FontStyle.Bold,TextAnchor.MiddleCenter);
        }
        void OnDestroy() {if(scopeMask)Destroy(scopeMask);}
    }
}

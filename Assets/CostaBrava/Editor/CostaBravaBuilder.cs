using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CostaBrava.Editor
{
    [InitializeOnLoad]
    public static class CostaBravaBuilder
    {
        public const string Root="Assets/CostaBrava";
        public const string ScenePath=Root+"/Scenes/CostaBrava.unity";
        static Dictionary<string,Material> materials;
        static Transform world;
        static AnimatorController characterController;
        static System.Random random;
        static CostaBravaBuilder() {EditorApplication.delayCall+=FirstSetup;}
        static void FirstSetup()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {EditorApplication.delayCall+=FirstSetup;return;}
            if(System.IO.File.Exists(ScenePath) || !System.IO.File.Exists(Root+"/Art/Custom/viva-kiosk.fbx"))return;
            if(EditorSceneManager.GetActiveScene().isDirty)return;
            Build();
        }
        [MenuItem("Tools/Costa Brava/Criar ou atualizar cidade")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) {Debug.LogWarning("Saia do modo Play antes de criar a cidade.");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            System.IO.Directory.CreateDirectory(Root+"/Data/Materials");System.IO.Directory.CreateDirectory(Root+"/Data/Prefabs");
            AssetDatabase.Refresh();
            random=new System.Random(14);materials=new Dictionary<string,Material>();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            world=new GameObject("Costa Brava · Cidade").transform;
            SetupLighting();GroundAndRoads();Blocks();StreetProps();
            characterController=BuildAnimator();
            var nav=world.gameObject.AddComponent<NavMeshSurface>();nav.collectObjects=CollectObjects.Children;nav.useGeometry=NavMeshCollectGeometry.PhysicsColliders;nav.layerMask=1;
            nav.overrideTileSize=true;nav.tileSize=128;nav.overrideVoxelSize=true;nav.voxelSize=.18f;nav.BuildNavMesh();
            if(nav.navMeshData) {AssetDatabase.CreateAsset(nav.navMeshData,Root+"/Data/CityNavMesh_"+DateTime.UtcNow.ToString("HHmmss")+".asset");}
            var systems=new GameObject("Sistemas");var game=systems.AddComponent<GameManager>();
            game.wanted=systems.AddComponent<WantedSystem>();game.missions=systems.AddComponent<MissionSystem>();game.money=systems.AddComponent<MoneySystem>();game.shop=systems.AddComponent<ShopSystem>();game.hud=systems.AddComponent<GameHUD>();game.audioManager=systems.AddComponent<AudioManager>();systems.AddComponent<EffectsPool>();
            SetupAudio(game.audioManager);
            game.player=Player();
            var cameraObj=new GameObject("Câmera de terceira pessoa");cameraObj.tag="MainCamera";var cam=cameraObj.AddComponent<Camera>();cam.nearClipPlane=.15f;cam.farClipPlane=450;cam.backgroundColor=new Color(.42f,.72f,.85f);
            cameraObj.AddComponent<AudioListener>();cameraObj.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;game.followCamera=cameraObj.AddComponent<ThirdPersonCamera>();game.followCamera.target=game.player;game.followCamera.obstructionMask=(1<<0)|(1<<9);cameraObj.transform.position=new Vector3(-8,4,-39);cameraObj.transform.rotation=Quaternion.Euler(20,0,0);
            SetupVehicles(game);SetupPeople(game);SetupMissions(game);
            foreach(var t in world.GetComponentsInChildren<Transform>()) if(t.GetComponent<Renderer>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.companyName="Costa Brava Studio";PlayerSettings.productName="Costa Brava";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.runInBackground=true;
            AssetDatabase.SaveAssets();Selection.activeGameObject=game.player.gameObject;
            SceneView.lastActiveSceneView?.LookAt(new Vector3(0,2,-15),Quaternion.Euler(45,0,0),95);
            Debug.Log("COSTA BRAVA: cidade criada, NavMesh pronto e cena salva. Pressione Play e Enter para jogar.");
            Validate();
            CostaBravaExpansionInstaller.Upgrade();
        }
        static Material Mat(string name,Color color,Texture texture=null,bool unlit=false)
        {
            if(materials.TryGetValue(name,out var existing))return existing;
            string path=Root+"/Data/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) {material=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;material.enableInstancing=true;if(texture)material.mainTexture=texture;
            if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.15f);
            materials[name]=material;return material;
        }
        static GameObject Cube(string name,Vector3 position,Vector3 scale,Material material,Transform parent=null,bool collider=true)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent?parent:world,false);obj.transform.localPosition=position;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider)UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;
        }
        static GameObject Primitive(PrimitiveType type,string name,Vector3 pos,Vector3 scale,Material material,Transform parent=null,bool collider=true)
        {
            var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent?parent:world,false);obj.transform.localPosition=pos;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;if(!collider)UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;
        }
        static Bounds BoundsOf(GameObject obj)
        {
            var renderers=obj.GetComponentsInChildren<Renderer>();var b=new Bounds();bool first=true;
            foreach(var renderer in renderers) {if(first){b=renderer.bounds;first=false;}else b.Encapsulate(renderer.bounds);}return b;
        }
        static GameObject Model(string path,string category,Transform parent,Vector3 localPosition,float desiredSize,bool byHeight=true)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Art/"+path);
            if(!prefab)throw new Exception("Modelo ausente: "+path);
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(obj,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            obj.transform.position=Vector3.zero;obj.transform.rotation=Quaternion.identity;obj.transform.localScale=Vector3.one;
            var bounds=BoundsOf(obj);float size=byHeight?bounds.size.y:bounds.size.z;float factor=desiredSize/Mathf.Max(.01f,size);
            obj.transform.localScale=Vector3.one*factor;obj.transform.position=new Vector3(-bounds.center.x*factor,-bounds.min.y*factor,-bounds.center.z*factor);
            obj.transform.SetParent(parent,false);obj.transform.localPosition+=localPosition;
            ConvertMaterials(obj,category,path);return obj;
        }
        static void ConvertMaterials(GameObject obj,string category,string path)
        {
            Texture texture=null;string name="";
            if(category=="Vehicles"||category=="Buildings") {texture=AssetDatabase.LoadAssetAtPath<Texture>(Root+"/Art/Kenney/"+category+"/Textures/colormap.png");name=category;}
            if(category=="Characters") {string letter=System.IO.Path.GetFileNameWithoutExtension(path).Replace("character-","");texture=AssetDatabase.LoadAssetAtPath<Texture>(Root+"/Art/Kenney/Characters/Textures/texture-"+letter+".png");name="Character-"+letter;}
            foreach(var renderer in obj.GetComponentsInChildren<Renderer>())
            {
                if(texture)renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>Mat(name,Color.white,texture)).ToArray();
                else renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>Mat("Custom-"+(m?m.name:"cream").Replace(" ","_"),CustomColor(m?m.name:"cream"))).ToArray();
            }
        }
        static Color CustomColor(string name)
        {
            if(name.Contains("mint"))return new Color(.12f,.65f,.49f);if(name.Contains("leaf"))return new Color(.12f,.46f,.24f);
            if(name.Contains("navy"))return new Color(.035f,.11f,.16f);if(name.Contains("wood"))return new Color(.48f,.26f,.12f);
            if(name.Contains("coral"))return new Color(.92f,.31f,.19f);if(name.Contains("gold"))return new Color(1,.64f,.17f);
            return new Color(.94f,.86f,.64f);
        }
        static void SetupLighting()
        {
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.73f,.8f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.53f,.75f,.82f);RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.0026f;
            var sun=new GameObject("Sol da costa");var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.88f,.72f);light.intensity=1.7f;light.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(48,-35,0);RenderSettings.sun=light;
            var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.43f,.66f,.9f));sky.SetColor("_GroundColor",new Color(.67f,.72f,.68f));sky.SetFloat("_AtmosphereThickness",.8f);
            string skyPath=Root+"/Data/CoastalSky.mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(skyPath);if(saved){EditorUtility.CopySerialized(sky,saved);UnityEngine.Object.DestroyImmediate(sky);sky=saved;}else AssetDatabase.CreateAsset(sky,skyPath);RenderSettings.skybox=sky;
            var volume=new GameObject("Cor e exposição").AddComponent<Volume>();volume.isGlobal=true;var profile=ScriptableObject.CreateInstance<VolumeProfile>();var tonemap=profile.Add<Tonemapping>(true);tonemap.mode.value=TonemappingMode.ACES;
            string profilePath=Root+"/Data/CoastalVolume.asset";var old=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);if(old)profile=old;else AssetDatabase.CreateAsset(profile,profilePath);volume.sharedProfile=profile;
            QualitySettings.shadowDistance=65;QualitySettings.vSyncCount=0;
        }
        static void GroundAndRoads()
        {
            var sand=Mat("Sand",new Color(.78f,.71f,.53f));var asphalt=Mat("Asphalt",new Color(.16f,.22f,.25f));var concrete=Mat("Sidewalk",new Color(.7f,.7f,.61f));var white=Mat("RoadStripe",new Color(.97f,.87f,.54f));
            Cube("Ilha urbana",new Vector3(0,-.3f,4),new Vector3(172,.6f,132),sand);
            Cube("Oceano",new Vector3(0,-.2f,-105),new Vector3(400,.2f,80),Mat("Ocean",new Color(.12f,.53f,.65f)),null,false);
            foreach(float x in new[]{-66f,-22f,22f,66f})
            {
                Cube("Avenida",new Vector3(x,.005f,0),new Vector3(10,.02f,108),asphalt);
                foreach(float side in new[]{-1f,1f})Cube("Calçada",new Vector3(x+side*6.2f,.07f,0),new Vector3(2.4f,.14f,108),concrete);
                for(float z=-44;z<49;z+=7)Cube("Faixa central",new Vector3(x,.024f,z),new Vector3(.18f,.02f,3),white,null,false);
            }
            foreach(float z in new[]{-48f,0f,48f})
            {
                Cube("Rua transversal",new Vector3(0,.009f,z),new Vector3(148,.02f,10),asphalt);
                foreach(float side in new[]{-1f,1f})Cube("Calçada",new Vector3(0,.075f,z+side*6.2f),new Vector3(148,.15f,2.4f),concrete);
                for(float x=-66;x<70;x+=7)Cube("Faixa central",new Vector3(x,.028f,z),new Vector3(3,.02f,.18f),white,null,false);
                foreach(float x in new[]{-66f,-22f,22f,66f})
                {
                    Cube("Cruzamento",new Vector3(x,.03f,z),new Vector3(14,.02f,10),asphalt,null,false);
                    for(int n=-3;n<=3;n++) {Cube("Faixa de pedestres",new Vector3(x+n*.85f,.055f,z+7),new Vector3(.45f,.02f,2.4f),concrete,null,false);Cube("Faixa de pedestres",new Vector3(x+n*.85f,.055f,z-7),new Vector3(.45f,.02f,2.4f),concrete,null,false);}
                }
            }
            Cube("Passeio à beira-mar",new Vector3(0,.12f,-58),new Vector3(158,.24f,6),concrete);
            Cube("Muro beira-mar",new Vector3(0,.45f,-62),new Vector3(162,.9f,.5f),concrete);
            Cube("Limite norte",new Vector3(0,1,68),new Vector3(173,2,1),concrete);
            foreach(float x in new[]{-86f,86f})Cube("Limite lateral",new Vector3(x,1,3),new Vector3(1,2,131),concrete);
        }
        static void Building(string letter,Vector3 pos,float height,float rotation=0)
        {
            var parent=new GameObject("Edifício "+letter).transform;parent.SetParent(world);parent.position=pos;
            var model=Model("Kenney/Buildings/building-"+letter+".fbx","Buildings",parent,Vector3.zero,height);model.transform.localRotation=Quaternion.Euler(0,rotation,0);
            var b=BoundsOf(model);
            float footprint=Mathf.Max(b.size.x,b.size.z);if(footprint>12) {float reduction=12/footprint;var scale=model.transform.localScale;scale.x*=reduction;scale.z*=reduction;model.transform.localScale=scale;b=BoundsOf(model);}
            var box=parent.gameObject.AddComponent<BoxCollider>();box.center=parent.InverseTransformPoint(b.center);box.size=new Vector3(b.size.x,b.size.y,b.size.z);
            // A smaller render distance avoids drawing unseen distant district facades.
            var lod=parent.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.025f,model.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
        }
        static void Blocks()
        {
            var paving=Mat("PlazaPaving",new Color(.78f,.73f,.6f));var grass=Mat("ParkGrass",new Color(.3f,.5f,.31f));
            Cube("Praça do Sol",new Vector3(0,.04f,-24),new Vector3(28,.08f,31),paving);
            Cube("Jardim da praça",new Vector3(0,.11f,-22),new Vector3(11,.12f,10),grass);
            Model("Custom/wave-monument.fbx","Custom",world,new Vector3(0,.18f,-22),3);
            Model("Custom/viva-kiosk.fbx","Custom",world,new Vector3(10,.16f,-36),3.6f);
            Cube("Colisão da loja",new Vector3(10,1.5f,-36),new Vector3(5.5f,3,3.2f),Mat("InvisibleProxy",new Color(.94f,.86f,.64f))).GetComponent<Renderer>().enabled=false;
            Label("PRAÇA DO SOL",new Vector3(0,3.8f,-22),.2f,new Color(1,.83f,.43f));
            foreach(var spot in new[]{new Vector3(-10,0,-22),new Vector3(10,0,-22),new Vector3(-10,0,-12),new Vector3(10,0,-12)})Model("Custom/palm.fbx","Custom",world,spot,6);
            for(int n=0;n<4;n++)Model("Custom/bench.fbx","Custom",world,new Vector3(-6+n*4,0,-29),1.1f);
            Building("c",new Vector3(-49,0,-18),12,180);Building("d",new Vector3(-36,0,-17),10,180);
            Building("b",new Vector3(-50,0,-33),8);Building("a",new Vector3(-36,0,-33),7);
            Label("GARAGEM / SOL",new Vector3(-43,4,-41),.24f,new Color(1,.8f,.3f));
            Building("h",new Vector3(36,0,-18),13);Building("g",new Vector3(50,0,-17),15);
            Building("a",new Vector3(48,0,-33),8);Label("CAFÉ MARÉ",new Vector3(48,3,-40),.24f,new Color(1,.69f,.5f));
            Building("k",new Vector3(-49,0,23),16);Building("e",new Vector3(-34,0,20),12);
            Label("POLÍCIA COSTEIRA",new Vector3(-45,3,10),.23f,new Color(.45f,.7f,1));
            Cube("Parque das Palmeiras",new Vector3(0,.03f,24),new Vector3(28,.06f,29),grass);
            Cube("Caminho do parque",new Vector3(0,.07f,24),new Vector3(4,.08f,30),paving);
            Cube("Abrigo",new Vector3(8,1.6f,30),new Vector3(7,3.2f,5),Mat("Shelter",new Color(.32f,.59f,.5f)));
            Label("ABRIGO / BRISA",new Vector3(8,3,27.3f),.2f,new Color(.66f,1,.8f));
            for(int i=0;i<6;i++)Model("Custom/palm.fbx","Custom",world,new Vector3(i%2==0?-10:10,0,12+(i/2)*10),6.5f);
            Building("j",new Vector3(48,0,35),15);Building("f",new Vector3(34,0,35),11);
            Cube("Pátio do porto",new Vector3(44,.04f,18),new Vector3(28,.08f,18),paving);Label("PORTO / ACESSO RESTRITO",new Vector3(44,4,9),.22f,new Color(1,.57f,.31f));
            for(int n=0;n<7;n++) {var crate=Cube("Caixa de carga",new Vector3(33+(n%3)*3, .65f,14+(n/3)*3),Vector3.one*1.3f,Mat("Cargo",new Color(.78f,.4f,.19f)));var body=crate.AddComponent<Rigidbody>();body.mass=18;crate.AddComponent<ImpactSurface>().color=new Color(.7f,.46f,.24f);}
            // Distant skyline, outside the playable roads.
            for(int n=0;n<10;n++) Building(((char)('a'+n%8)).ToString(),new Vector3(-70+n*15,0,59),random.Next(12,22));
        }
        static void StreetProps()
        {
            var metal=Mat("Metal",new Color(.15f,.24f,.27f));var warm=Mat("Lamp",new Color(1,.84f,.5f),null,true);var coral=Mat("Coral",new Color(.91f,.3f,.18f));
            foreach(float z in new[]{-41f,7f,41f})for(float x=-72;x<=72;x+=24)
            {
                Primitive(PrimitiveType.Cylinder,"Poste",new Vector3(x,2.5f,z),new Vector3(.15f,2.5f,.15f),metal);
                Cube("Luminária",new Vector3(x,5.1f,z),new Vector3(.8f,.2f,.6f),warm,null,false);
            }
            for(int n=0;n<10;n++) {float x=-70+n*15;Model("Custom/palm.fbx","Custom",world,new Vector3(x,0,-58),6);Model("Custom/bench.fbx","Custom",world,new Vector3(x+4,.15f,-58),1.1f);}
            foreach(float x in new[]{-29f,15f,59f})foreach(float z in new[]{-40f,8f})
            {
                var bin=Primitive(PrimitiveType.Cylinder,"Lixeira",new Vector3(x,.55f,z),new Vector3(.65f,.5f,.65f),metal);bin.AddComponent<Rigidbody>().mass=30;bin.AddComponent<ImpactSurface>().color=Color.yellow;
                Primitive(PrimitiveType.Cylinder,"Hidrante",new Vector3(x+3,.4f,z),new Vector3(.34f,.4f,.34f),coral);
            }
            foreach(float x in new[]{-60f,-16f,28f,72f})foreach(float z in new[]{-6f,42f})
            {
                Cube("Semáforo poste",new Vector3(x,2,z),new Vector3(.12f,4,.12f),metal);
                Cube("Semáforo caixa",new Vector3(x,3.4f,z),new Vector3(.5f,1.2f,.35f),metal);
                Primitive(PrimitiveType.Sphere,"Sinal verde",new Vector3(x,3.1f,z-.19f),Vector3.one*.23f,Mat("TrafficGreen",new Color(.22f,.83f,.44f),null,true),null,false);
            }
        }
        static TextMesh Label(string text,Vector3 position,float size,Color color,Transform parent=null)
        {
            var obj=new GameObject(text);obj.transform.SetParent(parent?parent:world,false);obj.transform.localPosition=position;obj.layer=2;
            var label=obj.AddComponent<TextMesh>();label.text=text;label.characterSize=size*.15f;label.fontSize=64;label.anchor=TextAnchor.MiddleCenter;label.color=color;return label;
        }
        static AnimatorController BuildAnimator()
        {
            string modelPath=Root+"/Art/Kenney/Characters/character-a.fbx";
            var clips=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            Debug.Log("Costa Brava animações: "+string.Join(", ",clips.Select(c=>c.name)));
            AnimationClip Find(string name) => clips.FirstOrDefault(c=>c.name.Equals(name,StringComparison.OrdinalIgnoreCase))??clips.FirstOrDefault(c=>c.name.EndsWith("|"+name,StringComparison.OrdinalIgnoreCase))??clips.FirstOrDefault(c=>c.name.IndexOf(name,StringComparison.OrdinalIgnoreCase)>=0);
            var idle=Find("idle");var walk=Find("walk");var sprint=Find("sprint");var die=Find("die");
            if(!idle || !walk || !sprint)throw new Exception("Clips de locomoção não foram importados.");
            string path=Root+"/Data/Character.controller";
            var existing=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(existing)return existing;
            var controller=AnimatorController.CreateAnimatorControllerAtPath(path);controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("Vertical",AnimatorControllerParameterType.Float);controller.AddParameter("Grounded",AnimatorControllerParameterType.Bool);controller.AddParameter("Die",AnimatorControllerParameterType.Trigger);
            var state=controller.layers[0].stateMachine;var locomotion=state.AddState("Locomoção");state.defaultState=locomotion;
            var tree=new BlendTree {name="Idle · Walk · Run · Sprint",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,controller);tree.AddChild(idle,0);tree.AddChild(walk,2.5f);tree.AddChild(sprint,4.6f);tree.AddChild(sprint,7.5f);locomotion.motion=tree;
            var jump=state.AddState("Pulo e queda");jump.motion=idle;
            var transition=locomotion.AddTransition(jump);transition.hasExitTime=false;transition.duration=.1f;transition.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
            transition=jump.AddTransition(locomotion);transition.hasExitTime=false;transition.duration=.15f;transition.AddCondition(AnimatorConditionMode.If,0,"Grounded");
            var death=state.AddState("Morte");death.motion=die?die:idle;var deathTransition=state.AddAnyStateTransition(death);deathTransition.hasExitTime=false;deathTransition.duration=.1f;deathTransition.AddCondition(AnimatorConditionMode.If,0,"Die");
            return controller;
        }
        static GameObject CharacterModel(Transform root,string letter)
        {
            var visual=Model("Kenney/Characters/character-"+letter+".fbx","Characters",root,Vector3.zero,1.8f);
            var animator=visual.GetComponent<Animator>();if(!animator)animator=visual.AddComponent<Animator>();animator.runtimeAnimatorController=characterController;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
            return visual;
        }
        static WeaponDefinition Weapon(string name,int magazine,int reserve,float damage,float interval,bool automatic)
        {
            string path=Root+"/Data/"+name+".asset";var asset=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if(!asset) {asset=ScriptableObject.CreateInstance<WeaponDefinition>();AssetDatabase.CreateAsset(asset,path);}
            asset.displayName=name;asset.magazineSize=magazine;asset.startingReserve=reserve;asset.damage=damage;asset.fireInterval=interval;asset.automatic=automatic;asset.reloadTime=automatic?1.8f:1.25f;asset.recoil=automatic?.65f:1.5f;return asset;
        }
        static PlayerController Player()
        {
            var obj=new GameObject("Jogador / Alex");obj.layer=2;obj.transform.position=new Vector3(-8,.2f,-32);
            var controller=obj.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.3f;controller.center=new Vector3(0,.9f,0);controller.stepOffset=.35f;
            var player=obj.AddComponent<PlayerController>();player.controller=controller;
            var visual=CharacterModel(obj.transform,"a");player.visual=visual.transform;player.animator=visual.GetComponent<Animator>();
            player.health=obj.AddComponent<PlayerHealth>();player.combat=obj.AddComponent<PlayerCombat>();player.combat.player=player;
            obj.AddComponent<CharacterPose>().player=player;
            player.combat.weapons=new[]{Weapon("Pistola BR-12",12,60,27,.27f,false),Weapon("Carabina Sol-30",30,120,17,.095f,true)};
            var grip=new GameObject("Armas").transform;grip.SetParent(obj.transform,false);grip.localPosition=new Vector3(.42f,1.28f,.42f);
            var pistol=new GameObject("Pistola").transform;pistol.SetParent(grip,false);var rifle=new GameObject("Carabina").transform;rifle.SetParent(grip,false);
            var metal=Mat("GunMetal",new Color(.1f,.16f,.19f));var wood=Mat("GunAccent",new Color(.49f,.31f,.16f));
            Cube("Cano pistola",new Vector3(0,0,.12f),new Vector3(.09f,.11f,.3f),metal,pistol,false);Cube("Empunhadura",new Vector3(0,-.11f,0),new Vector3(.07f,.22f,.1f),wood,pistol,false);
            Cube("Carabina corpo",new Vector3(0,0,.17f),new Vector3(.11f,.15f,.45f),metal,rifle,false);Cube("Coronha",new Vector3(0,0,-.16f),new Vector3(.1f,.13f,.24f),wood,rifle,false);Cube("Cano",new Vector3(0,.02f,.52f),new Vector3(.05f,.05f,.3f),metal,rifle,false);
            var muzzle=new GameObject("Boca da arma").transform;muzzle.SetParent(grip,false);muzzle.localPosition=new Vector3(0,0,.35f);player.combat.muzzle=muzzle;player.combat.models=new[]{pistol.gameObject,rifle.gameObject};
            foreach(var t in obj.GetComponentsInChildren<Transform>())t.gameObject.layer=2;
            return player;
        }
        static VehicleController Car(string modelName,Vector3 position,float yaw,bool traffic=false,Vector3[] route=null)
        {
            var root=new GameObject(modelName+" / veículo");root.layer=9;
            var model=Model("Kenney/Vehicles/"+modelName+".fbx","Vehicles",root.transform,new Vector3(0,-.72f,0),4.4f,false);
            var box=root.AddComponent<BoxCollider>();box.size=new Vector3(1.8f,.95f,3.9f);box.center=new Vector3(0,0,0);
            root.AddComponent<Rigidbody>();var car=root.AddComponent<VehicleController>();car.traffic=traffic;car.route=route;car.cruiseSpeed=6+random.Next(0,4);car.maximumSpeed=modelName=="delivery"?20:26;
            car.wheels=model.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("wheel-")).ToArray();
            car.brakeLights=new[]{Cube("Freio esquerdo",new Vector3(-.6f,.04f,-2.03f),new Vector3(.3f,.14f,.08f),Mat("BrakeLight",new Color(.9f,.05f,.025f)),root.transform,false).GetComponent<Renderer>(),Cube("Freio direito",new Vector3(.6f,.04f,-2.03f),new Vector3(.3f,.14f,.08f),Mat("BrakeLight",new Color(.9f,.05f,.025f)),root.transform,false).GetComponent<Renderer>()};
            foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=9;
            root.transform.position=position;root.transform.rotation=Quaternion.Euler(0,yaw,0);return car;
        }
        static void SetupVehicles(GameManager game)
        {
            game.missions.deliveryVan=Car("delivery",new Vector3(-42,.75f,-42),90);
            Car("sedan-sports",new Vector3(-15,.75f,-39),0);Car("suv",new Vector3(58,.75f,-36),180);Car("sedan",new Vector3(-31,.75f,39),90);
            var loop=new[]{new Vector3(-64,0,-50),new Vector3(68,0,-50),new Vector3(68,0,46),new Vector3(-64,0,46)};
            for(int n=0;n<4;n++) {var rotated=Enumerable.Range(0,4).Select(i=>loop[(i+n)%4]).ToArray();Vector3 forward=rotated[1]-rotated[0];Car(n%2==0?"taxi":"hatchback-sports",rotated[0]+Vector3.up*.75f,Quaternion.LookRotation(forward).eulerAngles.y,true,rotated);}
            var police=Car("police",new Vector3(0,-200,0),0);police.IsPolice=true;police.traffic=false;
            game.wanted.policeCarPrefab=PrefabUtility.SaveAsPrefabAsset(police.gameObject,Root+"/Data/Prefabs/PoliceCar.prefab");UnityEngine.Object.DestroyImmediate(police.gameObject);
        }
        static NPCController NPC(NPCType type,Vector3 position,string letter)
        {
            var root=new GameObject(type.ToString());root.layer=8;var visual=CharacterModel(root.transform,letter);
            var hitbox=root.AddComponent<CapsuleCollider>();hitbox.center=new Vector3(0,.9f,0);hitbox.height=1.8f;hitbox.radius=.33f;
            var agent=root.AddComponent<NavMeshAgent>();agent.radius=.32f;agent.height=1.8f;agent.speed=1.4f;agent.angularSpeed=360;agent.acceleration=16;agent.stoppingDistance=type==NPCType.Pedestrian?.3f:5;
            var npc=root.AddComponent<NPCController>();npc.type=type;npc.animator=visual.GetComponent<Animator>();npc.hitbox=hitbox;npc.maximumHealth=type==NPCType.Police?90:75;
            if(type!=NPCType.Pedestrian)
            {
                var gun=Cube("Arma",new Vector3(.4f,1.25f,.45f),new Vector3(.09f,.12f,.35f),Mat("GunMetal",new Color(.1f,.16f,.19f)),root.transform,false);
                var muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(root.transform,false);muzzle.localPosition=new Vector3(.4f,1.25f,.65f);npc.muzzle=muzzle;
            }
            foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=8;
            if(NavMesh.SamplePosition(position,out var spot,10,NavMesh.AllAreas))position=spot.position;root.transform.position=position;return npc;
        }
        static void SetupPeople(GameManager game)
        {
            var locations=new[]{new Vector3(-14,0,-15),new Vector3(15,0,-26),new Vector3(-28,0,-6),new Vector3(29,0,-6),new Vector3(-14,0,7),new Vector3(15,0,27),new Vector3(57,0,-8),new Vector3(-57,0,7),new Vector3(4,0,-57),new Vector3(43,0,-57),new Vector3(-35,0,-57),new Vector3(15,0,42)};
            for(int n=0;n<locations.Length;n++)NPC(NPCType.Pedestrian,locations[n],((char)('b'+n%12)).ToString());
            game.missions.enemies=new[]{NPC(NPCType.Hostile,new Vector3(40,0,13),"q"),NPC(NPCType.Hostile,new Vector3(51,0,20),"r"),NPC(NPCType.Hostile,new Vector3(40,0,23),"q")};foreach(var enemy in game.missions.enemies)enemy.missionEnemy=true;
            var officer=NPC(NPCType.Police,new Vector3(-40,0,9),"n");game.wanted.policePrefab=PrefabUtility.SaveAsPrefabAsset(officer.gameObject,Root+"/Data/Prefabs/Police.prefab");UnityEngine.Object.DestroyImmediate(officer.gameObject);
            NPC(NPCType.Police,new Vector3(-45,0,9),"n").responder=false;
            NPC(NPCType.Police,new Vector3(-18,0,-6),"n").responder=false;
        }
        static Transform Marker(string title,Vector3 position,Color color)
        {
            var root=new GameObject(title).transform;root.position=position;root.gameObject.layer=2;
            var material=Mat("Marker-"+title.Replace(" ","_").Replace("/","_"),color,null,true);
            Primitive(PrimitiveType.Cylinder,"Área",new Vector3(0,.035f,0),new Vector3(2.5f,.03f,2.5f),material,root,false).layer=2;
            var diamond=Primitive(PrimitiveType.Cube,"Marcador",new Vector3(0,2.2f,0),Vector3.one*.45f,material,root,false);diamond.transform.localRotation=Quaternion.Euler(0,0,45);diamond.layer=2;
            var label=Label(title,new Vector3(0,3,0),.11f,color,root);var marker=root.gameObject.AddComponent<WorldMarker>();marker.icon=diamond.transform;marker.label=label.transform;return root;
        }
        static void SetupMissions(GameManager game)
        {
            var gold=new Color(1,.75f,.25f);
            game.missions.starts=new[]{Marker("ENTREGA EXPRESSA",new Vector3(-9,.2f,-27),gold),Marker("CARGA PERDIDA",new Vector3(7,.2f,-27),gold),Marker("ÚLTIMA SAÍDA",new Vector3(0,.2f,-12),gold)};
            game.missions.delivery=Marker("ENTREGAR / CAFÉ MARÉ",new Vector3(42,.1f,-42),new Color(.25f,.7f,1));
            game.missions.yard=Marker("PÁTIO DO PORTO",new Vector3(47,.15f,17),new Color(1,.4f,.2f));
            game.missions.package=new GameObject("Ponto de carga").transform;game.missions.package.position=new Vector3(47,.2f,17);
            game.missions.packageVisual=Cube("Carga recuperável",new Vector3(47,.5f,17),Vector3.one*.7f,Mat("MissionCargo",new Color(1,.55f,.15f)));
            game.missions.safe=Marker("ABRIGO SEGURO",new Vector3(8,.1f,23),new Color(.3f,.95f,.65f));
            game.shop.counter=Marker("VIVA / LOJA",new Vector3(10,.2f,-32.5f),new Color(.3f,.9f,.7f));
        }
        static void SetupAudio(AudioManager audio)
        {
            audio.footsteps=AssetDatabase.FindAssets("t:AudioClip",new[]{Root+"/Audio/Kenney"}).Select(g=>AssetDatabase.GUIDToAssetPath(g)).Where(p=>p.Contains("footstep_concrete")).Select(p=>AssetDatabase.LoadAssetAtPath<AudioClip>(p)).ToArray();
            audio.grassFootsteps=AssetDatabase.FindAssets("t:AudioClip",new[]{Root+"/Audio/Kenney"}).Select(g=>AssetDatabase.GUIDToAssetPath(g)).Where(p=>p.Contains("footstep_grass")).Select(p=>AssetDatabase.LoadAssetAtPath<AudioClip>(p)).ToArray();
            audio.impactClip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Kenney/impactMetal_light_000.ogg");audio.explorationMusic=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/coast.wav");audio.pursuitMusic=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/pursuit.wav");
        }
        [MenuItem("Tools/Costa Brava/Validar cena")]
        public static void Validate()
        {
            var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();var errors=new List<string>();
            if(!game)errors.Add("GameManager ausente");
            else
            {
                if(!game.player || !game.followCamera || !game.wanted || !game.missions || !game.shop || !game.audioManager)errors.Add("Referências de sistemas incompletas");
                if(game.player.combat.weapons.Length<2 || game.player.combat.weapons.Length>3)errors.Add("Configuração de armas inválida");
                if(game.missions.starts.Length!=3 || game.missions.enemies.Length!=3)errors.Add("Configuração de missões inválida");
                if(!game.wanted.policePrefab || !game.wanted.policeCarPrefab)errors.Add("Prefabs policiais ausentes");
                if(!game.audioManager.explorationMusic || !game.audioManager.pursuitMusic || game.audioManager.footsteps.Length==0)errors.Add("Áudio incompleto");
                foreach(var start in game.missions.starts.Concat(new[]{game.missions.safe,game.missions.delivery,game.shop.counter}))
                {
                    if(!NavMesh.SamplePosition(start.position,out var hit,2,NavMesh.AllAreas))errors.Add("Marcador fora do NavMesh: "+start.name);
                    else {var path=new NavMeshPath();if(!NavMesh.CalculatePath(game.spawnPoint,hit.position,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete)errors.Add("Marcador sem caminho: "+start.name);}
                }
            }
            string report=errors.Count==0?"PASS · cena configurada, 2 armas, 3 missões, veículos, áudio, polícia e caminhos NavMesh verificados.":string.Join("\n",errors);
            System.IO.Directory.CreateDirectory("Documentation");System.IO.File.WriteAllText("Documentation/SceneValidation.txt",report);Debug.Log("Costa Brava validação: "+report);
        }
        [MenuItem("Tools/Costa Brava/Finalizar apresentação e oclusão")]
        public static void FinalizeScene()
        {
            if(EditorApplication.isPlaying)return;
            // This machine uses OpenGL; keep regular batching/instancing without the unsupported resident drawer.
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            if(pipeline) {var serialized=new SerializedObject(pipeline);var resident=serialized.FindProperty("m_GPUResidentDrawerMode");if(resident!=null) {resident.intValue=0;serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();}}
            foreach(var label in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))if(label.characterSize>.05f)label.characterSize*=.15f;
            StaticOcclusionCulling.smallestOccluder=3;StaticOcclusionCulling.smallestHole=.5f;StaticOcclusionCulling.backfaceThreshold=100;
            bool baked=StaticOcclusionCulling.Compute();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            System.IO.File.WriteAllText("Documentation/Occlusion.txt",baked?"PASS · Occlusion Culling calculado para a cidade.":"Occlusion Culling não foi calculado pelo editor.");
            Debug.Log("Costa Brava oclusão: "+baked);Validate();
        }
    }
    public class CostaBravaModelImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.StartsWith(CostaBravaBuilder.Root+"/Art/"))return;
            var importer=(ModelImporter)assetImporter;importer.importCameras=false;importer.importLights=false;
            if(assetPath.Contains("/Characters/")) {importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;}
        }
        void OnPreprocessAnimation()
        {
            if(!assetPath.Contains("CostaBrava/Art/Kenney/Characters/"))return;
            var importer=(ModelImporter)assetImporter;var clips=importer.defaultClipAnimations;
            foreach(var clip in clips) {clip.loopTime=!clip.name.Contains("die") && !clip.name.Contains("pick-up");clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;}
            importer.clipAnimations=clips;
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(CostaBravaBuilder.Root+"/Art/"))return;
            var importer=(TextureImporter)assetImporter;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Compressed;
        }
    }
}

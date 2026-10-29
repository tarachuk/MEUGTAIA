using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.Rendering.Universal;

namespace CostaBrava.Editor
{
    [InitializeOnLoad]
    public static class CostaBravaExpansionInstaller
    {
        const string Root="Assets/CostaBrava";
        const string Expansion="Expansão / Bairro Farol";
        static Transform parent;
        static CostaBravaExpansionInstaller() {EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!File.Exists("Library/CostaBravaUpgrade.flag"))return;
            File.Delete("Library/CostaBravaUpgrade.flag");Upgrade();
        }
        [MenuItem("Tools/Costa Brava/Instalar expansão Farol na cena atual")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying)return;
            var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();if(!game) {Debug.LogError("Abra a cena CostaBrava antes da expansão.");return;}
            var scene=EditorSceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Documentation/Backups");File.Copy(scene.path,"Documentation/Backups/active-before-farol-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            var nav=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();if(!nav)throw new Exception("NavMeshSurface ausente.");
            var existing=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name==Expansion);
            if(!existing)
            {
                parent=new GameObject(Expansion).transform;parent.SetParent(nav.transform,false);
                ExtendGround(nav.transform);AddBlocks();RepairSidewalks(nav.transform);
            }
            else parent=existing;
            UpgradeActors(game);InstallSystems(game);InstallArsenal(game);
            PopulateDistrict();
            foreach(var enemy in game.missions.enemies)if(enemy)enemy.gameObject.SetActive(false);
            foreach(var body in UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if(body.GetComponent<VehicleController>())continue;
                if(body.name.Contains("Caixa") || body.name.Contains("Lixeira") || body.name.Contains("Barril"))
                {
                    if(!body.GetComponent<DestructibleProp>())body.gameObject.AddComponent<DestructibleProp>();
                    foreach(var t in body.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
                }
            }
            nav.BuildNavMesh();
            if(nav.navMeshData && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(nav.navMeshData)))AssetDatabase.CreateAsset(nav.navMeshData,Root+"/Data/FarolNavMesh-"+DateTime.Now.ToString("HHmmss")+".asset");
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            StaticOcclusionCulling.Compute();EditorSceneManager.SaveScene(scene);
            Validate();Debug.Log("FAROL: expansão instalada na cena existente; oito quarteirões, ruas cortadas, arsenal e sistemas configurados.");
        }
        static Material Material(string name,Color color,bool unlit=false)
        {
            string path=Root+"/Data/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) {m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.enableInstancing=true;return m;
        }
        static GameObject Box(string name,Vector3 position,Vector3 size,Material material,bool collider=true)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider)UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;
        }
        static Bounds ModelBounds(GameObject obj)
        {
            var renderers=obj.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;
        }
        static Color CustomColor(string name)
        {
            if(name.Contains("navy") || name.Contains("metal"))return new Color(.035f,.11f,.16f);
            if(name.Contains("coral"))return new Color(.92f,.31f,.19f);
            if(name.Contains("glass"))return new Color(.2f,.65f,.85f);
            if(name.Contains("wood"))return new Color(.48f,.26f,.12f);
            if(name.Contains("mint") || name.Contains("leaf"))return new Color(.12f,.55f,.37f);
            return new Color(.94f,.86f,.64f);
        }
        static GameObject Model(string relative,Vector3 position,float size,bool height=true,Transform owner=null)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Art/"+relative);if(!source)throw new Exception("Arte não importada: "+relative);
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(source);PrefabUtility.UnpackPrefabInstance(obj,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            obj.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);obj.transform.localScale=Vector3.one;
            var bounds=ModelBounds(obj);float factor=size/(height?bounds.size.y:bounds.size.z);obj.transform.localScale=Vector3.one*factor;obj.transform.position=-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*factor;
            obj.transform.SetParent(owner?owner:parent,false);obj.transform.localPosition+=position;
            bool building=relative.Contains("Buildings/");var texture=building?AssetDatabase.LoadAssetAtPath<Texture>(Root+"/Art/Kenney/Buildings/Textures/colormap.png"):null;
            foreach(var renderer in obj.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(old=>
                {
                    var material=Material(building?"Buildings":"Farol-"+(old?old.name:"cream").Replace(" ","_"),building?Color.white:CustomColor(old?old.name:"cream"));if(texture)material.mainTexture=texture;return material;
                }).ToArray();
            }
            return obj;
        }
        static void ExtendGround(Transform world)
        {
            var originals=world.GetComponentsInChildren<Transform>().ToArray();
            foreach(var t in originals)
            {
                bool extend=t.name=="Ilha urbana" || t.name=="Rua transversal" || t.name=="Passeio à beira-mar" || t.name=="Muro beira-mar" || t.name=="Limite norte" || (t.name=="Calçada" && t.localScale.x>100);
                if(extend) {Undo.RecordObject(t,"Ampliar cidade a leste");var scale=t.localScale;scale.x+=44;t.localScale=scale;t.localPosition+=Vector3.right*22;}
                if(t.name=="Limite lateral" && t.localPosition.x>0) {Undo.RecordObject(t,"Limite leste");t.localPosition+=Vector3.right*44;}
            }
            var road=Material("Asphalt",new Color(.16f,.22f,.25f));var walk=Material("Sidewalk",new Color(.7f,.7f,.61f));var stripe=Material("RoadStripe",new Color(.97f,.87f,.54f));
            Box("Avenida",new Vector3(110,.005f,0),new Vector3(10,.02f,108),road);
            foreach(float x in new[]{103.8f,116.2f})Box("Calçada",new Vector3(x,.07f,0),new Vector3(2.4f,.14f,108),walk);
            for(float z=-44;z<49;z+=7)if(Mathf.Abs(z)>9 && Mathf.Abs(z-48)>9 && Mathf.Abs(z+48)>9)Box("Faixa central",new Vector3(110,.025f,z),new Vector3(.18f,.02f,3),stripe,false);
            foreach(float z in new[]{-48f,0f,48f})
            {
                Box("Cruzamento",new Vector3(110,.03f,z),new Vector3(14,.02f,10),road,false);
                for(float x=76;x<108;x+=7)Box("Faixa central",new Vector3(x,.028f,z),new Vector3(3,.02f,.18f),stripe,false);
            }
        }
        static void RepairSidewalks(Transform world)
        {
            var sidewalks=world.GetComponentsInChildren<Transform>().Where(t=>t.name=="Calçada").ToArray();
            float[] xs={-66,-22,22,66,110};float[] zs={-48,0,48};
            foreach(var t in sidewalks)
            {
                bool horizontal=t.localScale.x>t.localScale.z;float length=horizontal?t.localScale.x:t.localScale.z;
                if(length<20)continue;
                Vector3 p=t.position;Vector3 scale=t.lossyScale;float start=(horizontal?p.x:p.z)-length/2;float end=start+length;float cursor=start;
                var material=t.GetComponent<Renderer>().sharedMaterial;var owner=t.parent;
                foreach(float road in horizontal?xs:zs)
                {
                    float low=Mathf.Max(start,road-5.1f),high=Mathf.Min(end,road+5.1f);if(high<=start || low>=end)continue;
                    if(low>cursor+.05f)Segment(cursor,low);cursor=Mathf.Max(cursor,high);
                }
                if(end>cursor+.05f)Segment(cursor,end);
                Undo.DestroyObjectImmediate(t.gameObject);
                void Segment(float a,float b)
                {
                    var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name="Calçada recortada";obj.transform.SetParent(owner,true);var pos=p;if(horizontal)pos.x=(a+b)/2;else pos.z=(a+b)/2;obj.transform.position=pos;var dimensions=scale;if(horizontal)dimensions.x=b-a;else dimensions.z=b-a;obj.transform.localScale=dimensions;obj.GetComponent<Renderer>().sharedMaterial=material;
                }
            }
            var marking=Material("PedestrianWhite",new Color(.95f,.96f,.86f));
            foreach(var t in world.GetComponentsInChildren<Transform>().Where(t=>t.name=="Faixa de pedestres").ToArray())Undo.DestroyObjectImmediate(t.gameObject);
            foreach(float x in xs)foreach(float z in zs)for(int n=-3;n<=3;n++)
            {
                foreach(float side in new[]{-1f,1f})
                {
                    Box("Faixa de pedestres",new Vector3(x+n*.85f,.055f,z+side*7),new Vector3(.45f,.02f,2.4f),marking,false);
                    Box("Faixa de pedestres",new Vector3(x+side*7,.055f,z+n*.85f),new Vector3(2.4f,.02f,.45f),marking,false);
                }
            }
        }
        static void Building(string letter,Vector3 position,float height)
        {
            var model=Model("Kenney/Buildings/building-"+letter+".fbx",position,height);var b=ModelBounds(model);float width=Mathf.Max(b.size.x,b.size.z);
            if(width>11) {var scale=model.transform.localScale;scale.x*=11/width;scale.z*=11/width;model.transform.localScale=scale;b=ModelBounds(model);}
            var box=model.AddComponent<BoxCollider>();box.center=model.transform.InverseTransformPoint(b.center);box.size=Vector3.Scale(b.size,new Vector3(1/model.transform.lossyScale.x,1/model.transform.lossyScale.y,1/model.transform.lossyScale.z));
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
        }
        static void AddBlocks()
        {
            var shop=Model("Custom/farol-arms.fbx",new Vector3(88,.1f,-35),4.5f);var bounds=ModelBounds(shop);
            var collider=shop.AddComponent<BoxCollider>();collider.center=shop.transform.InverseTransformPoint(bounds.center);collider.size=Vector3.Scale(bounds.size,new Vector3(1/shop.transform.lossyScale.x,1/shop.transform.lossyScale.y,1/shop.transform.lossyScale.z));
            // A small exterior counter keeps interaction reachable without an interior scene.
            Building("h",new Vector3(80,0,-16),12);Building("f",new Vector3(96,0,-16),10);
            Building("d",new Vector3(80,0,33),11);Building("g",new Vector3(96,0,33),14);
            Box("Jardim Farol",new Vector3(88,.035f,17),new Vector3(23,.07f,14),Material("ParkGrass",new Color(.3f,.5f,.31f)));
            Model("Custom/palm.fbx",new Vector3(79,0,14),6);Model("Custom/palm.fbx",new Vector3(98,0,14),6);
            Model("Custom/bench.fbx",new Vector3(88,0,12),1.1f);
            for(int i=0;i<6;i++)
            {
                var barrel=Model("Custom/breakable-barrel.fbx",new Vector3(76+i%3*2,0,23+i/3*2),1.2f);barrel.name="Barril destrutível";
                var b=ModelBounds(barrel);var box=barrel.AddComponent<BoxCollider>();box.center=barrel.transform.InverseTransformPoint(b.center);box.size=Vector3.Scale(b.size,new Vector3(1/barrel.transform.lossyScale.x,1/barrel.transform.lossyScale.y,1/barrel.transform.lossyScale.z));barrel.AddComponent<Rigidbody>().mass=12;barrel.AddComponent<DestructibleProp>();
            }
            foreach(float z in new[]{-41f,7f,41f})foreach(float x in new[]{79f,101f,117f})
            {
                Box("Poste",new Vector3(x,2.5f,z),new Vector3(.15f,5,.15f),Material("Metal",new Color(.15f,.24f,.27f)));
                Box("Luminária",new Vector3(x,5.1f,z),new Vector3(.8f,.2f,.6f),Material("Lamp",new Color(1,.84f,.5f),true),false);
            }
        }
        static void UpgradeActors(GameManager game)
        {
            foreach(var car in UnityEngine.Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None))AddVehicleSystems(car);
            string police=AssetDatabase.GetAssetPath(game.wanted.policeCarPrefab);
            var prefab=PrefabUtility.LoadPrefabContents(police);AddVehicleSystems(prefab.GetComponent<VehicleController>());PrefabUtility.SaveAsPrefabAsset(prefab,police);PrefabUtility.UnloadPrefabContents(prefab);
            if(!game.missions.replacementVanPrefab)
            {
                var copy=UnityEngine.Object.Instantiate(game.missions.deliveryVan.gameObject);game.missions.replacementVanPrefab=PrefabUtility.SaveAsPrefabAsset(copy,Root+"/Data/Prefabs/ReplacementVan.prefab");UnityEngine.Object.DestroyImmediate(copy);
            }
        }
        static void AddVehicleSystems(VehicleController car)
        {
            if(!car.GetComponent<VehicleDamage>())car.gameObject.AddComponent<VehicleDamage>();
            var lights=car.GetComponent<AutomaticLights>();if(!lights)lights=car.gameObject.AddComponent<AutomaticLights>();lights.vehicle=true;
            car.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeRotation;
        }
        static void PopulateDistrict()
        {
            if(!parent.Find("Pedestre Farol 1"))
            {
                var source=UnityEngine.Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).FirstOrDefault(n=>n.type==NPCType.Pedestrian);
                if(source)
                {
                    Vector3[] points={new Vector3(76,.15f,-25),new Vector3(100,.15f,-25),new Vector3(77,.15f,12),new Vector3(100,.15f,27)};
                    for(int i=0;i<points.Length;i++) {var clone=UnityEngine.Object.Instantiate(source.gameObject,parent);clone.name="Pedestre Farol "+(i+1);clone.transform.position=points[i];}
                }
            }
            foreach(var car in UnityEngine.Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None))if(car.traffic && car.route!=null)
            {
                bool changed=false;for(int i=0;i<car.route.Length;i++)if(car.route[i].x>60 && car.route[i].x<100) {car.route[i]+=Vector3.right*44;changed=true;}
                if(changed && car.transform.position.x>60 && car.transform.position.x<100)car.transform.position+=Vector3.right*44;
            }
            if(!parent.Cast<Transform>().Any(t=>t.name=="Carro estacionado / Farol"))
            {
                var source=UnityEngine.Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None).FirstOrDefault(c=>!c.IsPolice && !c.traffic && c.name.Contains("sedan"));
                if(source) {var clone=UnityEngine.Object.Instantiate(source.gameObject,parent);clone.name="Carro estacionado / Farol";clone.transform.SetPositionAndRotation(new Vector3(103.5f,.9f,-33),Quaternion.Euler(0,180,0));foreach(var t in clone.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);}
            }
        }
        static void InstallSystems(GameManager game)
        {
            var melee=game.player.GetComponent<PlayerMelee>();if(!melee)melee=game.player.gameObject.AddComponent<PlayerMelee>();melee.player=game.player;
            if(!game.GetComponent<MiniMap>())game.gameObject.AddComponent<MiniMap>();
            var clock=game.GetComponent<DayNightCycle>();if(!clock)clock=game.gameObject.AddComponent<DayNightCycle>();clock.sun=RenderSettings.sun;
            if(!game.followCamera.GetComponent<SpeedPostEffects>())game.followCamera.gameObject.AddComponent<SpeedPostEffects>();
            game.followCamera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
            if(!game.weaponShop)game.weaponShop=game.gameObject.AddComponent<WeaponShop>();
            if(!game.weaponShop.counter)
            {
                var marker=Box("FAROL / Loja de armas",new Vector3(88,.04f,-28),new Vector3(2.6f,.08f,2.6f),Material("FarolMarker",new Color(1,.62f,.23f),true),false);marker.layer=2;game.weaponShop.counter=marker.transform;
                var label=new GameObject("FAROL · E / RIFLE $100").AddComponent<TextMesh>();label.transform.SetParent(marker.transform,false);label.transform.localPosition=new Vector3(0,30,0);label.transform.localScale=new Vector3(1/2.6f,1/.08f,1/2.6f);label.characterSize=.02f;label.fontSize=48;label.text="FAROL · E / RIFLE $100";label.anchor=TextAnchor.MiddleCenter;label.color=new Color(1,.73f,.4f);label.gameObject.layer=2;
            }
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))if(t.name=="Luminária")
            {
                var automatic=t.GetComponent<AutomaticLights>();if(!automatic)automatic=t.gameObject.AddComponent<AutomaticLights>();automatic.fixture=t.GetComponent<Renderer>();
                if(automatic.lights==null || automatic.lights.Length==0)
                {
                    var lamp=new GameObject("Luz automática de poste").AddComponent<Light>();lamp.transform.SetParent(t,false);lamp.transform.localPosition=new Vector3(0,-3,0);lamp.type=LightType.Point;lamp.range=13;lamp.intensity=3;lamp.color=new Color(1,.78f,.47f);lamp.shadows=LightShadows.None;lamp.enabled=false;automatic.lights=new[]{lamp};
                }
            }
        }
        static void InstallArsenal(GameManager game)
        {
            string path=Root+"/Data/Rifle Farol.asset";var rifle=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if(!rifle) {rifle=ScriptableObject.CreateInstance<WeaponDefinition>();AssetDatabase.CreateAsset(rifle,path);}
            rifle.displayName="Rifle Farol";rifle.magazineSize=5;rifle.startingReserve=20;rifle.damage=90;rifle.fireInterval=.9f;rifle.reloadTime=1.9f;rifle.range=250;rifle.recoil=3;rifle.scoped=true;rifle.scopeFov=18;
            var combat=game.player.combat;
            if(combat.models.Length<3)
            {
                var grip=combat.models[0].transform.parent;
                var model=Model("Custom/farol-rifle.fbx",new Vector3(0,-.11f,.12f),.95f,false,grip);model.name="Rifle Farol / inventário 3";
                foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=2;model.SetActive(false);combat.models=combat.models.Take(2).Concat(new[]{model}).ToArray();
            }
            combat.weapons=combat.weapons.Take(2).Concat(new[]{rifle}).ToArray();EditorUtility.SetDirty(rifle);
        }
        [MenuItem("Tools/Costa Brava/Validar expansão Farol")]
        public static void Validate()
        {
            var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();var errors=new List<string>();
            if(!game || !game.weaponShop || game.player.combat.weapons.Length!=3)errors.Add("Arsenal/loja incompletos");
            if(game)
            {
                if(!game.GetComponent<MiniMap>() || !game.GetComponent<DayNightCycle>() || !game.player.GetComponent<PlayerMelee>())errors.Add("Sistemas ausentes");
                foreach(var enemy in game.missions.enemies)if(enemy.gameObject.activeSelf)errors.Add("Inimigo de missão ativo na exploração");
                if(!NavMesh.SamplePosition(game.weaponShop.counter.position,out var sample,2,NavMesh.AllAreas))errors.Add("Loja fora do NavMesh");
                else {var path=new NavMeshPath();if(!NavMesh.CalculatePath(game.spawnPoint,sample.position,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete)errors.Add("Loja sem caminho acessível");}
            }
            foreach(var box in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))if(box.name=="Calçada recortada")
            {
                var b=box.bounds;foreach(float x in new[]{-66f,-22f,22f,66f,110f})foreach(float z in new[]{-48f,0f,48f})if(b.min.x<x+4.9f && b.max.x>x-4.9f && b.min.z<z+4.9f && b.max.z>z-4.9f)errors.Add("Calçada invade cruzamento");
            }
            File.WriteAllLines("Documentation/FarolSceneChecks.txt",errors.Count==0?new[]{"PASS · Calçadas, oito quarteirões, loja acessível, três armas e inimigos de missão inativos."}:errors.ToArray());
            if(errors.Count>0)Debug.LogError("FAROL validação: "+string.Join("; ",errors));else Debug.Log("FAROL validação: PASS");
        }
    }
}

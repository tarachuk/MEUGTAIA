// Local file-based bridge. Enabled explicitly from the Unity Tools menu.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PcUnityBridge
{
    [Serializable] class Request {
        public string id, action, name, primitive;
        public double expires;
        public int objectId;
        public float[] position, rotation, scale;
    }
    [Serializable] class Item {
        public int id;
        public string name;
        public int parentId;
        public float[] position, rotation, scale;
    }
    [Serializable] class Response {
        public bool ok;
        public string id, error, scenePath;
        public int objectId;
        public bool truncated;
        public List<Item> objects = new List<Item>();
    }
    [Serializable] class Heartbeat { public string version = "0.1.0"; }
    const string EnabledKey = "PcUnityBridge.Enabled";
    static double nextPoll, nextHeartbeat;
    // Plugin handles are independent of Unity's version-specific object ID API.
    static readonly Dictionary<int, GameObject> Handles = new Dictionary<int, GameObject>();
    static readonly Dictionary<GameObject, int> ObjectHandles = new Dictionary<GameObject, int>();
    static int nextHandle = new System.Random().Next(1, 1000000000);
    static string Root { get { return Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library/PcUnityBridge"); } }
    static PcUnityBridge() { EditorApplication.update += Tick; }

    [MenuItem("Tools/PC e Unity/Ativar ponte")]
    static void Enable() {
        SessionState.SetBool(EnabledKey, true);
        Directory.CreateDirectory(Path.Combine(Root, "requests"));
        Directory.CreateDirectory(Path.Combine(Root, "responses"));
        nextHeartbeat = 0;
        Debug.Log("PC e Unity: ponte local ativada nesta sessão.");
    }
    [MenuItem("Tools/PC e Unity/Desativar ponte")]
    static void Disable() {
        SessionState.SetBool(EnabledKey, false);
        string hb = Path.Combine(Root, "heartbeat.json");
        if (File.Exists(hb)) File.Delete(hb);
        Debug.Log("PC e Unity: ponte desativada.");
    }
    static void Write(string path, string contents) {
        string temp = path + ".tmp";
        File.WriteAllText(temp, contents);
        if (File.Exists(path)) File.Delete(path);
        File.Move(temp, path);
    }
    static void Tick() {
        if (!SessionState.GetBool(EnabledKey, false) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        double now = EditorApplication.timeSinceStartup;
        if (now < nextPoll) return;
        nextPoll = now + 0.1;
        try {
            Directory.CreateDirectory(Path.Combine(Root, "requests"));
            Directory.CreateDirectory(Path.Combine(Root, "responses"));
            if (now >= nextHeartbeat) {
                Write(Path.Combine(Root, "heartbeat.json"), JsonUtility.ToJson(new Heartbeat()));
                nextHeartbeat = now + 2;
            }
            string[] files = Directory.GetFiles(Path.Combine(Root, "requests"), "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            int processed = 0;
            foreach (string file in files) {
                if (processed++ >= 8) break;
                string claimed = file + ".processing";
                try { File.Move(file, claimed); } catch (IOException) { continue; }
                Response response = new Response { id = Path.GetFileNameWithoutExtension(file) };
                try {
                    Request req = JsonUtility.FromJson<Request>(File.ReadAllText(claimed));
                    if (req == null || req.id != response.id || !Guid.TryParseExact(req.id, "N", out _)) throw new Exception("Requisição inválida.");
                    double utc = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                    if (req.expires <= utc) throw new Exception("Requisição expirada; nenhuma ação executada.");
                    Execute(req, response);
                    response.ok = true;
                } catch (Exception ex) { response.error = ex.Message; }
                try { Write(Path.Combine(Root, "responses", response.id + ".json"), JsonUtility.ToJson(response)); }
                finally { File.Delete(claimed); }
            }
        } catch (Exception ex) { Debug.LogWarning("PC e Unity: " + ex.Message); }
    }
    static float[] Values(Vector3 v) { return new float[] { v.x, v.y, v.z }; }
    static int Handle(GameObject obj) {
        int id;
        if (ObjectHandles.TryGetValue(obj, out id)) return id;
        id = nextHandle++;
        Handles[id] = obj;
        ObjectHandles[obj] = id;
        return id;
    }
    static Vector3 Vector(float[] v) {
        if (v == null || v.Length != 3) throw new Exception("Vetor inválido.");
        foreach (float f in v) if (float.IsNaN(f) || float.IsInfinity(f)) throw new Exception("Número inválido.");
        return new Vector3(v[0], v[1], v[2]);
    }
    static void Collect(Transform t, Response r) {
        if (r.objects.Count >= 1000) { r.truncated = true; return; }
        r.objects.Add(new Item { id = Handle(t.gameObject), name = t.name,
            parentId = t.parent == null ? 0 : Handle(t.parent.gameObject),
            position = Values(t.localPosition), rotation = Values(t.localEulerAngles), scale = Values(t.localScale) });
        foreach (Transform child in t) Collect(child, r);
    }
    static void Execute(Request req, Response r) {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) throw new Exception("Nenhuma cena ativa.");
        r.scenePath = scene.path;
        if (req.action == "scene") {
            foreach (GameObject root in scene.GetRootGameObjects()) Collect(root.transform, r);
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Saia do Play Mode para editar a cena.");
        if (req.action == "create") {
            if (String.IsNullOrWhiteSpace(req.name) || req.name.Length > 120) throw new Exception("Nome inválido.");
            PrimitiveType type;
            if (req.primitive != "Empty" && (!Enum.TryParse(req.primitive, out type) || !Enum.IsDefined(typeof(PrimitiveType), type))) throw new Exception("Tipo inválido.");
            GameObject obj = req.primitive == "Empty" ? new GameObject(req.name) : GameObject.CreatePrimitive((PrimitiveType)Enum.Parse(typeof(PrimitiveType), req.primitive));
            obj.name = req.name;
            SceneManager.MoveGameObjectToScene(obj, scene);
            Undo.RegisterCreatedObjectUndo(obj, "PC e Unity: criar objeto");
            EditorSceneManager.MarkSceneDirty(scene);
            r.objectId = Handle(obj);
        } else if (req.action == "transform") {
            GameObject obj;
            if (!Handles.TryGetValue(req.objectId, out obj) || obj == null || obj.scene != scene) throw new Exception("Objeto não pertence à cena ativa; consulte a cena novamente.");
            Vector3 pos = Vector(req.position), rot = Vector(req.rotation), scale = Vector(req.scale);
            Undo.RecordObject(obj.transform, "PC e Unity: transformar objeto");
            obj.transform.localPosition = pos;
            obj.transform.localEulerAngles = rot;
            obj.transform.localScale = scale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            r.objectId = Handle(obj);
        } else if (req.action == "save") {
            if (String.IsNullOrEmpty(scene.path)) throw new Exception("Salve a cena pela primeira vez no editor.");
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Não foi possível salvar a cena.");
        } else throw new Exception("Ação desconhecida.");
    }
}

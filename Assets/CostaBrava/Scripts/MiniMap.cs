using UnityEngine;
namespace CostaBrava
{
    public class MiniMap : MonoBehaviour
    {
        public float range=72;
        Camera mapCamera;
        RenderTexture texture;
        GUIStyle textStyle;
        float nextRender;
        void Start()
        {
            texture=new RenderTexture(256,256,16,RenderTextureFormat.ARGB32);texture.Create();
            mapCamera=new GameObject("Câmera do minimapa").AddComponent<Camera>();mapCamera.transform.SetParent(transform);mapCamera.transform.rotation=Quaternion.Euler(90,0,0);
            mapCamera.orthographic=true;mapCamera.orthographicSize=range;mapCamera.aspect=1;mapCamera.nearClipPlane=1;mapCamera.farClipPlane=230;mapCamera.cullingMask=(1<<0)|(1<<8)|(1<<9);mapCamera.targetTexture=texture;mapCamera.enabled=false;mapCamera.clearFlags=CameraClearFlags.SolidColor;mapCamera.backgroundColor=new Color(.08f,.2f,.25f);mapCamera.useOcclusionCulling=false;
            // Additional camera data prevents a second audio listener or post processing chain.
            mapCamera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
        }
        void LateUpdate()
        {
            if(!GameManager.Instance || !mapCamera)return;
            mapCamera.enabled=GameManager.Instance.Started;if(!mapCamera.enabled)return;
            mapCamera.depth=-20;Vector3 p=GameManager.Instance.player.Position;mapCamera.transform.position=new Vector3(p.x,130,p.z);
        }
        public Vector2 MapPoint(Vector3 point)
        {
            var p=GameManager.Instance.player.Position;return new Vector2(Mathf.Clamp((point.x-p.x)/(range*2),-.46f,.46f),Mathf.Clamp((p.z-point.z)/(range*2),-.46f,.46f));
        }
        public void Draw(Rect rect)
        {
            if(!texture)return;
            GUI.color=Color.white;GUI.DrawTexture(rect,texture,ScaleMode.StretchToFill);
            if(textStyle==null)textStyle=new GUIStyle(GUI.skin.label) {fontSize=23,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            Dot(rect,GameManager.Instance.shop.counter.position,"+",new Color(.3f,1,.7f));
            if(GameManager.Instance.weaponShop)Dot(rect,GameManager.Instance.weaponShop.counter.position,"W",new Color(1,.65f,.3f));
            if(GameManager.Instance.missions.Active>=0)Dot(rect,GameManager.Instance.missions.Target,"◆",new Color(1,.85f,.2f));
            textStyle.normal.textColor=Color.white;var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(GameManager.Instance.player.Vehicle?GameManager.Instance.player.Vehicle.transform.eulerAngles.y:GameManager.Instance.player.transform.eulerAngles.y,rect.center);GUI.Label(new Rect(rect.center.x-14,rect.center.y-17,28,34),"▲",textStyle);GUI.matrix=matrix;
            GUI.Label(new Rect(rect.x+5,rect.y+3,25,30),"N",textStyle);
        }
        void Dot(Rect rect,Vector3 point,string symbol,Color color)
        {
            var p=MapPoint(point);Vector2 screen=rect.center+Vector2.Scale(p,rect.size);
            GUI.color=new Color(.015f,.035f,.055f,.9f);GUI.DrawTexture(new Rect(screen.x-13,screen.y-14,26,28),Texture2D.whiteTexture);GUI.color=Color.white;textStyle.normal.textColor=color;GUI.Label(new Rect(screen.x-14,screen.y-17,28,34),symbol,textStyle);
        }
        void OnDestroy() {if(texture){texture.Release();Destroy(texture);}if(mapCamera)Destroy(mapCamera.gameObject);}
    }
}

using UnityEngine;
namespace CostaBrava
{
    public class WorldMarker : MonoBehaviour
    {
        public Transform icon;
        public Transform label;
        Vector3 initial;
        void Start() {if(icon)initial=icon.localPosition;}
        void Update() {if(icon) {icon.localPosition=initial+Vector3.up*Mathf.Sin(Time.time*2)*.16f;icon.Rotate(0,Time.deltaTime*40,0);}if(label && Camera.main)label.rotation=Quaternion.LookRotation(label.position-Camera.main.transform.position);}
    }
}

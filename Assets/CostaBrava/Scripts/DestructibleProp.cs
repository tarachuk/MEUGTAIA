using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CostaBrava
{
    public class DestructibleProp : MonoBehaviour
    {
        public static readonly List<DestructibleProp> All=new List<DestructibleProp>();
        public float maximumHealth=45;
        public bool Destroyed { get; private set; }
        public float Health { get; private set; }
        public Rigidbody Body { get; private set; }
        Vector3 originalScale;
        void Awake() {Health=maximumHealth;Body=GetComponent<Rigidbody>();originalScale=transform.localScale;All.Add(this);}
        public void Damage(float damage,Vector3 direction,Vector3 point)
        {
            if(Destroyed)return;
            Health-=damage;
            if(Body) {Body.isKinematic=false;Body.WakeUp();Body.AddForceAtPosition(direction*4,point,ForceMode.Impulse);}
            if(Health<=0)Break();
        }
        public void Blast(Vector3 center,float force,float radius,float damage)
        {
            if(Body) {Body.isKinematic=false;Body.WakeUp();Body.AddExplosionForce(force,center,radius,.8f,ForceMode.Impulse);}
            if(!Destroyed) {Health-=damage;if(Health<=0)Break();}
        }
        void Break()
        {
            Destroyed=true;transform.localScale=Vector3.Scale(originalScale,new Vector3(1.1f,.55f,1.1f));
            if(EffectsPool.Instance)EffectsPool.Instance.Impact(transform.position,Vector3.up,new Color(.68f,.46f,.25f),.25f);
            StartCoroutine(Retire());
        }
        IEnumerator Retire() {yield return new WaitForSeconds(18);gameObject.SetActive(false);}
        void OnDestroy() {All.Remove(this);}
    }
}

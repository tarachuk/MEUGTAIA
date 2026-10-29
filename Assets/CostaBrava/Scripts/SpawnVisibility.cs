using UnityEngine;
namespace CostaBrava
{
    public static class SpawnVisibility
    {
        public static bool IsHidden(Vector3 position,float radius=1)
        {
            var camera=Camera.main;if(!camera)return false;
            var planes=GeometryUtility.CalculateFrustumPlanes(camera);
            var bounds=new Bounds(position+Vector3.up,Vector3.one*radius*2);
            if(!GeometryUtility.TestPlanesAABB(planes,bounds))return true;
            // A visible extremity rejects the spawn; only solid world geometry can hide an actor.
            Vector3[] samples={position+Vector3.up*.25f,position+Vector3.up*1.8f,position+Vector3.up+camera.transform.right*radius,position+Vector3.up-camera.transform.right*radius};
            foreach(var sample in samples)if(!Physics.Linecast(camera.transform.position,sample,1,QueryTriggerInteraction.Ignore))return false;
            return true;
        }
    }
}

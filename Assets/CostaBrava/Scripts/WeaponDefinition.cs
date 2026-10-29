using UnityEngine;

namespace CostaBrava
{
    [CreateAssetMenu(menuName="Costa Brava/Arma")]
    public class WeaponDefinition : ScriptableObject
    {
        public string displayName;
        public int magazineSize=12, startingReserve=60;
        public float damage=25, fireInterval=.28f, reloadTime=1.3f, range=100, recoil=1.3f;
        public bool automatic;
        public bool scoped;
        public float scopeFov=18;
    }
}

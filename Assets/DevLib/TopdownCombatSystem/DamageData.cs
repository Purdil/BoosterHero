using UnityEngine;

namespace DevLib.TopdownCombatSystem
{
    public struct DamageData
    {
        public float DamageAmount;
        public bool IsCritical;
        public GameObject Dealer;
        public Vector2 DirectedKBForce; //방향성을 부여받은 넉백힘
    }
}
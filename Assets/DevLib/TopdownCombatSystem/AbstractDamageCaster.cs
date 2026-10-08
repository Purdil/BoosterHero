using UnityEngine;

namespace DevLib.TopdownCombatSystem
{
    public abstract class AbstractDamageCaster : MonoBehaviour
    {
        public GameObject Owner { get; private set; }

        [SerializeField] protected int maxHitCount = 5; //최대로 때릴 수 있는 적의 수
        [SerializeField] protected ContactFilter2D contactFilter;

        protected Collider2D[] _hitResults;
        
        public virtual void InitCaster(GameObject owner)
        {
            Owner = owner;
            _hitResults = new Collider2D[maxHitCount];
        }

        public abstract bool CastDamage(float damage, Vector2 direction, float kbForce);
    }
}
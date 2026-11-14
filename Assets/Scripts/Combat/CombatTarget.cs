using UnityEngine;

namespace SpaceRunner.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatTarget : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private Collider hurtbox;
        [SerializeField] private Transform aimPoint;
        public HealthComponent Health => health;
        public Vector3 AimPosition => aimPoint != null ? aimPoint.position : hurtbox.bounds.center;
        public void Initialize(HealthComponent targetHealth, Collider targetCollider)
        {
            health = targetHealth; hurtbox = targetCollider;
        }
        public bool Owns(Collider other) => other == hurtbox || other.GetComponentInParent<CombatTarget>() == this;
    }
}

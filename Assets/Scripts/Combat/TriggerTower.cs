using UnityEngine;

namespace SpaceRunner.Combat
{
    public sealed class TriggerTower : EnemyActor
    {
        [SerializeField, Min(0.1f)] private float attackRange = 10;
        [SerializeField, Min(0.05f)] private float shotInterval = 1.5f;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform aimPivot;
        [SerializeField] private TowerProjectile projectilePrefab;
        private float cooldown;
        public float AttackRange => attackRange;
        public bool IsTargetInRange => Target != null && (Target.AimPosition - transform.position).sqrMagnitude <= attackRange * attackRange;

        private void Update()
        {
            if (!CanAct) return;
            cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
            if (!IsTargetInRange || cooldown > 0) return;
            Fire();
            cooldown = shotInterval;
        }
        public TowerProjectile Fire()
        {
            if (!CanAct || !IsTargetInRange) return null;
            if (aimPivot != null) aimPivot.rotation = Quaternion.LookRotation(Target.AimPosition - aimPivot.position);
            var shot = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity, Projectiles);
            shot.Launch(Session, Target, this, Target.AimPosition - muzzle.position);
            return shot;
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0.45f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}

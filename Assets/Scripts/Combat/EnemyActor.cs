using UnityEngine;

namespace SpaceRunner.Combat
{
    public abstract class EnemyActor : MonoBehaviour
    {
        protected GameSession Session { get; private set; }
        protected CombatTarget Target { get; private set; }
        protected Transform Projectiles { get; private set; }
        protected bool CanAct => Session != null && Session.CanControlPlayer && Target != null && !Target.Health.IsDead;
        public virtual void Initialize(GameSession session, CombatTarget target, Transform projectiles)
        {
            Session = session; Target = target; Projectiles = projectiles;
        }
    }
}

using UnityEngine;
using UnityEngine.AI;

namespace SpaceRunner.Combat
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class MeleeEnemy : EnemyActor
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 1.8f;
        [SerializeField, Min(0.05f)] private float repathInterval = 0.15f;
        [SerializeField, Min(0.1f)] private float targetProjectionDistance = 5;
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private LayerMask sightLayers = ~((1 << 2) | (1 << 5) | (1 << 30));
        private NavMeshAgent agent;
        private float repathIn;
        private void Awake() { agent = GetComponent<NavMeshAgent>(); agent.speed = moveSpeed; }
        public override void Initialize(GameSession session, CombatTarget target, Transform projectiles)
        {
            base.Initialize(session, target, projectiles);
            // Scene layout instances keep navigation disabled until the level's NavMesh exists.
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.enabled = true;
        }
        private void Update()
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            agent.isStopped = !CanAct;
            if (!CanAct) return;
            repathIn -= Time.deltaTime;
            if (repathIn > 0) return;
            repathIn = repathInterval;
            if (NavMesh.SamplePosition(Target.AimPosition, out var hit, targetProjectionDistance, agent.areaMask))
                agent.SetDestination(hit.position);
            else agent.ResetPath();
        }
        private void OnTriggerEnter(Collider other) => TryContact(other);
        private void OnTriggerStay(Collider other) => TryContact(other);
        private void TryContact(Collider other)
        {
            if (!CanAct || !Target.Owns(other)) return;
            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position + Vector3.up;
            Vector3 delta = Target.AimPosition - origin;
            // The contact volume must not kill a player standing on the other side of a thin wall.
            foreach (var hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude, sightLayers, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && !Target.Owns(hit.collider)) return;
            Target.Health.Kill();
        }
    }
}

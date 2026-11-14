using SpaceRunner.Core;
using UnityEngine;

namespace SpaceRunner.Combat
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class TowerProjectile : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 25;
        [SerializeField, Min(0.1f)] private float speed = 12;
        [SerializeField, Min(0)] private float secondsAfterRangeExit = 10;
        [SerializeField] private LayerMask impactLayers = ~((1 << 2) | (1 << 5) | (1 << 30));
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private readonly Collider[] overlaps = new Collider[32];
        private GameSession session;
        private CombatTarget target;
        private TriggerTower owner;
        private DepartureLifetime expiry;
        private Vector3 direction;
        private float radius;
        private bool spent;
        public Vector3 Direction => direction;
        public bool ExpiryStarted => expiry != null && expiry.HasStarted;
        public float SecondsRemaining => expiry?.Remaining ?? secondsAfterRangeExit;

        public void Launch(GameSession gameSession, CombatTarget combatTarget, TriggerTower tower, Vector3 aimDirection)
        {
            session = gameSession; target = combatTarget; owner = tower;
            direction = aimDirection.normalized;
            if (direction == Vector3.zero) direction = Vector3.forward;
            transform.rotation = Quaternion.LookRotation(direction);
            var sphere = GetComponent<SphereCollider>();
            radius = sphere.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
            expiry = new DepartureLifetime(secondsAfterRangeExit);
        }
        private void Update()
        {
            if (spent || session == null || !session.CanControlPlayer) return;
            if (owner == null || !owner.IsTargetInRange) expiry.Begin();
            expiry.Tick(Time.deltaTime);
            if (expiry.HasExpired) Despawn();
        }
        private void FixedUpdate()
        {
            if (spent || session == null || !session.CanControlPlayer) return;
            // Sweep each step, including initial overlaps, so fast bullets cannot tunnel through walls.
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, overlaps, impactLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (CanHit(overlaps[i])) { Impact(overlaps[i]); return; }
            float distance = speed * Time.fixedDeltaTime;
            count = Physics.SphereCastNonAlloc(transform.position, radius, direction, hits, distance, impactLayers, QueryTriggerInteraction.Ignore);
            int nearest = -1;
            for (int i = 0; i < count; i++)
                if (CanHit(hits[i].collider) && (nearest < 0 || hits[i].distance < hits[nearest].distance)) nearest = i;
            if (nearest >= 0) { Impact(hits[nearest].collider); return; }
            GetComponent<Rigidbody>().MovePosition(transform.position + direction * distance);
        }
        private bool CanHit(Collider other) => other != null && other.transform != transform &&
            !other.transform.IsChildOf(transform) && (owner == null || !other.transform.IsChildOf(owner.transform));
        private void OnTriggerEnter(Collider other)
        {
            if (spent || session == null || !session.CanControlPlayer || other.isTrigger ||
                (impactLayers.value & (1 << other.gameObject.layer)) == 0 || !CanHit(other)) return;
            Impact(other);
        }
        private void Impact(Collider other)
        {
            if (spent) return;
            spent = true;
            if (target != null && target.Owns(other)) target.Health.TakeDamage(damage);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        private void Despawn() { spent = true; gameObject.SetActive(false); Destroy(gameObject); }
    }
}

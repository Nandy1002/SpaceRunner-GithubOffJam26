using System;
using UnityEngine;

namespace SpaceRunner.Combat
{
    [Serializable]
    public struct EnemySpawn
    {
        public EnemyActor prefab;
        public Transform point;
    }

    public sealed class CombatSystem : MonoBehaviour
    {
        [SerializeField, Min(1)] private int playerMaximumHealth = 100;
        [SerializeField] private HealthHudView hudPrefab;
        [SerializeField] private GroundNavigationRegion navigationRegion;
        [SerializeField] private EnemySpawn[] spawns = Array.Empty<EnemySpawn>();
        [SerializeField, Tooltip("Optional scene layout. Place or duplicate enemy prefab instances below this transform to set their spawn positions.")]
        private Transform sceneEnemyLayout;
        private EnemyActor[] sceneTemplates = Array.Empty<EnemyActor>();
        private GameSession session;
        private Transform encounter;
        public HealthComponent PlayerHealth { get; private set; }
        public CombatTarget PlayerTarget { get; private set; }
        public Transform Encounter => encounter;
        public void Initialize(GameSession gameSession, Transform player, Collider hurtbox)
        {
            if (session != null) return;
            session = gameSession;
            if (sceneEnemyLayout != null)
            {
                sceneTemplates = sceneEnemyLayout.GetComponentsInChildren<EnemyActor>();
                foreach (var template in sceneTemplates) template.gameObject.SetActive(false);
            }
            PlayerHealth = player.GetComponent<HealthComponent>() ?? player.gameObject.AddComponent<HealthComponent>();
            PlayerHealth.Initialize(playerMaximumHealth);
            PlayerHealth.DamageAllowed = () => session != null && session.CanControlPlayer;
            PlayerHealth.Died += OnPlayerDied;
            PlayerTarget = player.GetComponent<CombatTarget>() ?? player.gameObject.AddComponent<CombatTarget>();
            PlayerTarget.Initialize(PlayerHealth, hurtbox);
            if (hudPrefab != null) Instantiate(hudPrefab, transform).Initialize(session, PlayerHealth);
            if (navigationRegion != null) navigationRegion.Build();
            session.RunChanged += OnRunChanged;
            if (session.IsPlaying) OnRunChanged(true);
        }
        private void OnPlayerDied() => session.EndRun(RunEndReason.PlayerDied);
        private void OnRunChanged(bool playing)
        {
            ClearEncounter();
            if (!playing) return;
            PlayerHealth.ResetHealth();
            encounter = new GameObject("Current Run Enemies and Projectiles").transform;
            encounter.SetParent(transform, false);
            foreach (var spawn in spawns)
            {
                if (spawn.prefab == null || spawn.point == null) continue;
                Spawn(spawn.prefab, spawn.point);
            }
            foreach (var template in sceneTemplates)
                if (template != null) Spawn(template, template.transform);
        }
        private void Spawn(EnemyActor template, Transform point)
        {
            var enemy = Instantiate(template, point.position, point.rotation, encounter);
            enemy.name = template.name;
            enemy.Initialize(session, PlayerTarget, encounter);
            enemy.gameObject.SetActive(true);
        }
        private void ClearEncounter()
        {
            if (encounter == null) return;
            encounter.gameObject.SetActive(false);
            Destroy(encounter.gameObject);
            encounter = null;
        }
        private void OnDestroy()
        {
            if (session != null) session.RunChanged -= OnRunChanged;
            if (PlayerHealth != null)
            {
                PlayerHealth.Died -= OnPlayerDied;
                PlayerHealth.DamageAllowed = null;
            }
            ClearEncounter();
        }
    }
}

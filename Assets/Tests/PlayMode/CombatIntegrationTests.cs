using System.Collections;
using System.Linq;
using NUnit.Framework;
using SpaceRunner.Combat;
using SpaceRunner.Core;
using SpaceRunner.Demo;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SpaceRunner.Tests
{
    public sealed class CombatIntegrationTests
    {
        private WatchPrototypeBootstrap prototype;
        private TriggerTower tower;
        private MeleeEnemy melee;
        private Rigidbody playerBody;
        private GameObject testWall;
        private int initialEnemyCount;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("WatchPrototype");
            yield return null;
            prototype = Object.FindFirstObjectByType<WatchPrototypeBootstrap>();
            prototype.Session.StartRun();
            initialEnemyCount = prototype.Combat.Encounter.GetComponentsInChildren<EnemyActor>().Length;
            tower = prototype.Combat.Encounter.GetComponentInChildren<TriggerTower>();
            melee = prototype.Combat.Encounter.GetComponentInChildren<MeleeEnemy>();
            tower.enabled = false;
            melee.gameObject.SetActive(false);
            // Keep the target still without pausing the simulation under test.
            prototype.Player.enabled = false;
            playerBody = prototype.Player.GetComponent<Rigidbody>();
            playerBody.isKinematic = true;
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (prototype != null) prototype.Session.ReturnToMenu();
            if (testWall != null) Object.Destroy(testWall);
            yield return SceneManager.LoadSceneAsync("SampleScene");
            Time.timeScale = 1;
            yield return null;
        }
        private void MovePlayer(Vector3 position)
        {
            playerBody.position = position;
            prototype.Player.transform.position = position;
            Physics.SyncTransforms();
        }
        private void MoveTower(Vector3 position) { tower.transform.position = position; Physics.SyncTransforms(); }
        private TowerProjectile AirShot()
        {
            MovePlayer(tower.transform.position + new Vector3(0, 2, -5));
            var shot = tower.Fire();
            Assert.That(shot, Is.Not.Null);
            shot.transform.position = new Vector3(0, 40, 0);
            shot.GetComponent<Rigidbody>().position = shot.transform.position;
            shot.Launch(prototype.Session, prototype.Combat.PlayerTarget, tower, Vector3.up);
            Physics.SyncTransforms();
            return shot;
        }

        [UnityTest]
        public IEnumerator SceneLayoutControlsSpawnsAndGeometrySurvivesRestart()
        {
            var template = prototype.transform.Find("Enemy Layout - Move or Duplicate These/Tower 01");
            var wall = prototype.transform.Find("Octagonal Tunnel/Interior Walls - Move These/Wall 01");
            Assert.That(template, Is.Not.Null);
            Assert.That(wall, Is.Not.Null);
            Assert.That(prototype.transform.Find("Octagonal Tunnel/Eight Inner Surfaces").childCount, Is.EqualTo(8));
            var desiredSpawn = new Vector3(-3, 0, 11);
            template.position = desiredSpawn;
            wall.position += Vector3.right;
            var desiredWall = wall.position;
            prototype.Session.ReturnToMenu();
            playerBody.isKinematic = false;
            prototype.Session.StartRun();
            var spawned = prototype.Combat.Encounter.GetComponentsInChildren<TriggerTower>().First(enemy => enemy.name == "Tower 01");
            Assert.That(spawned.transform.position, Is.EqualTo(desiredSpawn));
            Assert.That(wall.position, Is.EqualTo(desiredWall));
            Assert.That(spawned.transform, Is.Not.SameAs(template));
            yield return null;
        }

        [UnityTest]
        public IEnumerator HealthHudDeathAndNextRunResetTogether()
        {
            var health = prototype.Combat.PlayerHealth;
            health.TakeDamage(25);
            Assert.That(health.Current, Is.EqualTo(75));
            var hud = Object.FindFirstObjectByType<HealthHudView>();
            Assert.That(hud.GetComponentInChildren<Text>().text, Is.EqualTo("HEALTH  75 / 100"));
            prototype.Watch.PauseGameplayWhileOpen = false;
            prototype.Watch.Toggle();
            yield return new WaitForSecondsRealtime(0.5f);
            health.Kill();
            Assert.That(prototype.Session.IsPlaying, Is.False);
            Assert.That(prototype.Session.LastEndReason, Is.EqualTo(RunEndReason.PlayerDied));
            Assert.That(prototype.Watch.State.Phase, Is.EqualTo(WatchPhase.Closed));
            Assert.That(prototype.Combat.Encounter, Is.Null);
            Assert.That(Object.FindFirstObjectByType<MainMenuView>().GetComponentsInChildren<Text>().Any(text => text.text == "YOU DIED"), Is.True);
            yield return null;
            playerBody.isKinematic = false;
            prototype.Session.StartRun();
            Assert.That(health.Current, Is.EqualTo(100));
            Assert.That(prototype.Combat.Encounter.GetComponentsInChildren<EnemyActor>().Length, Is.EqualTo(initialEnemyCount));
            Assert.That(prototype.Session.Timer.ElapsedSeconds, Is.Zero);
        }

        [UnityTest]
        public IEnumerator TowerFiresOnlyInRangeAndProjectilesNeverHome()
        {
            MovePlayer(new Vector3(-4, 2, -6));
            tower.enabled = true;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(prototype.Combat.Encounter.GetComponentsInChildren<TowerProjectile>(), Is.Empty);
            MovePlayer(new Vector3(-4, 2, 1));
            yield return new WaitForSecondsRealtime(0.05f);
            tower.enabled = false;
            var shot = prototype.Combat.Encounter.GetComponentInChildren<TowerProjectile>();
            Assert.That(shot, Is.Not.Null);
            Vector3 direction = shot.Direction, origin = shot.transform.position;
            MovePlayer(new Vector3(4, 2, 1));
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(shot.Direction, Is.EqualTo(direction));
            Assert.That(Vector3.Angle(shot.transform.position - origin, direction), Is.LessThan(0.1f));
            MovePlayer(new Vector3(0, 2, -6));
            tower.enabled = true;
            yield return new WaitForSecondsRealtime(1.7f);
            Assert.That(prototype.Combat.Encounter.GetComponentsInChildren<TowerProjectile>().All(existing => existing == shot), Is.True,
                "Only the original missed bullet may remain; the out-of-range tower must not fire another.");
            Assert.That(tower != null, Is.True);
        }

        [UnityTest]
        public IEnumerator RangeExitStartsTenSecondsPauseFreezesAndReentryDoesNotRenew()
        {
            var shot = AirShot();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(shot.ExpiryStarted, Is.False);
            MovePlayer(new Vector3(0, 2, -6));
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(shot.ExpiryStarted, Is.True);
            Assert.That(shot.SecondsRemaining, Is.InRange(9.8f, 10f));
            prototype.Watch.Toggle();
            float remaining = shot.SecondsRemaining;
            Vector3 position = shot.transform.position;
            prototype.Combat.PlayerHealth.TakeDamage(25);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(shot.SecondsRemaining, Is.EqualTo(remaining));
            Assert.That(shot.transform.position, Is.EqualTo(position));
            Assert.That(prototype.Combat.PlayerHealth.Current, Is.EqualTo(100));
            prototype.Watch.PauseGameplayWhileOpen = false;
            MovePlayer(tower.transform.position + new Vector3(0, 2, -5));
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(shot.SecondsRemaining, Is.LessThan(remaining));
            Assert.That(shot.transform.position.y, Is.GreaterThan(position.y));
            Time.timeScale = 10;
            float deadline = Time.realtimeSinceStartup + 4;
            while (shot != null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(shot == null, Is.True);
            Assert.That(tower != null, Is.True);
        }

        [UnityTest]
        public IEnumerator BulletDamagesOnceAndSolidWallBlocksIt()
        {
            MoveTower(new Vector3(0, 0, 5));
            MovePlayer(new Vector3(0, 2, 0));
            var shot = tower.Fire();
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(shot == null, Is.True);
            Assert.That(prototype.Combat.PlayerHealth.Current, Is.EqualTo(75));
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(prototype.Combat.PlayerHealth.Current, Is.EqualTo(75));
            prototype.Combat.PlayerHealth.ResetHealth();
            testWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testWall.transform.position = new Vector3(0, 1.8f, 2.5f);
            testWall.transform.localScale = new Vector3(3, 3, 0.05f);
            testWall.layer = 7;
            Physics.SyncTransforms();
            shot = tower.Fire();
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(shot == null, Is.True);
            Assert.That(prototype.Combat.PlayerHealth.Current, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator MeleeUsesNavigationAndStopsWhenWatchPauses()
        {
            MovePlayer(new Vector3(0, 2, 6));
            melee.gameObject.SetActive(true);
            var agent = melee.GetComponent<NavMeshAgent>();
            Assert.That(agent.Warp(new Vector3(0, 0, 12)), Is.True);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(agent.path.corners.Length, Is.GreaterThanOrEqualTo(3), "The beacon pedestal must cause a route around it.");
            Vector3 position = melee.transform.position;
            prototype.Watch.Toggle();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Vector3.Distance(position, melee.transform.position), Is.LessThan(0.01f));
            prototype.Watch.PauseGameplayWhileOpen = false;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(Vector3.Distance(position, melee.transform.position), Is.InRange(0.3f, 1.1f));
            Assert.That(prototype.Session.IsPlaying, Is.True);
        }

        [UnityTest]
        public IEnumerator MeleeContactKillsAtFullHealthButCannotReachThroughWall()
        {
            MovePlayer(new Vector3(2, 2, 0));
            melee.gameObject.SetActive(true);
            var agent = melee.GetComponent<NavMeshAgent>();
            Assert.That(agent.Warp(new Vector3(2.95f, 0, 0)), Is.True);
            agent.enabled = false;
            testWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testWall.transform.position = new Vector3(2.5f, 1.5f, 0);
            testWall.transform.localScale = new Vector3(0.05f, 3, 3);
            Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(prototype.Session.IsPlaying, Is.True);
            testWall.SetActive(false);
            Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(prototype.Combat.PlayerHealth.IsDead, Is.True);
            Assert.That(prototype.Session.LastEndReason, Is.EqualTo(RunEndReason.PlayerDied));
        }
    }
}

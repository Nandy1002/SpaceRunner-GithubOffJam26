using NUnit.Framework;
using SpaceRunner.Core;

namespace SpaceRunner.Tests
{
    public sealed class CombatStateTests
    {
        [Test]
        public void DamageClampsAndDeathOccursOnceUntilReset()
        {
            var health = new HealthState(100);
            int deaths = 0; health.Died += () => deaths++;
            health.Damage(-10); health.Damage(0);
            Assert.That(health.Current, Is.EqualTo(100));
            health.Damage(25); Assert.That(health.Current, Is.EqualTo(75));
            health.Damage(int.MaxValue); health.Kill(); health.Damage(1);
            Assert.That(health.Current, Is.Zero); Assert.That(deaths, Is.EqualTo(1));
            health.Reset(); Assert.That(health.IsDead, Is.False);
            health.Kill(); Assert.That(deaths, Is.EqualTo(2));
        }
        [Test]
        public void InstantKillIgnoresHowMuchHealthRemains()
        {
            var health = new HealthState(10000);
            health.Kill(); Assert.That(health.IsDead, Is.True);
        }
        [Test]
        public void LifetimeBeginsAtDepartureAndCannotBeRenewed()
        {
            var lifetime = new DepartureLifetime(10);
            lifetime.Tick(60); Assert.That(lifetime.Remaining, Is.EqualTo(10));
            lifetime.Begin(); lifetime.Tick(6);
            lifetime.Begin(); Assert.That(lifetime.Remaining, Is.EqualTo(4));
            lifetime.Tick(0); Assert.That(lifetime.HasExpired, Is.False);
            lifetime.Tick(4); Assert.That(lifetime.HasExpired, Is.True);
        }
    }
}

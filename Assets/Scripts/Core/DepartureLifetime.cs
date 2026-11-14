using System;

namespace SpaceRunner.Core
{
    // A one-way countdown: re-entering the firing radius never renews an old bullet.
    public sealed class DepartureLifetime
    {
        public bool HasStarted { get; private set; }
        public float Remaining { get; private set; }
        public bool HasExpired => HasStarted && Remaining <= 0;
        public DepartureLifetime(float seconds) => Remaining = Math.Max(0, seconds);
        public void Begin() => HasStarted = true;
        public void Tick(float scaledSeconds)
        {
            if (HasStarted && scaledSeconds > 0) Remaining = Math.Max(0, Remaining - scaledSeconds);
        }
    }
}

using System;
using System.Collections.Generic;

namespace SpaceRunner.Core
{
    /// <summary>Each feature releases only the pause it owns.</summary>
    public sealed class PauseState
    {
        private readonly HashSet<Lease> leases = new();
        public bool IsPaused => leases.Count > 0;
        public event Action<bool> Changed;

        public IDisposable Acquire()
        {
            var lease = new Lease(this);
            bool wasPaused = IsPaused;
            leases.Add(lease);
            if (!wasPaused) Changed?.Invoke(true);
            return lease;
        }

        private void Release(Lease lease)
        {
            if (leases.Remove(lease) && !IsPaused) Changed?.Invoke(false);
        }

        private sealed class Lease : IDisposable
        {
            private PauseState owner;
            public Lease(PauseState owner) => this.owner = owner;
            public void Dispose()
            {
                var previous = owner;
                owner = null;
                previous?.Release(this);
            }
        }
    }
}

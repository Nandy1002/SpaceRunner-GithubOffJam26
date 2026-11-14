using System;

namespace SpaceRunner.Core
{
    public sealed class HealthState
    {
        public int Maximum { get; }
        public int Current { get; private set; }
        public bool IsDead => Current == 0;
        public event Action Changed;
        public event Action Died;

        public HealthState(int maximum)
        {
            if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
            Maximum = Current = maximum;
        }

        public void Damage(int amount)
        {
            if (amount <= 0 || IsDead) return;
            Current = amount >= Current ? 0 : Current - amount;
            Changed?.Invoke();
            if (IsDead) Died?.Invoke();
        }

        public void Kill() => Damage(Current);
        public void Reset() { Current = Maximum; Changed?.Invoke(); }
    }
}

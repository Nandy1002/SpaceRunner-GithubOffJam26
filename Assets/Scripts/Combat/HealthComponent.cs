using System;
using SpaceRunner.Core;
using UnityEngine;

namespace SpaceRunner.Combat
{
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        private HealthState state;
        public int Maximum => State.Maximum;
        public int Current => State.Current;
        public bool IsDead => State.IsDead;
        public Func<bool> DamageAllowed { get; set; }
        public event Action Changed;
        public event Action Died;
        private HealthState State
        {
            get { if (state == null) Initialize(maximumHealth); return state; }
        }
        public void Initialize(int maximum)
        {
            maximumHealth = Mathf.Max(1, maximum);
            state = new HealthState(maximumHealth);
            state.Changed += () => Changed?.Invoke();
            state.Died += () => Died?.Invoke();
            Changed?.Invoke();
        }
        public void TakeDamage(int amount)
        {
            if (DamageAllowed == null || DamageAllowed()) State.Damage(amount);
        }
        public void Kill()
        {
            if (DamageAllowed == null || DamageAllowed()) State.Kill();
        }
        public void ResetHealth() => State.Reset();
    }
}

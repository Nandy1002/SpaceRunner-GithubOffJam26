using System;
using UnityEngine;

namespace SpaceRunner
{
    public abstract class WatchPresentation : MonoBehaviour
    {
        public event Action<bool> Settled;
        public abstract void SetRaised(bool raised);
        public abstract void SnapClosed();
        protected void NotifySettled(bool raised) => Settled?.Invoke(raised);
    }
}

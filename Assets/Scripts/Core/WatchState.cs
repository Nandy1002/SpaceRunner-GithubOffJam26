using System;

namespace SpaceRunner.Core
{
    public enum WatchPhase { Closed, Opening, Open, Closing }

    /// <summary>Independent of models, input bindings, UI toolkit and scene loading.</summary>
    public sealed class WatchState : IDisposable
    {
        private readonly PauseState pause;
        private IDisposable pauseLease;
        public WatchPhase Phase { get; private set; }
        public bool PausesGameplay { get; private set; }
        public bool WantsOpen => Phase == WatchPhase.Opening || Phase == WatchPhase.Open;
        public event Action<WatchPhase> Changed;

        public WatchState(PauseState pause, bool pausesGameplay = true)
        {
            this.pause = pause;
            PausesGameplay = pausesGameplay;
        }

        public void SetPausesGameplay(bool enabled)
        {
            PausesGameplay = enabled;
            if (enabled && Phase != WatchPhase.Closed) pauseLease ??= pause.Acquire();
            else if (!enabled)
            {
                pauseLease?.Dispose();
                pauseLease = null;
            }
        }
        public void Toggle() => SetOpen(!WantsOpen);

        public void SetOpen(bool open)
        {
            if (open == WantsOpen) return;
            if (open && PausesGameplay) pauseLease ??= pause.Acquire();
            SetPhase(open ? WatchPhase.Opening : WatchPhase.Closing);
        }

        // Ignore a stale animation completion after the player reverses direction.
        public void CompletePresentation(bool raised)
        {
            if (raised && Phase == WatchPhase.Opening) SetPhase(WatchPhase.Open);
            else if (!raised && Phase == WatchPhase.Closing) Reset();
        }

        public void Reset()
        {
            SetPhase(WatchPhase.Closed);
            pauseLease?.Dispose();
            pauseLease = null;
        }

        private void SetPhase(WatchPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            Changed?.Invoke(phase);
        }

        public void Dispose() => Reset();
    }
}

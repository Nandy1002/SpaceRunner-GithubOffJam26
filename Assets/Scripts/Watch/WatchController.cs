using System;
using SpaceRunner.Core;
using UnityEngine;

namespace SpaceRunner
{
    public sealed class WatchController : MonoBehaviour
    {
        [SerializeField, Tooltip("On: pause world, timer and player controls. Off: all continue while the pointer can use the watch.")]
        private bool pauseGameplayWhileOpen = true;
        private IDisposable pointerLease;
        public bool PauseGameplayWhileOpen
        {
            get => pauseGameplayWhileOpen;
            set { pauseGameplayWhileOpen = value; State?.SetPausesGameplay(value); }
        }
        private GameSession session;
        private PlayerInputReader input;
        private WatchPresentation presentation;
        private WatchMenuView menu;
        public WatchState State { get; private set; }

        public void Initialize(GameSession gameSession, PlayerInputReader reader, WatchPresentation arm, WatchMenuView view)
        {
            session = gameSession; input = reader; presentation = arm; menu = view;
            State = new WatchState(session.Pause, pauseGameplayWhileOpen);
            State.Changed += ApplyPhase;
            presentation.Settled += State.CompletePresentation;
            input.WatchPressed += Toggle;
            input.BackPressed += Back;
            menu.ResumeRequested += Close;
            session.RunChanged += OnRunChanged;
            ApplyPhase(WatchPhase.Closed);
        }

        public void Toggle()
        {
            if (session != null && session.IsPlaying && isActiveAndEnabled) State.Toggle();
        }

        public void Close() => State?.SetOpen(false);

        private void Back()
        {
            if (session == null || !session.IsPlaying || !isActiveAndEnabled) return;
            if (State.Phase == WatchPhase.Open && menu.TryGoBack()) return;
            Toggle();
        }

        private void ApplyPhase(WatchPhase phase)
        {
            if (phase == WatchPhase.Closed)
            {
                pointerLease?.Dispose();
                pointerLease = null;
            }
            else pointerLease ??= session.PointerRequests.Acquire();
            if (menu != null) menu.SetVisible(phase == WatchPhase.Open);
            if (presentation == null) return;
            if (phase == WatchPhase.Closed) presentation.SnapClosed();
            else if (phase == WatchPhase.Opening) presentation.SetRaised(true);
            else if (phase == WatchPhase.Closing) presentation.SetRaised(false);
        }

        private void OnValidate() => State?.SetPausesGameplay(pauseGameplayWhileOpen);

        private void OnRunChanged(bool playing) { if (!playing) State.Reset(); }

        private void OnDisable() => State?.Reset();

        private void OnDestroy()
        {
            if (State == null) return;
            input.WatchPressed -= Toggle;
            input.BackPressed -= Back;
            presentation.Settled -= State.CompletePresentation;
            if (menu != null) menu.ResumeRequested -= Close;
            if (session != null) session.RunChanged -= OnRunChanged;
            State.Changed -= ApplyPhase;
            State.Dispose();
            pointerLease?.Dispose();
        }
    }
}

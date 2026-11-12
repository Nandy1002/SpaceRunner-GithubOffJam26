using System;
using SpaceRunner.Core;
using UnityEngine;

namespace SpaceRunner
{
    public enum RunEndReason { None, ReturnedToMenu, PlayerDied }

    [DefaultExecutionOrder(-300)]
    public sealed class GameSession : MonoBehaviour
    {
        public PauseState Pause { get; } = new();
        public PauseState PointerRequests { get; } = new();
        public RunTimer Timer { get; private set; }
        public bool IsPlaying => Timer != null && Timer.IsRunning;
        public bool CanControlPlayer => IsPlaying && !Pause.IsPaused;
        public RunEndReason LastEndReason { get; private set; }
        public event Action<bool> RunChanged;

        private IDisposable menuPause;
        private float previousTimeScale;
        private bool previousAudioPause;
        private bool ownsWorldPause;
        private CursorLockMode initialCursorLock;
        private bool initialCursorVisible;

        private void Awake()
        {
            Timer = new RunTimer(Pause);
            initialCursorLock = Cursor.lockState;
            initialCursorVisible = Cursor.visible;
            Pause.Changed += ApplyPause;
            PointerRequests.Changed += OnPointerChanged;
            menuPause = Pause.Acquire();
        }

        private void Update() => Timer.Tick(Time.unscaledDeltaTime);

        public void StartRun()
        {
            if (IsPlaying) return;
            LastEndReason = RunEndReason.None;
            Timer.StartRun();
            // Reset the player before releasing the menu's pause.
            RunChanged?.Invoke(true);
            menuPause?.Dispose();
            menuPause = null;
            RefreshCursor();
        }

        public void ReturnToMenu()
        {
            EndRun(RunEndReason.ReturnedToMenu);
        }

        public void EndRun(RunEndReason reason)
        {
            if (!IsPlaying) return;
            LastEndReason = reason;
            menuPause ??= Pause.Acquire();
            Timer.EndRun();
            RunChanged?.Invoke(false);
            RefreshCursor();
        }

        private void ApplyPause(bool paused)
        {
            if (paused)
            {
                previousTimeScale = Time.timeScale;
                previousAudioPause = AudioListener.pause;
                ownsWorldPause = true;
                Time.timeScale = 0;
                AudioListener.pause = true;
            }
            else RestoreWorld();
            RefreshCursor();
        }

        private void RestoreWorld()
        {
            if (!ownsWorldPause) return;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            ownsWorldPause = false;
        }

        private void RefreshCursor()
        {
            bool capture = CanControlPlayer && !PointerRequests.IsPaused;
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }

        private void OnPointerChanged(bool requested) => RefreshCursor();

        private void OnApplicationFocus(bool focused)
        {
            if (focused) RefreshCursor();
        }

        public void QuitApplication()
        {
            ReturnToMenu();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            Pause.Changed -= ApplyPause;
            PointerRequests.Changed -= OnPointerChanged;
            RestoreWorld();
            Cursor.lockState = initialCursorLock;
            Cursor.visible = initialCursorVisible;
        }
    }
}

using System;
using SpaceRunner.Core;
using UnityEngine;

namespace SpaceRunner
{
    [DefaultExecutionOrder(-150)]
    public sealed class NandyPlayerBridge : MonoBehaviour
    {
        private GameSession session;
        private WatchController watch;
        private FirstPersonController player;
        private PlayerSettings settings;
        private InputManager input;
        private IDisposable gravityPointer;
        private Vector3 spawnPosition, initialGravity;
        private Quaternion spawnRotation;
        public void Initialize(GameSession gameSession, WatchController watchController, FirstPersonController playerController, PlayerSettings playerSettings)
        {
            session = gameSession; watch = watchController; player = playerController; settings = playerSettings;
            spawnPosition = player.transform.position; spawnRotation = player.transform.rotation; initialGravity = Physics.gravity;
            input = InputManager.Instance;
            input.CursorExternallyManaged = true;
            session.Pause.Changed += RefreshPause;
            session.PointerRequests.Changed += PointerChanged;
            session.RunChanged += RefreshRun;
            watch.State.Changed += RefreshWatch;
            Refresh();
        }
        private void RefreshPause(bool _) => Refresh();
        private void RefreshRun(bool playing)
        {
            if (playing)
            {
                foreach (var ability in player.GetComponents<MonoBehaviour>())
                {
                    if (ability is IAbility && ability.enabled) { ability.enabled = false; ability.enabled = true; }
                }
                Physics.gravity = initialGravity;
                player.ResetForRun(spawnPosition, spawnRotation);
            }
            Refresh();
        }
        private void RefreshWatch(WatchPhase _) => Refresh();
        private void PointerChanged(bool _) { if (input != null) input.SuppressLookForCursorChange(); }
        private void Refresh()
        {
            input.GameplayInputBlocked = !session.CanControlPlayer;
            input.GravitySelectionBlocked = watch.State.Phase != WatchPhase.Closed;
            input.SuppressLookForCursorChange();
            if (input.GameplayInputBlocked || input.GravitySelectionBlocked) ReleaseGravityPointer();
        }
        private void Update()
        {
            if (input == null) return;
            player.mouseSensitivity = settings.LookSensitivity;
            if (input.TabHeld) gravityPointer ??= session.PointerRequests.Acquire();
            else ReleaseGravityPointer();
        }
        private void ReleaseGravityPointer() { var lease = gravityPointer; gravityPointer = null; lease?.Dispose(); }
        private void OnDestroy()
        {
            if (session != null)
            {
                session.Pause.Changed -= RefreshPause; session.PointerRequests.Changed -= PointerChanged; session.RunChanged -= RefreshRun;
            }
            if (watch?.State != null) watch.State.Changed -= RefreshWatch;
            ReleaseGravityPointer();
            Physics.gravity = initialGravity;
            if (input != null) { input.GameplayInputBlocked = input.GravitySelectionBlocked = input.CursorExternallyManaged = false; }
        }
    }
}

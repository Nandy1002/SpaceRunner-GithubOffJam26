using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceRunner
{
    // Menu actions only. Movement stays in the team's InputManager.
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private string watchBinding = "<Keyboard>/r";
        public event Action WatchPressed;
        public event Action BackPressed;
        private InputAction watch, back;
        private void Awake()
        {
            watch = new InputAction("Toggle Watch", InputActionType.Button, watchBinding);
            back = new InputAction("Back", InputActionType.Button, "<Keyboard>/escape");
        }
        public void SetWatchBinding(string path) => watch.ApplyBindingOverride(path);
        private void OnEnable() { watch.Enable(); back.Enable(); }
        private void Update()
        {
            if (watch.WasPressedThisFrame()) WatchPressed?.Invoke();
            else if (back.WasPressedThisFrame()) BackPressed?.Invoke();
        }
        private void OnDisable() { watch.Disable(); back.Disable(); }
        private void OnDestroy() { watch.Dispose(); back.Dispose(); }
    }
}

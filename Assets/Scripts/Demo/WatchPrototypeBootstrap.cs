using UnityEngine;

namespace SpaceRunner.Demo
{
    // Only connects scene objects. Level geometry and placement are authored in the scene.
    public sealed class WatchPrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private FirstPersonController scenePlayer;
        [SerializeField] private WatchSystem watchSystem;
        [SerializeField] private Combat.CombatSystem combatSystem;
        [SerializeField] private bool pauseGameplayWhileWatchOpen = true;
        public GameSession Session => watchSystem.Session;
        public WatchController Watch => watchSystem.Controller;
        public WatchMenuView WatchMenu => watchSystem.Rig.Menu;
        public FirstPersonController Player => scenePlayer;
        public WatchSystem System => watchSystem;
        public Combat.CombatSystem Combat => combatSystem;

        private void Awake()
        {
            if (scenePlayer == null || watchSystem == null || combatSystem == null)
            {
                Debug.LogError("Assign the scene Player, Watch System and Combat System on Watch Prototype.", this);
                enabled = false;
                return;
            }
            var body = scenePlayer.GetComponent<Renderer>();
            if (body != null) body.forceRenderingOff = true;
            watchSystem.Initialize(scenePlayer);
            Watch.PauseGameplayWhileOpen = pauseGameplayWhileWatchOpen;
            combatSystem.Initialize(Session, scenePlayer.transform, scenePlayer.GetComponent<Collider>());
        }
        private void OnValidate()
        {
            if (watchSystem != null && Watch != null) Watch.PauseGameplayWhileOpen = pauseGameplayWhileWatchOpen;
        }
    }
}

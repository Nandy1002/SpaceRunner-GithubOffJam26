using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SpaceRunner
{
    // Add this prefab once to a level and assign the existing player. No replacement motor is created.
    public sealed class WatchSystem : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private WatchRig watchRigPrefab;
        [SerializeField] private MainMenuView screenUIPrefab;
        [SerializeField] private GravityMenuBinding gravityMenuPrefab;
        [SerializeField] private bool pauseGameplayWhileWatchOpen = true;
        [SerializeField] private string watchBinding = "<Keyboard>/r";
        public GameSession Session { get; private set; }
        public WatchController Controller { get; private set; }
        public WatchRig Rig { get; private set; }
        public FirstPersonController Player => player;
        public GravityChangerUI GravityMenu { get; private set; }
        private GameObject screenUI, gravityUI;

        private void Start() { if (Session == null && player != null) Initialize(player); }
        public void Initialize(FirstPersonController playerController)
        {
            if (Session != null) return;
            player = playerController;
            if (EventSystem.current == null)
            {
                var events = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
            Session = gameObject.AddComponent<GameSession>();
            var settings = gameObject.AddComponent<PlayerSettings>();
            var input = gameObject.AddComponent<PlayerInputReader>();
            input.SetWatchBinding(watchBinding);
            Rig = Instantiate(watchRigPrefab); // World root avoids inherited non-uniform player scale.
            Rig.name = "Watch Rig";
            Rig.Initialize(player.playerCamera, Session, settings);
            Controller = gameObject.AddComponent<WatchController>();
            Controller.PauseGameplayWhileOpen = pauseGameplayWhileWatchOpen;
            Controller.Initialize(Session, input, Rig.Presentation, Rig.Menu);
            gameObject.AddComponent<NandyPlayerBridge>().Initialize(Session, Controller, player, settings);
            if (screenUIPrefab != null)
            {
                var menu = Instantiate(screenUIPrefab, transform);
                screenUI = menu.gameObject;
                menu.Initialize(Session);
            }
            if (gravityMenuPrefab != null)
            {
                var gravity = Instantiate(gravityMenuPrefab, transform);
                gravityUI = gravity.gameObject;
                gravity.Initialize(player);
                GravityMenu = gravity.Menu;
            }
        }
        private void OnValidate() { if (Controller != null) Controller.PauseGameplayWhileOpen = pauseGameplayWhileWatchOpen; }
        private void OnDestroy() { if (Rig != null) Destroy(Rig.gameObject); }
    }
}

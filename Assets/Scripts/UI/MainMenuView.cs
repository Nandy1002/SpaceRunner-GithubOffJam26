using UnityEngine;
using UnityEngine.UI;

namespace SpaceRunner
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject menuRoot, hudRoot, confirmation;
        [SerializeField] private Button startButton, quitButton, cancelButton, confirmButton;
        [SerializeField] private Text lastRun;
        [SerializeField] private Text title;
        [SerializeField] private CanvasGroup hud;
        private GameSession session;
        public void Initialize(GameSession gameSession)
        {
            session = gameSession;
            startButton.onClick.AddListener(session.StartRun);
            quitButton.onClick.AddListener(() => confirmation.SetActive(true));
            cancelButton.onClick.AddListener(() => confirmation.SetActive(false));
            confirmButton.onClick.AddListener(session.QuitApplication);
            session.RunChanged += SetPlaying;
            SetPlaying(session.IsPlaying);
        }
        private void SetPlaying(bool playing)
        {
            menuRoot.SetActive(!playing); hudRoot.SetActive(playing); confirmation.SetActive(false);
            lastRun.text = session.Timer.ElapsedSeconds > 0 ? "LAST RUN  " + session.Timer.DisplayTime : "";
            if (title != null) title.text = session.LastEndReason == RunEndReason.PlayerDied ? "YOU DIED" : "SPACE RUNNER";
        }
        private void Update()
        {
            if (session != null) hud.alpha = session.CanControlPlayer && !session.PointerRequests.IsPaused ? 1 : 0;
        }
        private void OnDestroy() { if (session != null) session.RunChanged -= SetPlaying; }
    }
}

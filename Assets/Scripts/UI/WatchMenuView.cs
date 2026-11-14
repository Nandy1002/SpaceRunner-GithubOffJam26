using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SpaceRunner
{
    [Serializable]
    public sealed class WatchPageEntry
    {
        public string id;
        public string title;
        public GameObject content;
        public Button openButton;
    }

    [RequireComponent(typeof(CanvasGroup))]
    public sealed class WatchMenuView : MonoBehaviour
    {
        [Header("Prefab bindings — artwork and layout are edited on the children")]
        [SerializeField] private Text title;
        [SerializeField] private Text timer;
        [SerializeField] private Text status;
        [SerializeField] private GameObject home;
        [SerializeField] private Transform pageHost;
        [SerializeField] private Button resumeButton, endRunButton, quitButton, backButton;
        [SerializeField] private SettingsPage settingsPage;
        [SerializeField] private WatchPageEntry[] pages;
        [Header("Confirmation")]
        [SerializeField] private GameObject confirmation;
        [SerializeField] private Text confirmTitle, confirmBody;
        [SerializeField] private Button cancelButton, confirmButton;
        public event Action ResumeRequested;
        public bool IsVisible { get; private set; }
        public Transform PageHost => pageHost;
        private readonly Dictionary<string, WatchPageEntry> registeredPages = new();
        private GameSession session;
        private PlayerSettings settings;
        private CanvasGroup group;
        private WatchFaceZoom focus;
        private string currentPage;
        private UnityAction confirmAction;

        public void Initialize(GameSession gameSession, PlayerSettings playerSettings)
        {
            session = gameSession; settings = playerSettings;
            group = GetComponent<CanvasGroup>();
            settingsPage.Initialize(settings);
            resumeButton.onClick.AddListener(() => ResumeRequested?.Invoke());
            endRunButton.onClick.AddListener(() => ShowConfirmation("END THIS RUN?", "Stop the timer and return to the main menu.", session.ReturnToMenu));
            quitButton.onClick.AddListener(() => ShowConfirmation("QUIT GAME?", "This will end your current run.", session.QuitApplication));
            backButton.onClick.AddListener(() => ShowPage(null));
            cancelButton.onClick.AddListener(HideConfirmation);
            confirmButton.onClick.AddListener(() => { var action = confirmAction; HideConfirmation(); action?.Invoke(); });
            foreach (var page in pages) RegisterPage(page);
            ShowPage(null);
            HideConfirmation();
            SetVisible(false);
        }

        // A new feature supplies its own prefab and button; no graphics are generated here.
        public void RegisterPage(WatchPageEntry page)
        {
            registeredPages.Add(page.id, page);
            page.content.SetActive(false);
            page.openButton.onClick.AddListener(() => ShowPage(page.id));
        }
        public void SetPresentation(WatchFaceZoom presentation) { focus = presentation; focus.SetExpanded(IsVisible); }
        private void ShowPage(string id)
        {
            if (currentPage == "settings") settings.Save();
            currentPage = id;
            home.SetActive(id == null);
            foreach (var page in registeredPages.Values) page.content.SetActive(page.id == id);
            backButton.gameObject.SetActive(id != null);
            title.text = id == null ? "RUN TIME" : registeredPages[id].title;
        }
        public bool TryGoBack()
        {
            if (confirmation.activeSelf) { HideConfirmation(); return true; }
            if (currentPage != null) { ShowPage(null); return true; }
            return false;
        }
        public void SetVisible(bool visible)
        {
            if (!visible && currentPage == "settings") settings.Save();
            IsVisible = visible;
            group.interactable = group.blocksRaycasts = false;
            if (visible) { HideConfirmation(); ShowPage(null); }
            if (focus != null) focus.SetExpanded(visible);
        }
        private void ShowConfirmation(string heading, string body, UnityAction action)
        {
            confirmTitle.text = heading; confirmBody.text = body; confirmAction = action;
            confirmation.SetActive(true);
        }
        private void HideConfirmation() { confirmation.SetActive(false); confirmAction = null; }
        private void Update()
        {
            if (session == null) return;
            timer.text = session.Timer.DisplayTime;
            status.text = session.Pause.IsPaused ? "PAUSED" : "RUNNING";
        }
    }
}

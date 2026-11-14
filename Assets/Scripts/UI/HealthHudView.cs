using UnityEngine;
using UnityEngine.UI;

namespace SpaceRunner.Combat
{
    public sealed class HealthHudView : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Slider bar;
        [SerializeField] private CanvasGroup group;
        private GameSession session;
        private HealthComponent health;
        public void Initialize(GameSession gameSession, HealthComponent playerHealth)
        {
            session = gameSession; health = playerHealth;
            health.Changed += Refresh;
            Refresh();
        }
        private void Refresh()
        {
            label.text = $"HEALTH  {health.Current} / {health.Maximum}";
            bar.maxValue = health.Maximum;
            bar.SetValueWithoutNotify(health.Current);
        }
        private void Update()
        {
            group.alpha = session != null && session.IsPlaying && !session.PointerRequests.IsPaused ? 1 : 0;
        }
        private void OnDestroy() { if (health != null) health.Changed -= Refresh; }
    }
}

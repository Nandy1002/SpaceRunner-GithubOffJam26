using UnityEngine;

namespace SpaceRunner
{
    /// <summary>Place on the Animator object; clips call Raised/Lowered animation events.</summary>
    public sealed class AnimatorWatchPresentation : WatchPresentation
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string openParameter = "WatchOpen";
        [SerializeField] private string closedState = "WatchClosed";
        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        public override void SetRaised(bool raised) => animator.SetBool(openParameter, raised);
        public void Raised() => NotifySettled(true);
        public void Lowered() => NotifySettled(false);

        public override void SnapClosed()
        {
            if (animator == null) return;
            animator.SetBool(openParameter, false);
            animator.Play(closedState, 0, 0);
            animator.Update(0);
        }
    }
}

using UnityEngine;

namespace SpaceRunner
{
    public sealed class PlaceholderWatchPresentation : WatchPresentation
    {
        [SerializeField, Min(0.05f)] private float transitionSeconds = 0.20f;
        [SerializeField] private Transform armRoot;
        private readonly Vector3 loweredPosition = new(-0.35f, -0.85f, 0.3f);
        private readonly Vector3 raisedPosition = new(-0.13f, -0.30f, 0.60f);
        private readonly Quaternion loweredRotation = Quaternion.Euler(35, -25, -45);
        private readonly Quaternion raisedRotation = Quaternion.Euler(-62, 0, -5);
        private float progress;
        private bool targetRaised, moving;

        private void Awake() { if (armRoot == null) armRoot = transform; SnapClosed(); }

        public void Initialize(Transform root) { armRoot = root; SnapClosed(); }
        public override void SetRaised(bool raised) { targetRaised = raised; moving = true; }

        private void Update()
        {
            if (!moving || armRoot == null) return;
            progress = Mathf.MoveTowards(progress, targetRaised ? 1 : 0, Time.unscaledDeltaTime / transitionSeconds);
            ApplyPose();
            if (progress == (targetRaised ? 1 : 0))
            {
                moving = false;
                NotifySettled(targetRaised);
            }
        }

        public override void SnapClosed()
        {
            moving = targetRaised = false;
            progress = 0;
            if (armRoot != null) ApplyPose();
        }

        private void ApplyPose()
        {
            float eased = Mathf.SmoothStep(0, 1, progress);
            armRoot.localPosition = Vector3.Lerp(loweredPosition, raisedPosition, eased);
            armRoot.localRotation = Quaternion.Slerp(loweredRotation, raisedRotation, eased);
        }
    }
}

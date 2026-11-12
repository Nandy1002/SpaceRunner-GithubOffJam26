using UnityEngine;

namespace SpaceRunner
{
    // Dolly the watch camera toward the physical face. The UI never leaves its wrist socket.
    [DefaultExecutionOrder(100)]
    public sealed class WatchFaceZoom : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float duration = 0.18f;
        [SerializeField] private Vector3 closeupAngles = new(-12, 5, -3);
        [SerializeField, Range(0.5f, 0.95f)] private float screenCoverage = 0.85f;
        [SerializeField] private Camera watchCamera;
        [SerializeField] private RectTransform face;
        [SerializeField] private RectTransform backdrop;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Renderer[] armVisuals;
        private Transform playerView;
        private float progress;
        private bool expanded;
        public bool IsExpanded => expanded && progress >= 1;
        public Camera Camera => watchCamera;

        public void Initialize(Transform view) { playerView = view; ApplyPose(); }
        public void SetExpanded(bool value)
        {
            expanded = value;
            if (!value) group.interactable = group.blocksRaycasts = false;
        }
        private void LateUpdate()
        {
            if (playerView == null) return;
            progress = Mathf.MoveTowards(progress, expanded ? 1 : 0, Time.unscaledDeltaTime / duration);
            ApplyPose();
        }
        private void ApplyPose()
        {
            if (playerView == null) return;
            float eased = Mathf.SmoothStep(0, 1, progress);
            Vector2 size = Vector2.Scale(face.rect.size, new Vector2(face.lossyScale.x, face.lossyScale.y));
            float halfFov = Mathf.Tan(watchCamera.fieldOfView * Mathf.Deg2Rad / 2);
            float distance = Mathf.Max(size.y, size.x / watchCamera.aspect) / (2 * halfFov * screenCoverage);
            Quaternion targetRotation = face.rotation * Quaternion.Euler(closeupAngles);
            Vector3 targetPosition = face.position - targetRotation * Vector3.forward * distance;
            watchCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(playerView.position, targetPosition, eased),
                Quaternion.Slerp(playerView.rotation, targetRotation, eased));
            backdrop.localScale = Vector3.one * Mathf.Lerp(1, 6, eased);
            // Only geometry is hidden at close range. The dial itself stays attached and rendered.
            foreach (var visual in armVisuals) if (visual != null) visual.forceRenderingOff = progress > 0.8f;
            group.interactable = group.blocksRaycasts = IsExpanded;
        }
        public void SnapClosed()
        {
            expanded = false; progress = 0;
            group.interactable = group.blocksRaycasts = false;
            foreach (var visual in armVisuals) if (visual != null) visual.forceRenderingOff = false;
            if (playerView != null) ApplyPose();
        }
        private void OnDisable() => SnapClosed();
    }
}

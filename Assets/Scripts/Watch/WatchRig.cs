using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SpaceRunner
{
    // This prefab contains all replaceable models, animation, camera and UI references.
    [DefaultExecutionOrder(50)]
    public sealed class WatchRig : MonoBehaviour
    {
        [SerializeField] private WatchPresentation presentation;
        [SerializeField] private WatchMenuView menu;
        [SerializeField] private WatchFaceZoom focus;
        public WatchPresentation Presentation => presentation;
        public WatchMenuView Menu => menu;
        public WatchFaceZoom Focus => focus;
        private Camera playerCamera;
        private int originalCullingMask;
        public void Initialize(Camera view, GameSession session, PlayerSettings settings)
        {
            playerCamera = view;
            transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
            originalCullingMask = view.cullingMask;
            view.cullingMask &= ~(1 << 30);
            view.GetUniversalAdditionalCameraData().cameraStack.Add(focus.Camera);
            menu.Initialize(session, settings);
            focus.Initialize(view.transform);
            menu.SetPresentation(focus);
        }
        private void LateUpdate()
        {
            if (playerCamera != null) transform.SetPositionAndRotation(playerCamera.transform.position, playerCamera.transform.rotation);
        }
        private void OnDestroy()
        {
            if (playerCamera == null) return;
            playerCamera.GetUniversalAdditionalCameraData().cameraStack.Remove(focus.Camera);
            playerCamera.cullingMask = originalCullingMask;
        }
    }
}

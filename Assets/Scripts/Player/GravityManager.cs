using UnityEngine;

public class GravityManager : MonoBehaviour
{
    [SerializeField] private GravityChangerUI gravityChangerUI;
    [SerializeField] private FirstPersonController playerController;

    private void Awake()
    {
        if (playerController == null)
        {
            Debug.LogWarning("PlayerController reference is not set in gravity manager", this);
        }
    }

    private void OnEnable()
    {
        if (gravityChangerUI != null)
        {
            gravityChangerUI.GravitySelected += SwitchGravity;
        }
    }

    private void OnDisable()
    {
        if (gravityChangerUI != null)
        {
            gravityChangerUI.GravitySelected -= SwitchGravity;
        }
    }

    private void SwitchGravity(int directionIndex)
    {

        if (playerController == null)
        {
            Debug.LogWarning("no FirstPersonController was found.", this);
            return;
        }

        float snappedPlayerYaw = playerController.GetSnappedPlayerYaw();
        Vector3 snappedPlayerRight = playerController.GetSnappedPlayerRight();
        Vector3 snappedPlayerUp = playerController.GetSnappedPlayerUp();
        Vector3 newGravity;

        // Resolve each direction from the player's current, gravity-aligned frame.
        switch (directionIndex)
        {
            case 0:
                newGravity = -snappedPlayerUp * 9.81f;
                break;
            case 1:
                newGravity = (-snappedPlayerRight - snappedPlayerUp).normalized * 9.81f;
                break;
            case 2:
                newGravity = -snappedPlayerRight * 9.81f;
                break;
            case 3:
                newGravity = (-snappedPlayerRight + snappedPlayerUp).normalized * 9.81f;
                break;
            case 4:
                newGravity = snappedPlayerUp * 9.81f;
                break;
            case 5:
                newGravity = (snappedPlayerRight + snappedPlayerUp).normalized * 9.81f;
                break;
            case 6:
                newGravity = snappedPlayerRight * 9.81f;
                break;
            case 7:
                newGravity = (snappedPlayerRight - snappedPlayerUp).normalized * 9.81f;
                break;
            default:
                Debug.LogWarning($"No gravity direction is configured for index: ", this);
                return;
        }

        Physics.gravity = newGravity;
        playerController.AlignToGravity(newGravity, snappedPlayerYaw);
        Debug.Log($"Gravity set to (index {directionIndex}) using player-local yaw {snappedPlayerYaw:0}°.", this);
    }
}

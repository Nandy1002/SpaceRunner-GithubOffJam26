using UnityEngine;

public class GravityManager : MonoBehaviour
{
    [SerializeField] private GravityChangerUI gravityChangerUI;
    [SerializeField] private FirstPersonController playerController;

    public void BindPlayer(FirstPersonController player) => playerController = player;

    private void Start()
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
        string directionName;

        // Resolve each direction from the player's current, gravity-aligned frame.
        switch (directionIndex)
        {
            case 0:
                newGravity = -snappedPlayerUp * 9.81f;
                directionName = "normal/down";
                break;
            case 1:
                newGravity = (-snappedPlayerRight - snappedPlayerUp).normalized * 9.81f;
                directionName = "down-left diagonal";
                break;
            case 2:
                newGravity = -snappedPlayerRight * 9.81f;
                directionName = "left wall";
                break;
            case 3:
                newGravity = (-snappedPlayerRight + snappedPlayerUp).normalized * 9.81f;
                directionName = "up-left diagonal";
                break;
            case 4:
                newGravity = snappedPlayerUp * 9.81f;
                directionName = "upside down";
                break;
            case 5:
                newGravity = (snappedPlayerRight + snappedPlayerUp).normalized * 9.81f;
                directionName = "up-right diagonal";
                break;
            case 6:
                newGravity = snappedPlayerRight * 9.81f;
                directionName = "right wall";
                break;
            case 7:
                newGravity = (snappedPlayerRight - snappedPlayerUp).normalized * 9.81f;
                directionName = "down-right diagonal";
                break;
            default:
                Debug.LogWarning($"No gravity direction is configured for index {directionIndex}.", this);
                return;
        }

        Physics.gravity = newGravity;
        playerController.AlignToGravity(newGravity, snappedPlayerYaw);
        Debug.Log($"Gravity set to {directionName} (index {directionIndex}) using player-local yaw {snappedPlayerYaw:0}°.", this);
    }
}

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
            playerController = FindFirstObjectByType<FirstPersonController>();
            if (playerController == null)
            {
                Debug.LogWarning("PlayerController reference is not set in gravity manager", this);
            }
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

    /// <summary>
    /// Changes gravity to one of 4 cardinal directions relative to the player's current frame:
    /// 0 = Down (Normal), 1 = Left (Left Wall), 2 = Up (Ceiling), 3 = Right (Right Wall).
    /// Gracefully maps legacy 8-slice indices as well.
    /// </summary>
    private void SwitchGravity(int directionIndex)
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FirstPersonController>();
            if (playerController == null)
            {
                Debug.LogWarning("No FirstPersonController was found.", this);
                return;
            }
        }

        float snappedPlayerYaw = playerController.GetSnappedPlayerYaw();
        Vector3 snappedPlayerRight = playerController.GetSnappedPlayerRight();
        Vector3 snappedPlayerUp = playerController.GetSnappedPlayerUp();
        Vector3 newGravity;
        string directionName;

        // Map index into one of the 4 cardinal directions
        int cardinalIndex;
        switch (directionIndex)
        {
            case 0:
                cardinalIndex = 0; // Down
                break;
            case 1:
                cardinalIndex = 1; // Left
                break;
            case 2:
                // If 8-slice: 2 is Left wall; if 4-slice: 2 is Up
                // We handle both: if directionIndex came from GravityChangerUI cardinal (0=Down, 1=Left, 2=Up, 3=Right)
                cardinalIndex = directionIndex;
                break;
            case 3:
                // 4-direction Right
                cardinalIndex = 3;
                break;
            case 4:
                // 8-direction Up
                cardinalIndex = 2;
                break;
            case 5:
            case 6:
                // 8-direction Right
                cardinalIndex = 3;
                break;
            case 7:
                // 8-direction Down-Right
                cardinalIndex = 0;
                break;
            default:
                cardinalIndex = 0;
                break;
        }

        switch (cardinalIndex)
        {
            case 0:
                newGravity = -snappedPlayerUp * 9.81f;
                directionName = "Normal Floor (Down)";
                break;
            case 1:
                newGravity = -snappedPlayerRight * 9.81f;
                directionName = "Left Wall";
                break;
            case 2:
                newGravity = snappedPlayerUp * 9.81f;
                directionName = "Ceiling (Up)";
                break;
            case 3:
                newGravity = snappedPlayerRight * 9.81f;
                directionName = "Right Wall";
                break;
            default:
                Debug.LogWarning($"No gravity direction is configured for index {directionIndex}.", this);
                return;
        }

        Physics.gravity = newGravity;
        playerController.AlignToGravity(newGravity, snappedPlayerYaw);
        Debug.Log($"Gravity set to {directionName} (index {directionIndex} -> cardinal {cardinalIndex}) using player-local yaw {snappedPlayerYaw:0}°.", this);
    }
}
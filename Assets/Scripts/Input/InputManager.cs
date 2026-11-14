using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private InputSystem_Actions controls;
    [SerializeField, Range(1f, 2f)] private float sprintHoldDuration = 1.5f;
    private float forwardHoldTime;

    // Optional integration hooks. Defaults preserve the standalone player behaviour.
    public bool GameplayInputBlocked { get; set; }
    public bool GravitySelectionBlocked { get; set; }
    public bool CursorExternallyManaged { get; set; }
    private int suppressLookThroughFrame = -1;
    public void SuppressLookForCursorChange() => suppressLookThroughFrame = Time.frameCount + 1;

    public InputSystem_Actions.PlayerActions Player => controls.Player;
    public InputSystem_Actions.UIActions UI => controls.UI;
    public Vector2 Move => GameplayInputBlocked ? Vector2.zero : Player.Move.ReadValue<Vector2>();
    public Vector2 Look => GameplayInputBlocked || Time.frameCount <= suppressLookThroughFrame ? Vector2.zero : Player.Look.ReadValue<Vector2>();
    public bool LookUsesPointer => Player.Look.activeControl?.device is Pointer;
    public bool ZoomPressedThisFrame => !GameplayInputBlocked && Player.Gravity.WasPressedThisFrame();
    public bool ZoomReleasedThisFrame => !GameplayInputBlocked && Player.Gravity.WasReleasedThisFrame();
    public bool ZoomHeld => !GameplayInputBlocked && Player.Gravity.IsPressed();
    public bool SprintHeld => !GameplayInputBlocked && forwardHoldTime >= sprintHoldDuration;
    public bool JumpPressedThisFrame => !GameplayInputBlocked && Player.Jump.WasPressedThisFrame();
    public bool DashPressedThisFrame => !GameplayInputBlocked && Player.Dash.WasPressedThisFrame();
    public bool CrouchPressedThisFrame => !GameplayInputBlocked && Player.Crouch.WasPressedThisFrame();
    public bool CrouchHeld => !GameplayInputBlocked && Player.Crouch.IsPressed();
    public bool CrouchReleasedThisFrame => !GameplayInputBlocked && Player.Crouch.WasReleasedThisFrame();
    public bool TabPressedThisFrame => !GameplayInputBlocked && !GravitySelectionBlocked && Player.Tab.WasPressedThisFrame();
    public bool TabReleasedThisFrame => !GameplayInputBlocked && !GravitySelectionBlocked && Player.Tab.WasReleasedThisFrame();
    public bool TabHeld => !GameplayInputBlocked && !GravitySelectionBlocked && Player.Tab.IsPressed();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateIfMissing()
    {
        if (Instance == null)
        {
            new GameObject(nameof(InputManager)).AddComponent<InputManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        controls = new InputSystem_Actions();
    }

    private void Update()
    {
        if (GameplayInputBlocked) return;
        if (Move.y > 0.1f)
        {
            forwardHoldTime += Time.deltaTime;
        }
        else
        {
            forwardHoldTime = 0f;
        }
    }

    private void OnEnable()
    {
        controls?.Enable();
    }

    private void OnDisable()
    {
        controls?.Disable();
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        controls?.Dispose();
        Instance = null;
    }
}

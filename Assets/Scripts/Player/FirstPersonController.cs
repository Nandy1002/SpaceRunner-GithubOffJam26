using UnityEngine;

[RequireComponent(typeof(PlayerState))]
public class FirstPersonController : MonoBehaviour
{
    private Rigidbody rb;
    private PlayerState playerState;
    private WallRunAbility wallRunAbility;
    private float currentWallRunLean;
    private Quaternion gravityOrientation;
    private Quaternion gravityTransitionStartOrientation;
    private float gravityTransitionProgress = 1f;
    private bool gravityRotationConstraintsReleased;
    private Vector3 GravityUp => Physics.gravity.sqrMagnitude > 0.001f
        ? -Physics.gravity.normalized
        : Vector3.up;

    [Header("Camera Movement")]
    public Camera playerCamera;
    [SerializeField, Min(0.01f)] private float gravityRotationTransitionDuration = 0.5f;

    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 0.15f;
    public float gamepadLookSpeed = 120f;
    public float maxLookAngle = 50f;

    public bool lockCursor = true;

    // Internal Variables
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    [Header("Camera Zoom")]
    public bool enableZoom = true;
    public bool holdToZoom = false;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;

    // Internal Variables
    private bool isZoomed = false;


    [Header("Movement")]
    public bool playerCanMove = true;
    public float walkSpeed = 8f;
    public float maxVelocityChange = 15f;
    public float speedTransitionRate = 1.5f;
    public LayerMask groundLayerMask = ~0;

    // Internal Variables
    private float currentMoveSpeed;

    [Header("Sprint")]
    public bool enableSprint = true;
    public float sprintSpeed = 12f;
    public float sprintFOV = 80f;

    // Internal Variables

    [Header("Slide")]
    public bool enableSlide = true;
    public float slideDuration = 0.8f;
    public float slideSpeedMultiplier = 1.25f;
    public float slideDeceleration = 12f;
    public float slideCooldown = 0.4f;
    [Range(0.35f, 0.75f)] public float slideHeightMultiplier = 0.6f;
    [Range(-80f, 80f)] public float slideTiltAngle = -60f;
    public float slidePostureTransitionSpeed = 0.8f;
    public float slideTiltTransitionSpeed = 180f;

    private float slideTimeRemaining;
    private float slideCooldownRemaining;
    private float slideCurrentSpeed;
    private Vector3 slideDirection;
    private float currentSlideTilt;
    private float currentPostureHeightMultiplier = 1f;
    private RigidbodyConstraints standingRigidbodyConstraints;


    [Header("Jump")]
    public bool enableJump = true;
    public float jumpPower = 10f;
    public float jumpBufferTime = 0.2f;
    public float risingGravityMultiplier = 1.5f;
    public float fallingGravityMultiplier = 2.5f;

    // Internal Variables
    private float jumpBufferRemaining;
    private bool groundContactThisStep;


    [Header("Crouch")]
    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public float crouchHeight = .75f;
    public float speedReduction = .5f;

    // Internal Variables
    private CapsuleCollider playerCapsule;
    private float standingCapsuleHeight;
    private Vector3 standingCapsuleCenter;
    private float standingWalkSpeed;


    [Header("Head Bob")]
    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    [Range(1f, 2f)] public float sprintBobSpeedMultiplier = 1.35f;
    [Range(0f, 1f)] public float sprintBobAmountMultiplier = 0.5f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);

    // Internal Variables
    private Vector3 jointOriginalPos;
    private Vector3 jointPosturePosition;
    private float timer = 0;

    private PlayerStateEnum CurrentState => playerState.GetCurrentState();
    private bool IsGrounded => IsControllerLocomotionState(CurrentState) && CurrentState != PlayerStateEnum.OnAir;
    private bool IsSliding => CurrentState == PlayerStateEnum.Slide;
    private bool IsCrouched => CurrentState == PlayerStateEnum.Crouch;
    private bool IsSprinting => CurrentState == PlayerStateEnum.Run || IsSliding;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerState = GetComponent<PlayerState>();
        wallRunAbility = GetComponent<WallRunAbility>();
        gravityOrientation = transform.rotation;
        standingRigidbodyConstraints = rb.constraints;
        playerCapsule = GetComponent<CapsuleCollider>();

        if (playerState.IsInState(PlayerStateEnum.Idle))
        {
            playerState.SetCurrentState(PlayerStateEnum.OnAir);
        }

        // Set internal variables
        playerCamera.fieldOfView = fov;
        standingCapsuleHeight = playerCapsule.height;
        standingCapsuleCenter = playerCapsule.center;
        standingWalkSpeed = walkSpeed;
        currentMoveSpeed = walkSpeed;
        jointOriginalPos = joint.localPosition;
        jointPosturePosition = jointOriginalPos;

    }

    void Start()
    {
        if(lockCursor && !InputManager.Instance.CursorExternallyManaged)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

    }

    float camRotation;

    private void Update()
    {
        InputManager cameraInput = InputManager.Instance;
        bool gravityUIOpen = cameraInput.TabHeld;
        if (!cameraInput.CursorExternallyManaged && gravityUIOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
        }
        else if (!cameraInput.CursorExternallyManaged && lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (cameraInput.GameplayInputBlocked) return;

        #region Camera

        // Control camera movement
        if(cameraCanMove && !gravityUIOpen)
        {
            Vector2 lookInput = cameraInput.Look;
            float lookScale = cameraInput.LookUsesPointer ? mouseSensitivity : gamepadLookSpeed * Time.deltaTime;

            yaw += lookInput.x * lookScale;

            if (!invertCamera)
            {
                pitch -= lookScale * lookInput.y;
            }
            else
            {
                // Inverted Y
                pitch += lookScale * lookInput.y;
            }
            // Clamp pitch between lookAngle
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);


        }

        #region Camera Zoom

        if (enableZoom)
        {
            if (InputManager.Instance.ZoomPressedThisFrame && !holdToZoom && !IsSprinting)
            {
                isZoomed = !isZoomed;
            }

            if (holdToZoom && !IsSprinting)
            {
                if (InputManager.Instance.ZoomPressedThisFrame)
                {
                    isZoomed = true;
                }
                else if (InputManager.Instance.ZoomReleasedThisFrame)
                {
                    isZoomed = false;
                }
            }
        }

        if (IsSprinting)
        {
            isZoomed = false;
        }

        float sprintProgress = Mathf.InverseLerp(walkSpeed, sprintSpeed, currentMoveSpeed);
        float targetFOV = Mathf.Lerp(fov, sprintFOV, sprintProgress);
        if (enableZoom && isZoomed)
        {
            targetFOV = zoomFOV;
        }

        if (enableZoom && isZoomed)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomStepTime * Time.deltaTime);
        }
        else
        {
            // Sprint FOV follows currentMoveSpeed, which changes using speedTransitionRate.
            playerCamera.fieldOfView = targetFOV;
        }
        #endregion
        #endregion
        UpdateSlidePosture(Time.deltaTime);

        #region Jump Input Buffer

        if (InputManager.Instance.JumpPressedThisFrame)
        {
            // WallRunAbility consumes this press as a wall jump, so don't also buffer
            // it for an automatic jump when the player lands.
            jumpBufferRemaining = CurrentState == PlayerStateEnum.WallRun ? 0f : jumpBufferTime;
        }
        else if (jumpBufferRemaining > 0f)
        {
            jumpBufferRemaining -= Time.deltaTime;
        }

        #endregion

        #region Crouch and Slide Input

        InputManager input = InputManager.Instance;
        if (input.CrouchPressedThisFrame)
        {
            if (CanStartSlide(input.Move))
            {
                StartSlide(input.Move);
            }
            else if (enableCrouch && IsControllerLocomotionState(CurrentState) && !IsSliding && IsGrounded
                && (!holdToCrouch || !playerState.IsInState(PlayerStateEnum.Crouch)))
            {
                Crouch();
            }
        }

        if (enableCrouch && holdToCrouch && playerState.IsInState(PlayerStateEnum.Crouch)
            && input.CrouchReleasedThisFrame && !IsSliding)
        {
            Crouch();
        }

        if (enableCrouch && holdToCrouch && playerState.IsInState(PlayerStateEnum.Crouch)
            && !input.CrouchHeld && !IsSliding && CanStandUp())
        {
            Crouch();
        }

        #endregion

        if (enableJump && jumpBufferRemaining > 0f && IsGrounded)
        {
            Jump();
            jumpBufferRemaining = 0f;
        }

        Quaternion smoothedGravityOrientation = gravityOrientation;
        if (gravityTransitionProgress < 1f)
        {
            gravityTransitionProgress = Mathf.Min(
                1f, gravityTransitionProgress + Time.deltaTime / gravityRotationTransitionDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, gravityTransitionProgress);
            smoothedGravityOrientation = Quaternion.Slerp(
                gravityTransitionStartOrientation, gravityOrientation, easedProgress);
        }

        transform.rotation = smoothedGravityOrientation * Quaternion.Euler(currentSlideTilt, yaw, 0f);
        if (gravityTransitionProgress >= 1f && gravityRotationConstraintsReleased)
        {
            rb.rotation = transform.rotation;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = IsSliding
                ? standingRigidbodyConstraints & ~RigidbodyConstraints.FreezeRotationX
                : standingRigidbodyConstraints;
            gravityRotationConstraintsReleased = false;
        }

        float targetWallRunLean = wallRunAbility != null ? wallRunAbility.CameraLeanAngle : 0f;
        currentWallRunLean = Mathf.MoveTowardsAngle(currentWallRunLean, targetWallRunLean, 180f * Time.deltaTime);
        playerCamera.transform.localEulerAngles = new Vector3(pitch - currentSlideTilt, 0f, currentWallRunLean);

        if(enableHeadBob)
        {
            HeadBob();
        }
    }

    private void FixedUpdate()
    {
        Vector3 gravityUp = GravityUp;
        float velocityAlongGravityUp = Vector3.Dot(rb.linearVelocity, gravityUp);
        bool groundedThisStep = groundContactThisStep && velocityAlongGravityUp <= 0.1f;
        groundContactThisStep = false;

        // WallRunAbility applies its own reduced gravity while wall-running.
        if (!groundedThisStep && CurrentState != PlayerStateEnum.WallRun)
        {
            float gravityMultiplier = velocityAlongGravityUp > 0f
                ? risingGravityMultiplier
                : fallingGravityMultiplier;
            rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
        }

        PlayerStateEnum state = CurrentState;
        if (IsControllerLocomotionState(state))
        {
            // Keep the slide state through brief missed ground-contact samples. The slide timer
            // ends the slide; if the player has actually left the ground, EndSlide moves to OnAir.
            if (!groundedThisStep && state != PlayerStateEnum.OnAir && state != PlayerStateEnum.Slide)
            {
                SetState(PlayerStateEnum.OnAir);
            }
            else if (groundedThisStep && (state == PlayerStateEnum.OnAir
                || state == PlayerStateEnum.Idle || state == PlayerStateEnum.Walk || state == PlayerStateEnum.Run))
            {
                UpdateGroundMovementState(InputManager.Instance.Move, InputManager.Instance.SprintHeld);
            }
        }

        if (!playerCanMove || !IsControllerLocomotionState(CurrentState))
        {
            return;
        }

        if (slideCooldownRemaining > 0f)
        {
            slideCooldownRemaining -= Time.fixedDeltaTime;
        }

        if (playerState.IsInState(PlayerStateEnum.Slide))
        {
            slideTimeRemaining -= Time.fixedDeltaTime;
            slideCurrentSpeed = Mathf.MoveTowards(slideCurrentSpeed, standingWalkSpeed, slideDeceleration * Time.fixedDeltaTime);
            if (slideTimeRemaining <= 0f)
            {
                EndSlide(groundedThisStep);
            }
        }

        InputManager input = InputManager.Instance;
        Vector2 moveInput = input.Move;
        state = CurrentState;
        bool sliding = state == PlayerStateEnum.Slide;
        Vector3 moveDirection = sliding
            ? slideDirection
            : new Vector3(moveInput.x, 0f, moveInput.y);
        bool sprinting = state == PlayerStateEnum.Run;
        float targetSpeed = sliding
            ? slideCurrentSpeed
            : sprinting ? sprintSpeed
            : state == PlayerStateEnum.Crouch ? standingWalkSpeed * speedReduction
            : walkSpeed;
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetSpeed, speedTransitionRate * Time.fixedDeltaTime);

        Vector3 worldMoveDirection = sliding ? moveDirection : transform.TransformDirection(moveDirection);
        Vector3 targetVelocity = worldMoveDirection * currentMoveSpeed;
        Vector3 velocityChange = targetVelocity - rb.linearVelocity;
        Vector3 tangentVelocityChange = Vector3.ProjectOnPlane(velocityChange, gravityUp);
        if (tangentVelocityChange.magnitude > maxVelocityChange)
        {
            tangentVelocityChange = tangentVelocityChange.normalized * maxVelocityChange;
        }

        rb.AddForce(tangentVelocityChange, ForceMode.VelocityChange);
    }
    private void OnCollisionStay(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Dot(collision.GetContact(i).normal, GravityUp) > 0.5f)
            {
                groundContactThisStep = true;
                return;
            }
        }
    }
    private void Jump()
    {
        if (!CanJumpFromCurrentState())
        {
            return;
        }

        if (playerState.IsInState(PlayerStateEnum.Slide))
        {
            EndSlide(IsGrounded);
        }

        rb.AddForce(GravityUp * jumpPower, ForceMode.Impulse);
        SetState(PlayerStateEnum.OnAir);
    }

    public float GetSnappedPlayerYaw()
    {
        // Snap the player's local yaw, which is measured around their current local up axis.
        float snappedYaw = Mathf.Floor((yaw + 22.5f) / 45f) * 45f;
        if (snappedYaw > 180f)
        {
            snappedYaw -= 360f;
        }
        else if (snappedYaw < -180f)
        {
            snappedYaw += 360f;
        }

        return Mathf.Approximately(snappedYaw, -180f) ? 180f : snappedYaw;
    }

    public Vector3 GetSnappedPlayerRight()
    {
        Quaternion snappedPlayerOrientation = gravityOrientation * Quaternion.Euler(0f, GetSnappedPlayerYaw(), 0f);
        return snappedPlayerOrientation * Vector3.right;
    }

    public Vector3 GetSnappedPlayerUp()
    {
        Quaternion snappedPlayerOrientation = gravityOrientation * Quaternion.Euler(0f, GetSnappedPlayerYaw(), 0f);
        return snappedPlayerOrientation * Vector3.up;
    }

    public void AlignToGravity(Vector3 gravity, float snappedPlayerYaw)
    {
        if (gravity.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion snappedPlayerOrientation = gravityOrientation * Quaternion.Euler(0f, snappedPlayerYaw, 0f);
        Vector3 gravityInPlayerHeading = Quaternion.Inverse(snappedPlayerOrientation) * gravity.normalized;
        float localZRoll = Mathf.Atan2(gravityInPlayerHeading.x, -gravityInPlayerHeading.y) * Mathf.Rad2Deg;
        gravityOrientation = snappedPlayerOrientation * Quaternion.Euler(0f, 0f, localZRoll);
        gravityTransitionStartOrientation = transform.rotation;
        gravityTransitionProgress = 0f;
        yaw = 0f;

        // The prefab freezes all Rigidbody rotation axes; release them during the animated roll.
        rb.constraints = standingRigidbodyConstraints & ~RigidbodyConstraints.FreezeRotation;
        rb.angularVelocity = Vector3.zero;
        gravityRotationConstraintsReleased = true;
    }
    // Called by the run/session adapter; standalone locomotion does not depend on a menu.
    public void ResetForRun(Vector3 position, Quaternion rotation)
    {
        rb.position = position; rb.rotation = rotation;
        rb.linearVelocity = rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(position, rotation);
        gravityOrientation = gravityTransitionStartOrientation = rotation;
        gravityTransitionProgress = 1f; gravityRotationConstraintsReleased = false;
        rb.constraints = standingRigidbodyConstraints;
        yaw = pitch = currentSlideTilt = currentWallRunLean = 0;
        slideTimeRemaining = slideCooldownRemaining = jumpBufferRemaining = timer = 0;
        currentMoveSpeed = walkSpeed; currentPostureHeightMultiplier = 1;
        isZoomed = false; groundContactThisStep = false;
        SetCapsuleHeight(1);
        jointPosturePosition = jointOriginalPos;
        joint.localPosition = jointOriginalPos;
        playerCamera.transform.localRotation = Quaternion.identity;
        playerCamera.fieldOfView = fov;
        playerState.SetCurrentState(PlayerStateEnum.OnAir);
    }

    private void Crouch()
    {
        if (playerState.IsInState(PlayerStateEnum.Crouch))
        {
            if (!CanStandUp())
            {
                return;
            }

            walkSpeed = standingWalkSpeed;
            UpdateGroundMovementState(InputManager.Instance.Move, InputManager.Instance.SprintHeld);
        }
        else if (IsGrounded)
        {
            walkSpeed = standingWalkSpeed * speedReduction;
            SetState(PlayerStateEnum.Crouch);
        }
    }
    private bool CanStartSlide(Vector2 moveInput)
    {
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, GravityUp);
        PlayerStateEnum state = CurrentState;

        return enableSlide && !playerState.IsInState(PlayerStateEnum.Slide)
            && (state == PlayerStateEnum.Walk || state == PlayerStateEnum.Run)
            && IsGrounded && slideCooldownRemaining <= 0f
            && moveInput.y > 0.1f
            && horizontalVelocity.magnitude >= standingWalkSpeed * 0.5f;
    }

    private void StartSlide(Vector2 moveInput)
    {
        slideTimeRemaining = slideDuration;
        slideDirection = Vector3.ProjectOnPlane(
            transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y)), GravityUp).normalized;
        slideCurrentSpeed = Mathf.Max(currentMoveSpeed, standingWalkSpeed) * slideSpeedMultiplier;
        RigidbodyConstraints slideRotationConstraints = gravityRotationConstraintsReleased
            ? RigidbodyConstraints.FreezeRotation
            : RigidbodyConstraints.FreezeRotationX;
        rb.constraints = standingRigidbodyConstraints & ~slideRotationConstraints;
        SetState(PlayerStateEnum.Slide);
    }

    private void EndSlide(bool grounded)
    {
        slideCooldownRemaining = slideCooldown;

        if (!grounded)
        {
            SetState(PlayerStateEnum.OnAir);
            return;
        }

        if (!CanStandUp())
        {
            walkSpeed = standingWalkSpeed * speedReduction;
            SetState(PlayerStateEnum.Crouch);
            return;
        }

        walkSpeed = standingWalkSpeed;
        UpdateGroundMovementState(InputManager.Instance.Move, InputManager.Instance.SprintHeld);
    }
    private void SetState(PlayerStateEnum state)
    {
        playerState.SetCurrentState(state);
    }

    private bool CanJumpFromCurrentState()
    {
        PlayerStateEnum state = CurrentState;
        return state == PlayerStateEnum.Idle
            || state == PlayerStateEnum.Walk
            || state == PlayerStateEnum.Run
            || state == PlayerStateEnum.Slide
            || state == PlayerStateEnum.Crouch;
    }

    private bool IsControllerLocomotionState(PlayerStateEnum state)
    {
        return state == PlayerStateEnum.Idle
            || state == PlayerStateEnum.Walk
            || state == PlayerStateEnum.Run
            || state == PlayerStateEnum.OnAir
            || state == PlayerStateEnum.Slide
            || state == PlayerStateEnum.Crouch;
    }

    private void UpdateGroundMovementState(Vector2 moveInput, bool sprintHeld)
    {
        if (moveInput.sqrMagnitude <= 0.01f)
        {
            SetState(PlayerStateEnum.Idle);
        }
        else if (enableSprint && sprintHeld)
        {
            SetState(PlayerStateEnum.Run);
        }
        else
        {
            SetState(PlayerStateEnum.Walk);
        }
    }

    private void UpdateSlidePosture(float deltaTime)
    {
        bool sliding = IsSliding;
        bool crouched = IsCrouched;
        float targetHeightMultiplier = sliding ? slideHeightMultiplier : crouched ? crouchHeight : 1f;
        currentPostureHeightMultiplier = Mathf.MoveTowards(currentPostureHeightMultiplier, targetHeightMultiplier, slidePostureTransitionSpeed * deltaTime);
        SetCapsuleHeight(currentPostureHeightMultiplier);

        float targetTilt = sliding ? slideTiltAngle : 0f;
        currentSlideTilt = Mathf.MoveTowards(currentSlideTilt, targetTilt, slideTiltTransitionSpeed * deltaTime);
        if (!sliding && Mathf.Approximately(currentSlideTilt, 0f)
            && !gravityRotationConstraintsReleased && rb.constraints != standingRigidbodyConstraints)
        {
            rb.rotation = transform.rotation;
            rb.constraints = standingRigidbodyConstraints;
        }
    }
    private void SetCapsuleHeight(float heightMultiplier)
    {
        if (playerCapsule == null)
        {
            return;
        }

        float newHeight = Mathf.Max(playerCapsule.radius * 2f, standingCapsuleHeight * heightMultiplier);
        float heightDifference = standingCapsuleHeight - newHeight;
        playerCapsule.height = newHeight;
        playerCapsule.center = standingCapsuleCenter - Vector3.up * (heightDifference * 0.5f);
        jointPosturePosition = jointOriginalPos - Vector3.up * (heightDifference * 0.5f);
        joint.localPosition = jointPosturePosition;
    }

    private bool CanStandUp()
    {
        if (playerCapsule == null)
        {
            return true;
        }

        Vector3 up = transform.up;
        Vector3 worldCenter = transform.TransformPoint(playerCapsule.center);
        Vector3 scale = transform.lossyScale;
        float worldRadius = playerCapsule.radius * Mathf.Max(scale.x, scale.z);
        float currentWorldHeight = playerCapsule.height * scale.y;
        float standingWorldHeight = standingCapsuleHeight * scale.y;
        Vector3 currentTop = worldCenter + up * Mathf.Max(0f, currentWorldHeight * 0.5f - worldRadius);
        float clearance = standingWorldHeight - currentWorldHeight;
        if (clearance <= 0.01f)
        {
            return true;
        }

        Vector3 origin = currentTop + up * 0.02f;
        float probeOffset = worldRadius * 0.5f;
        Vector3 right = transform.right * probeOffset;
        Vector3 forward = transform.forward * probeOffset;
        float probeDistance = clearance + 0.05f;

        return !Physics.Raycast(origin, up, probeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !Physics.Raycast(origin + right, up, probeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !Physics.Raycast(origin - right, up, probeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !Physics.Raycast(origin + forward, up, probeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !Physics.Raycast(origin - forward, up, probeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }
    #region Head Bob
    private void HeadBob()
    {
        PlayerStateEnum state = CurrentState;
        bool moving = state == PlayerStateEnum.Slide
            || ((state == PlayerStateEnum.Walk || state == PlayerStateEnum.Run || state == PlayerStateEnum.Crouch)
                && InputManager.Instance.Move.sqrMagnitude > 0.01f);

        if (moving)
        {
            float sprintProgress = Mathf.InverseLerp(walkSpeed, sprintSpeed, currentMoveSpeed);
            float frequency = bobSpeed * Mathf.Lerp(1f, sprintBobSpeedMultiplier, sprintProgress);
            float amountScale = Mathf.Lerp(1f, sprintBobAmountMultiplier, sprintProgress);

            if (state == PlayerStateEnum.Crouch)
            {
                frequency *= speedReduction;
            }

            timer += Time.deltaTime * frequency;
            Vector3 bobOffset = Mathf.Sin(timer) * bobAmount * amountScale;
            joint.localPosition = jointPosturePosition + bobOffset;
        }
        else
        {
            timer = 0f;
            joint.localPosition = Vector3.Lerp(joint.localPosition, jointPosturePosition, Time.deltaTime * bobSpeed);
        }
    }
    #endregion
}

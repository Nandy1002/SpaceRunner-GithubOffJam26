using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PlayerState))]
public class WallRunAbility : MonoBehaviour, IAbility
{
    [Header("Wall Run")]
    [SerializeField] private float wallRunSpeed = 22f;
    [SerializeField] private float wallCheckDistance = 1.2f;
    [SerializeField, Min(0f)] private float wallSnapSpeed = 6f;
    [SerializeField, Min(0f)] private float wallClearance = 0.05f;
    [SerializeField, Range(0f, 45f)] private float wallRunCameraRoll = 20f;
    [SerializeField] private float wallCheckHeight = 1f;
    [SerializeField] private LayerMask wallLayerMask = ~0;
    [SerializeField] private float wallRunGravityMultiplier = 0.25f;
    [SerializeField, Min(0f)] private float maxWallRunFallSpeed = 1.5f;
    [SerializeField] private float maxWallRunDuration = 2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayerMask = ~0;

    [Header("Wall Jump")]
    [SerializeField, Min(0f)] private float wallJumpUpForce = 5f;
    [SerializeField, Min(0f)] private float wallJumpAwayForce = 4f;
    [SerializeField, Min(0f)] private float wallJumpRegrabCooldown = 0.75f;
    [SerializeField, Min(0f)] private float wallJumpRegrabSurfaceDistance = 0.5f;
    private Rigidbody rb;
    private PlayerState playerState;
    private bool isWallRunning;
    private bool jumpQueued;
    private float wallRunTimer;
    private Vector3 wallNormal;
    private Collider lastWallCollider;
    private Vector3 lastWallPoint;
    private Vector3 lastWallNormal;
    private float wallJumpRegrabTimer;
    private readonly RaycastHit[] wallHitBuffer = new RaycastHit[8];

    private Vector3 GravityUp => Physics.gravity.sqrMagnitude > 0.001f ? -Physics.gravity.normalized : Vector3.up;

    public bool IsActive => isWallRunning;

    public float CameraLeanAngle
    {
        get
        {
            if (!isWallRunning)
            {
                return 0f;
            }

            bool wallIsOnLeft = Vector3.Dot(transform.right, wallNormal) > 0f;
            return wallIsOnLeft ? -wallRunCameraRoll : wallRunCameraRoll;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerState = GetComponent<PlayerState>();
    }

    private void Update()
    {
        // Cache the press until FixedUpdate so this works at any render/physics rate.
        if (InputManager.Instance != null && InputManager.Instance.JumpPressedThisFrame)
        {
            jumpQueued = true;
        }
    }

    private void FixedUpdate()
    {
        if (wallJumpRegrabTimer > 0f)
        {
            wallJumpRegrabTimer = Mathf.Max(0f, wallJumpRegrabTimer - Time.fixedDeltaTime);
            if (wallJumpRegrabTimer <= 0f)
            {
                lastWallCollider = null;
            }
        }

        bool grounded = IsGrounded();
        bool foundWall = FindWall(out Vector3 detectedWallNormal, out float detectedWallDistance,
            out Collider detectedWallCollider, out Vector3 detectedWallPoint);
        bool movingForward = InputManager.Instance != null && InputManager.Instance.Move.y > 0.1f;

        if (isWallRunning)
        {
            if (jumpQueued)
            {
                WallJump();
            }
            else if (grounded || !foundWall || !movingForward ||
                     wallRunTimer >= maxWallRunDuration)
            {
                StopWallRun();
            }
            else
            {
                wallNormal = detectedWallNormal;
                lastWallCollider = detectedWallCollider;
                lastWallPoint = detectedWallPoint;
                lastWallNormal = detectedWallNormal;
                ContinueWallRun(detectedWallDistance);
            }
        }
        else if (playerState.IsInState(PlayerStateEnum.OnAir)
                 && !grounded && foundWall && movingForward)
        {
            StartWallRun(detectedWallNormal, detectedWallCollider, detectedWallPoint);
        }

        // Consume the jump press so it cannot trigger later unexpectedly.
        jumpQueued = false;
    }

    private bool FindWall(out Vector3 normal, out float distance, out Collider wallCollider, out Vector3 point)
    {
        Vector3 origin = transform.position + GravityUp * wallCheckHeight;

        if (TryFindWallInDirection(origin, transform.right, out RaycastHit rightHit)
            || TryFindWallInDirection(origin, -transform.right, out rightHit))
        {
            normal = rightHit.normal;
            distance = rightHit.distance;
            wallCollider = rightHit.collider;
            point = rightHit.point;
            return true;
        }

        normal = Vector3.zero;
        distance = 0f;
        wallCollider = null;
        point = Vector3.zero;
        return false;
    }

    private bool TryFindWallInDirection(Vector3 origin, Vector3 direction, out RaycastHit closestHit)
    {
        int hitCount = Physics.SphereCastNonAlloc(
            origin, 0.15f, direction, wallHitBuffer, wallCheckDistance,
            wallLayerMask, QueryTriggerInteraction.Ignore);

        closestHit = default;
        float closestDistance = float.MaxValue;
        bool foundHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = wallHitBuffer[i];
            bool sameSurfaceAsWallJump = hit.collider == lastWallCollider
                && Vector3.Distance(hit.point, lastWallPoint) <= wallJumpRegrabSurfaceDistance
                && Vector3.Dot(hit.normal, lastWallNormal) > 0.95f;

            if (hit.collider == null
                || hit.transform == transform
                || hit.transform.IsChildOf(transform)
                || (wallJumpRegrabTimer > 0f && sameSurfaceAsWallJump)
                || hit.distance >= closestDistance)
            {
                continue;
            }

            closestHit = hit;
            closestDistance = hit.distance;
            foundHit = true;
        }

        return foundHit;
    }

    private bool IsGrounded()
    {
        Vector3 gravityUp = GravityUp;
        Vector3 checkPosition;
        if (groundCheck != null)
        {
            checkPosition = groundCheck.position;
        }
        else
        {
            // Fall back to the player's capsule so the ability works without a separate
            // ground-check transform assigned in the Inspector.
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                return false;
            }

            checkPosition = transform.position + capsule.center - gravityUp * (capsule.height * 0.5f - groundCheckRadius);
        }

        // A downward ray avoids counting a nearby vertical wall as ground.
        RaycastHit[] hits = Physics.RaycastAll(
            checkPosition + gravityUp * 0.05f, -gravityUp, groundCheckRadius + 0.1f,
            groundLayerMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            bool isPlayerCollider = hitTransform == transform || hitTransform.IsChildOf(transform);
            if (!isPlayerCollider && Vector3.Dot(hits[i].normal, gravityUp) > 0.5f)
            {
                return true;
            }
        }

        return false;
    }

    private void StartWallRun(Vector3 normal, Collider wallCollider, Vector3 point)
    {
        isWallRunning = true;
        wallNormal = normal;
        lastWallCollider = wallCollider;
        lastWallPoint = point;
        lastWallNormal = normal;
        wallRunTimer = 0f;
        playerState.SetCurrentState(PlayerStateEnum.WallRun);
    }

    private void ContinueWallRun(float wallHitDistance)
    {
        wallRunTimer += Time.fixedDeltaTime;

        Vector3 gravityUp = GravityUp;
        Vector3 runDirection =
            Vector3.ProjectOnPlane(transform.forward, wallNormal);
        runDirection = Vector3.ProjectOnPlane(runDirection, gravityUp).normalized;

        if (runDirection.sqrMagnitude > 0f)
        {
            Vector3 velocity = rb.linearVelocity;
            Vector3 verticalVel = Vector3.Project(velocity, gravityUp);
            float fallSpeed = Vector3.Dot(verticalVel, gravityUp);
            if (fallSpeed < -maxWallRunFallSpeed)
            {
                verticalVel = -gravityUp * maxWallRunFallSpeed;
            }
            rb.linearVelocity = runDirection * wallRunSpeed + verticalVel;
        }

        SnapTowardWall(wallHitDistance);

        // Reduce the effect of gravity while wall-running.
        rb.AddForce(
            -Physics.gravity * (1f - wallRunGravityMultiplier),
            ForceMode.Acceleration);
    }

    private void SnapTowardWall(float wallHitDistance)
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        float playerRadius = capsule != null
            ? capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z)
            : 0.5f;
        const float wallCastRadius = 0.15f;
        float distanceFromPlayerToWall = wallHitDistance + wallCastRadius - playerRadius;
        float distanceToClose = distanceFromPlayerToWall - wallClearance;
        if (distanceToClose <= 0f)
        {
            return;
        }

        float targetSnapSpeed = Mathf.Min(wallSnapSpeed, distanceToClose / Time.fixedDeltaTime);
        float currentSnapSpeed = Vector3.Dot(rb.linearVelocity, -wallNormal);
        rb.AddForce(-wallNormal * (targetSnapSpeed - currentSnapSpeed), ForceMode.VelocityChange);
    }

    private void WallJump()
    {
        wallJumpRegrabTimer = wallJumpRegrabCooldown;
        StopWallRun();

        Vector3 gravityUp = GravityUp;
        Vector3 velocity = rb.linearVelocity;
        velocity -= Vector3.Project(velocity, gravityUp);
        rb.linearVelocity = velocity;

        rb.AddForce(
            gravityUp * wallJumpUpForce + wallNormal * wallJumpAwayForce,
            ForceMode.VelocityChange);
    }

    private void StopWallRun()
    {
        isWallRunning = false;
        wallRunTimer = 0f;
        if (playerState.IsInState(PlayerStateEnum.WallRun))
        {
            playerState.SetCurrentState(PlayerStateEnum.OnAir);
        }
    }

    private void OnDisable()
    {
        if (isWallRunning)
        {
            StopWallRun();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * wallCheckHeight;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin + transform.right * wallCheckDistance, 0.15f);
        Gizmos.DrawWireSphere(origin - transform.right * wallCheckDistance, 0.15f);

        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}

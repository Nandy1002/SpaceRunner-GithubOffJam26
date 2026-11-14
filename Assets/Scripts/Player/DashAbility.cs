using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PlayerState))]
public class DashAbility : MonoBehaviour, IAbility
{
    [Header("Dash")]
    [SerializeField, Min(0.01f)] private float dashDuration = 0.2f;
    [SerializeField, Min(0f)] private float dashCooldown = 0.5f;
    [SerializeField, Min(0f)] private float dashDistance = 12f;
    [SerializeField] private LayerMask obstacleLayerMask = ~0;

    private Rigidbody rb;
    private PlayerState playerState;
    private CapsuleCollider playerCapsule;
    private FirstPersonController playerController;
    private bool isDashing;
    private float dashTimeRemaining;
    private float cooldownRemaining;
    private float currentDashSpeed;
    private Vector3 dashDirection;
    private PlayerStateEnum stateBeforeDash;

    public bool IsActive => isDashing;
    public float DashCooldown => dashCooldown;
    public float CooldownRemaining => cooldownRemaining;
    public float CooldownNormalized => dashCooldown > 0f ? Mathf.Clamp01(cooldownRemaining / dashCooldown) : 0f;
    public bool IsReady => cooldownRemaining <= 0f && !isDashing;

    private Vector3 GravityUp => playerController != null
        ? playerController.GetGravityUp()
        : (Physics.gravity.sqrMagnitude > 0.001f ? -Physics.gravity.normalized : Vector3.up);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerState = GetComponent<PlayerState>();
        playerCapsule = GetComponent<CapsuleCollider>();
        playerController = GetComponent<FirstPersonController>();
    }

    private void Update()
    {
        if (InputManager.Instance != null && InputManager.Instance.DashPressedThisFrame)
        {
            TryStartDash();
        }
    }

    private void FixedUpdate()
    {
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.fixedDeltaTime);
        }

        if (!isDashing)
        {
            return;
        }

        dashTimeRemaining -= Time.fixedDeltaTime;
        Vector3 gravityUp = GravityUp;
        float stepDistance = currentDashSpeed * Time.fixedDeltaTime;

        // Obstacle sweep check before advancing to prevent wall penetration / clipping
        if (playerCapsule != null)
        {
            Vector3 center = transform.position + transform.rotation * playerCapsule.center;
            float halfHeight = Mathf.Max(0f, (playerCapsule.height * 0.5f) - playerCapsule.radius);
            Vector3 point1 = center + gravityUp * halfHeight;
            Vector3 point2 = center - gravityUp * halfHeight;
            float castRadius = playerCapsule.radius * 0.9f;

            if (Physics.CapsuleCast(point1, point2, castRadius, dashDirection, out RaycastHit hit,
                    stepDistance + 0.1f, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && hit.transform != transform && !hit.transform.IsChildOf(transform))
                {
                    // Stop dash cleanly at the obstacle contact without penetrating
                    float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);
                    rb.position += dashDirection * safeDistance;
                    EndDash();

                    // Strip any forward velocity into the wall
                    Vector3 v = rb.linearVelocity;
                    float velIntoWall = Vector3.Dot(v, dashDirection);
                    if (velIntoWall > 0f)
                    {
                        v -= dashDirection * velIntoWall;
                    }
                    rb.linearVelocity = v;
                    return;
                }
            }
        }

        // Clamp vertical velocity along gravity up so depenetration impulses cannot build up into a super jump
        Vector3 verticalVel = Vector3.Project(rb.linearVelocity, gravityUp);
        if (Vector3.Dot(verticalVel, gravityUp) > 0f)
        {
            verticalVel = Vector3.zero;
        }

        rb.linearVelocity = dashDirection * currentDashSpeed + verticalVel;

        if (dashTimeRemaining <= 0f)
        {
            EndDash();
        }
    }

    private void TryStartDash()
    {
        if (isDashing || cooldownRemaining > 0f || !CanDashFromState(playerState.GetCurrentState()))
        {
            return;
        }

        Vector3 gravityUp = GravityUp;
        Vector2 moveInput = InputManager.Instance != null ? InputManager.Instance.Move : Vector2.zero;
        Vector3 localDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        dashDirection = localDirection.sqrMagnitude > 0.01f
            ? transform.TransformDirection(localDirection).normalized
            : transform.forward;
        dashDirection = Vector3.ProjectOnPlane(dashDirection, gravityUp).normalized;

        if (dashDirection.sqrMagnitude < 0.001f)
        {
            dashDirection = Vector3.ProjectOnPlane(transform.forward, gravityUp).normalized;
        }

        currentDashSpeed = dashDistance / dashDuration;

        stateBeforeDash = playerState.GetCurrentState();
        isDashing = true;
        dashTimeRemaining = dashDuration;
        cooldownRemaining = dashCooldown;
        playerState.SetCurrentState(PlayerStateEnum.Dashing);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isDashing)
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (Vector3.Dot(contact.normal, dashDirection) < -0.3f)
            {
                EndDash();
                Vector3 v = rb.linearVelocity;
                float forwardVel = Vector3.Dot(v, dashDirection);
                if (forwardVel > 0f)
                {
                    v -= dashDirection * forwardVel;
                }
                rb.linearVelocity = v;
                return;
            }
        }
    }

    private bool CanDashFromState(PlayerStateEnum state)
    {
        return state == PlayerStateEnum.Idle
            || state == PlayerStateEnum.Walk
            || state == PlayerStateEnum.Run
            || state == PlayerStateEnum.OnAir
            || state == PlayerStateEnum.Crouch;
    }

    private void EndDash()
    {
        isDashing = false;
        dashTimeRemaining = 0f;

        // Ensure vertical velocity along gravity up is clamped so player does not launch into sky
        Vector3 gravityUp = GravityUp;
        Vector3 vert = Vector3.Project(rb.linearVelocity, gravityUp);
        if (Vector3.Dot(vert, gravityUp) > 3f)
        {
            rb.linearVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, gravityUp) + gravityUp * 3f;
        }

        if (playerState != null && playerState.IsInState(PlayerStateEnum.Dashing))
        {
            playerState.SetCurrentState(stateBeforeDash);
        }
    }

    private void OnDisable()
    {
        if (isDashing)
        {
            EndDash();
        }
    }
}
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
public class MovementWip2 : MonoBehaviour
{
    private const float MinGroundNormalY = 0.6f;

    [Header("Input")]
    public bool useNewInputSystem = false;

    [Header("References")]
    public Transform cameraTransform;
    public Transform groundProbe;
    public GrappleHookSystem grappleHookSystem;

    [Header("Movement")]
    public float maxMoveSpeed = 8f;
    public float groundAcceleration = 35f;
    public float airAcceleration = 12f;
    public float jumpImpulse = 8.5f;
    public float airControlPercent = 0.6f;
    public float turnSpeed = 720f;

    [Header("Jump")]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    public float probeRadius = 0.28f;
    public float probeDistance = 0.55f;
    public LayerMask groundLayers = ~0;

    [Header("Platform")]
    [Range(0f, 1f)] public float platformVelocityCarryOnJump = 0.7f;

    private Rigidbody rb;

    private Vector2 moveInput;
    private bool grappleHeld;
    private bool grapplePressedThisFrame;
    private bool grappleReleasedThisFrame;
    private float jumpBufferCounter;
    private float coyoteCounter;
    private Vector3 desiredFacingDirection;

    private bool isGrounded;
    private Rigidbody groundedBody;
    private Vector3 groundedBodyVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (groundProbe == null)
        {
            groundProbe = transform;
        }

        if (grappleHookSystem == null)
        {
            grappleHookSystem = GetComponent<GrappleHookSystem>();
        }

        if (grappleHookSystem != null && grappleHookSystem.cameraTransform == null)
        {
            grappleHookSystem.cameraTransform = cameraTransform;
        }
    }

    private void Update()
    {
        ReadInput();
        HandleGrappleInput();
    }

    private void FixedUpdate()
    {
        UpdateGroundState();
        UpdateJumpTimers();
        HandleMovement();
        HandleFacing();
        HandleJump();
    }

    private void ReadInput()
    {
        grapplePressedThisFrame = false;
        grappleReleasedThisFrame = false;

        if (useNewInputSystem)
        {
#if ENABLE_INPUT_SYSTEM
            moveInput = ReadMoveInputNewSystem();

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpBufferCounter = jumpBufferTime;
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    grapplePressedThisFrame = true;
                }

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    grappleReleasedThisFrame = true;
                }

                grappleHeld = Mouse.current.leftButton.isPressed;
            }
#else
            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
            return;
        }

        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }

        if (Input.GetMouseButtonDown(0))
        {
            grapplePressedThisFrame = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            grappleReleasedThisFrame = true;
        }

        grappleHeld = Input.GetMouseButton(0);
    }

    private void HandleGrappleInput()
    {
        if (grappleHookSystem == null)
        {
            return;
        }

        if (grapplePressedThisFrame)
        {
            grappleHookSystem.TryStartGrapple();
        }

        if (grappleReleasedThisFrame)
        {
            grappleHookSystem.StopGrapple(true);
        }

        grappleHookSystem.SetGrappleHeld(grappleHeld);
    }

    private void UpdateGroundState()
    {
        RaycastHit hit;
        Vector3 origin = groundProbe.position;

        bool foundGround = Physics.SphereCast(
            origin,
            probeRadius,
            Vector3.down,
            out hit,
            probeDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore);

        isGrounded = foundGround && hit.normal.y >= MinGroundNormalY;
        groundedBody = isGrounded ? hit.rigidbody : null;
        groundedBodyVelocity = groundedBody != null ? groundedBody.GetPointVelocity(hit.point) : Vector3.zero;
    }

    private void HandleMovement()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 desiredDir = camRight * moveInput.x + camForward * moveInput.y;
        if (desiredDir.sqrMagnitude > 1f)
        {
            desiredDir.Normalize();
        }

        if (desiredDir.sqrMagnitude > 0.0001f)
        {
            desiredFacingDirection = desiredDir;
        }

        Vector3 desiredWorldVelocity = desiredDir * maxMoveSpeed;
        Vector3 targetBaseVelocity = desiredWorldVelocity + groundedBodyVelocity;

        Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 targetHorizontal = new Vector3(targetBaseVelocity.x, 0f, targetBaseVelocity.z);
        Vector3 horizontalDelta = targetHorizontal - currentHorizontal;

        float accel = isGrounded ? groundAcceleration : airAcceleration * airControlPercent;
        Vector3 accelStep = Vector3.ClampMagnitude(horizontalDelta / Time.fixedDeltaTime, accel);

        rb.AddForce(accelStep, ForceMode.Acceleration);
    }

    private void HandleFacing()
    {
        if (desiredFacingDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(desiredFacingDirection, Vector3.up);
        Quaternion nextRotation = Quaternion.RotateTowards(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(nextRotation);
    }

    private void UpdateJumpTimers()
    {
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter = Mathf.Max(0f, coyoteCounter - Time.fixedDeltaTime);
        }

        jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.fixedDeltaTime);
    }

    private void HandleJump()
    {
        if (jumpBufferCounter <= 0f || coyoteCounter <= 0f)
        {
            return;
        }

        jumpBufferCounter = 0f;
        coyoteCounter = 0f;

        float newVelY = Mathf.Max(rb.linearVelocity.y, 0f);

        // Carry part of platform velocity into jump to keep platform movement believable.
        Vector3 carry = groundedBodyVelocity * platformVelocityCarryOnJump;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x + carry.x, newVelY, rb.linearVelocity.z + carry.z);
        rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
    }

#if ENABLE_INPUT_SYSTEM
    private static Vector2 ReadMoveInputNewSystem()
    {
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) y -= 1f;
        if (Keyboard.current.wKey.isPressed) y += 1f;

        Vector2 result = new Vector2(x, y);
        return result.sqrMagnitude > 1f ? result.normalized : result;
    }
#endif
}

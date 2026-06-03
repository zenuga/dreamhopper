using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovementWip : MonoBehaviour
{
    private const float MinHorizontalBoostVelocitySqrMagnitude = 0.01f;
    private const float MinBoostDirectionSqrMagnitude = 0.001f;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float acceleration = 50f;
    public float airAcceleration = 20f;
    public float jumpForce = 15f;
    public float maxSpeed = 20f;
    public float inputDeadzone = 0.15f;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Platform Settings")]
    public LayerMask platformLayer;

    [Header("References")]
    public Grapplehook grappleHook;
    public GravityModifier gravityModifier;

    [Header("Special Mode")]
    public float walkOnPulledObjectDistance = 1.5f;
    private bool specialModeActive = false;

    private bool jumpRequest;
    private Rigidbody rb;
    private bool isGrounded;

    public bool canUseGrapplingHook
    {
        get => grappleHook != null && grappleHook.canUseGrapplingHook;
        set
        {
            if (grappleHook != null)
            {
                grappleHook.canUseGrapplingHook = value;
            }
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        if (grappleHook == null)
        {
            grappleHook = GetComponent<Grapplehook>();
        }
    }

    void Update()
    {
        if (grappleHook != null)
        {
            grappleHook.HandleUpdate();
        }

        if (Input.GetButtonDown("Jump"))
        {
            jumpRequest = true;
        }
    }

    void FixedUpdate()
    {
        CheckGrounded();

        if (grappleHook != null)
        {
            grappleHook.HandleFixedUpdate();
        }

        HandleMovement();

        if (grappleHook != null)
        {
            grappleHook.ResetSkipStopThisFixedUpdate();
        }
    }

    void HandleMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(horizontalInput) < inputDeadzone) horizontalInput = 0f;
        if (Mathf.Abs(verticalInput) < inputDeadzone) verticalInput = 0f;

        Vector3 gravityDown = Vector3.down;

        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, gravityDown).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, gravityDown).normalized;

        if (cameraForward.sqrMagnitude < 0.001f)
            cameraForward = Vector3.ProjectOnPlane(transform.forward, gravityDown).normalized;
        if (cameraRight.sqrMagnitude < 0.001f)
            cameraRight = Vector3.ProjectOnPlane(transform.right, gravityDown).normalized;

        Vector3 moveDirection = (cameraRight * horizontalInput + cameraForward * verticalInput).normalized;

        Vector3 currentHorizontalVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
        Vector3 targetVelocity = moveDirection * moveSpeed;

        bool isGrappling = grappleHook != null && grappleHook.IsGrappling;
        bool skipStop = grappleHook != null && grappleHook.ShouldSkipStopThisFixedUpdate;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            float accelRate = isGrounded ? acceleration : airAcceleration;
            Vector3 newHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetVelocity, accelRate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(newHorizontalVelocity.x, rb.linearVelocity.y, newHorizontalVelocity.z);
        }
        else if (isGrounded && !isGrappling && !skipStop)
        {
            Vector3 slowedVelocity = Vector3.MoveTowards(currentHorizontalVelocity, Vector3.zero, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(slowedVelocity.x, rb.linearVelocity.y, slowedVelocity.z);
        }

        if (new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude > maxSpeed * 2f)
        {
            Vector3 clamped = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).normalized * (maxSpeed * 2f);
            rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
        }

        if (isGrounded && jumpRequest)
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            jumpRequest = false;
        }

        jumpRequest = false;
    }

    void CheckGrounded()
    {
        Vector3 down = Vector3.down;
        isGrounded = Physics.Raycast(transform.position, down, 1.1f, platformLayer);

        // Special mode: Check if we can walk on the pulled-to object
        if (!isGrounded && gravityModifier != null)
        {
            specialModeActive = CheckSpecialModeGround();
            if (specialModeActive)
            {
                isGrounded = true;
            }
        }
        else
        {
            specialModeActive = false;
        }
    }

    bool CheckSpecialModeGround()
    {
        // Get the direction of gravity pull
        Vector3 gravityDir = gravityModifier.GravityDirection;
        if (gravityDir.sqrMagnitude < Mathf.Epsilon)
            return false;

        // Cast a ray in the direction of gravity to check if we're on the pulled-to object
        // This allows walking on the surface of the object we're being pulled to
        if (Physics.Raycast(transform.position, gravityDir, out RaycastHit hit, walkOnPulledObjectDistance, platformLayer))
        {
            // Check if the hit object has a GravityBody component (it's a pull target)
            if (hit.collider.GetComponent<GravityBody>() != null)
            {
                return true;
            }
        }

        return false;
    }
}

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

    private bool jumpRequest;
    private Rigidbody rb;
    private GravityModifier gravityModifier;
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
        gravityModifier = GetComponent<GravityModifier>();
        rb.useGravity = gravityModifier == null;

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
        if (gravityModifier != null && gravityModifier.GravityDirection.sqrMagnitude > Mathf.Epsilon)
        {
            gravityDown = gravityModifier.GravityDirection;
        }

        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, gravityDown).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, gravityDown).normalized;

        if (cameraForward.sqrMagnitude < 0.001f)
            cameraForward = Vector3.ProjectOnPlane(transform.forward, gravityDown).normalized;
        if (cameraRight.sqrMagnitude < 0.001f)
            cameraRight = Vector3.ProjectOnPlane(transform.right, gravityDown).normalized;

        Vector3 moveDirection = (cameraRight * horizontalInput + cameraForward * verticalInput).normalized;

        Vector3 gravityVelocity = Vector3.Project(rb.linearVelocity, gravityDown);
        Vector3 currentHorizontalVelocity = rb.linearVelocity - gravityVelocity;
        Vector3 targetVelocity = moveDirection * moveSpeed;

        bool isGrappling = grappleHook != null && grappleHook.IsGrappling;
        bool skipStop = grappleHook != null && grappleHook.ShouldSkipStopThisFixedUpdate;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            float accelRate = isGrounded ? acceleration : airAcceleration;
            Vector3 newHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetVelocity, accelRate * Time.fixedDeltaTime);
            rb.linearVelocity = newHorizontalVelocity + gravityVelocity;
        }
        else if (isGrounded && !isGrappling && !skipStop)
        {
            Vector3 slowedVelocity = Vector3.MoveTowards(currentHorizontalVelocity, Vector3.zero, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = slowedVelocity + gravityVelocity;
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
        Vector3 down = gravityModifier != null && gravityModifier.GravityDirection.sqrMagnitude > Mathf.Epsilon
            ? gravityModifier.GravityDirection
            : Vector3.down;

        isGrounded = Physics.Raycast(transform.position, down, 1.1f, platformLayer);
    }
}

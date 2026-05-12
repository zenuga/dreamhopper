using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AdvancedPlayerMovement3D : MonoBehaviour
{
    private const float MinHorizontalBoostVelocitySqrMagnitude = 0.01f;
    private const float MinBoostDirectionSqrMagnitude = 0.001f;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float jumpForce = 15f;
    public float maxSpeed = 20f;
    public float inputDeadzone = 0.15f;

    private bool jumpRequest;

    [Header("Grappling Hook Settings")]
    public float grappleReleaseBoost = 2f;
    public float grappleReleaseBoostDuration = 0.15f;
    public bool canUseGrapplingHook = true;
    public GameObject grapplingHookPrefab;
    public float hookSpeed = 20f;
    public float maxGrappleDistance = 20f;
    public float grapplePullSpeed = 2f; // How fast the rope shortens
    public LayerMask hookableLayers;

    [Header("Camera")]
    public Transform cameraTransform; // Reference to the camera

    [Header("Platform Settings")]
    public LayerMask platformLayer;

    private Rigidbody rb;
    private bool isGrounded;
    private bool isGrappling;
    private bool skipStopThisFixedUpdate;
    private Vector3 pendingReleaseBoost;
    private Vector3 grapplePoint;
    private GameObject currentHook;
    private SpringJoint grappleJoint;
    private float currentGrappleLength;
    private LineRenderer lineRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
        }
    }

    void Update()
    {
        HandleInput();
        UpdateLineRenderer();
        if (isGrappling)
        {
            ShortenGrapple();
        }

        if (Input.GetButtonDown("Jump"))
        {
            jumpRequest = true;
        }
    }

    void FixedUpdate()
    {
        CheckGrounded();
        HandleMovement();
    }

    void HandleInput()
    {
        if (canUseGrapplingHook && Input.GetButtonDown("Fire1") && !isGrappling)
        {
            ShootGrapplingHook();
        }
        if (Input.GetButtonUp("Fire1") && isGrappling)
        {
            ReleaseGrapple();
        }
    }

    void HandleMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(horizontalInput) < inputDeadzone)
        {
            horizontalInput = 0f;
        }
        if (Mathf.Abs(verticalInput) < inputDeadzone)
        {
            verticalInput = 0f;
        }

        // Get camera forward direction, project to horizontal plane
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        // Calculate movement direction relative to camera
        Vector3 moveDirection = cameraRight * horizontalInput + cameraForward * verticalInput;

        bool hasMovementInput = moveDirection.sqrMagnitude > 0.001f;
        if (hasMovementInput)
        {
            moveDirection.Normalize();
            Vector3 horizontalVelocity = moveDirection * moveSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
        else if (!isGrappling && !skipStopThisFixedUpdate)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        if (pendingReleaseBoost.sqrMagnitude > 0.0001f)
        {
            float boostStepScale = Time.fixedDeltaTime / Mathf.Max(grappleReleaseBoostDuration, 0.001f);
            Vector3 boostStep = pendingReleaseBoost * boostStepScale;
            rb.linearVelocity += new Vector3(boostStep.x, 0f, boostStep.z);
            pendingReleaseBoost -= boostStep;
            if (pendingReleaseBoost.sqrMagnitude < 0.0001f)
            {
                pendingReleaseBoost = Vector3.zero;
            }
        }

        // Clamp max speed horizontally
        Vector3 clampedHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (clampedHorizontal.magnitude > maxSpeed)
        {
            clampedHorizontal = clampedHorizontal.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(clampedHorizontal.x, rb.linearVelocity.y, clampedHorizontal.z);
        }

        // Jumping only in FixedUpdate, using stored jump request for consistency
        if (isGrounded && jumpRequest)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequest = false;
        }
        else if (!isGrounded)
        {
            jumpRequest = false;
        }

        skipStopThisFixedUpdate = false;
    }

    void ShootGrapplingHook()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxGrappleDistance, hookableLayers))
        {
            grapplePoint = hit.point;
            isGrappling = true;
            currentGrappleLength = Vector3.Distance(transform.position, grapplePoint);

            // Instantiate hook
            if (grapplingHookPrefab != null)
            {
                currentHook = Instantiate(grapplingHookPrefab, grapplePoint, Quaternion.identity);
            }

            // Create spring joint for physics-based grapple
            grappleJoint = gameObject.AddComponent<SpringJoint>();
            grappleJoint.connectedBody = null; // Connect to world point
            grappleJoint.connectedAnchor = grapplePoint;
            grappleJoint.spring = 100f; // High spring for tight rope
            grappleJoint.damper = 10f;
            grappleJoint.maxDistance = currentGrappleLength;
            grappleJoint.minDistance = 0f;
            grappleJoint.autoConfigureConnectedAnchor = false;
        }
    }

    void ShortenGrapple()
    {
        if (grappleJoint != null)
        {
            currentGrappleLength -= grapplePullSpeed * Time.deltaTime;
            currentGrappleLength = Mathf.Max(currentGrappleLength, 1f); // Minimum length
            grappleJoint.maxDistance = currentGrappleLength;
        }
    }

    void ReleaseGrapple()
    {
        Vector3 grappleReleaseDirection = transform.position - grapplePoint;
        grappleReleaseDirection.y = 0f;
        if (grappleReleaseDirection.sqrMagnitude > MinBoostDirectionSqrMagnitude)
        {
            grappleReleaseDirection.Normalize();
        }

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 boostDirection = horizontalVelocity.sqrMagnitude > MinHorizontalBoostVelocitySqrMagnitude ? horizontalVelocity.normalized : grappleReleaseDirection;
        if (boostDirection.sqrMagnitude > MinBoostDirectionSqrMagnitude)
        {
            pendingReleaseBoost = boostDirection * grappleReleaseBoost;
            skipStopThisFixedUpdate = true;
        }

        isGrappling = false;

        if (grappleJoint != null)
        {
            Destroy(grappleJoint);
            grappleJoint = null;
        }
        if (currentHook != null)
        {
            Destroy(currentHook);
            currentHook = null;
        }
    }

    void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f, platformLayer);
    }

    void UpdateLineRenderer()
    {
        if (isGrappling && currentHook != null)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, grapplePoint);
            lineRenderer.enabled = true;
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (platformLayer == (platformLayer | (1 << collision.gameObject.layer)))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (platformLayer == (platformLayer | (1 << collision.gameObject.layer)))
        {
            isGrounded = false;
        }
    }
}

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovementWip : MonoBehaviour
{
    private const float MinHorizontalBoostVelocitySqrMagnitude = 0.01f;
    private const float MinBoostDirectionSqrMagnitude = 0.001f;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float acceleration = 50f; // How fast we reach moveSpeed
    public float airAcceleration = 20f; // Lower control in the air
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
    public float grapplePullSpeed = 2f; 
    public LayerMask hookableLayers;

    [Header("Camera")]
    public Transform cameraTransform; 

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
        rb.useGravity = true; // Ensure gravity is on
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

        if (Mathf.Abs(horizontalInput) < inputDeadzone) horizontalInput = 0f;
        if (Mathf.Abs(verticalInput) < inputDeadzone) verticalInput = 0f;

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraRight * horizontalInput + cameraForward * verticalInput).normalized;

        // --- NEW PHYSICS-FRIENDLY MOVEMENT ---
        
        // 1. Get current horizontal velocity
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        // 2. Calculate target velocity based on input
        Vector3 targetVelocity = moveDirection * moveSpeed;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            // If we are grounded, move normally. 
            // If we are in the air, use lower acceleration so we don't instantly override the rocket boost.
            float accelRate = isGrounded ? acceleration : airAcceleration;
            
            // Move current velocity towards target velocity
            Vector3 newHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetVelocity, accelRate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(newHorizontalVelocity.x, rb.linearVelocity.y, newHorizontalVelocity.z);
        }
        else if (isGrounded && !isGrappling && !skipStopThisFixedUpdate)
        {
            // Only stop the player completely if they are on the ground and not touching keys
            Vector3 slowedVelocity = Vector3.MoveTowards(currentHorizontalVelocity, Vector3.zero, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(slowedVelocity.x, rb.linearVelocity.y, slowedVelocity.z);
        }

        // --- GRAPPLE BOOST LOGIC ---
        if (pendingReleaseBoost.sqrMagnitude > 0.0001f)
        {
            float boostStepScale = Time.fixedDeltaTime / Mathf.Max(grappleReleaseBoostDuration, 0.001f);
            Vector3 boostStep = pendingReleaseBoost * boostStepScale;
            rb.linearVelocity += new Vector3(boostStep.x, 0f, boostStep.z);
            pendingReleaseBoost -= boostStep;
        }

        // Clamp max speed (Allowing for slight overshoot from explosions)
        if (new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude > maxSpeed * 2f) 
        {
            Vector3 clamped = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).normalized * (maxSpeed * 2f);
            rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
        }

        // Jumping
        if (isGrounded && jumpRequest)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequest = false;
        }
        
        jumpRequest = false;
        skipStopThisFixedUpdate = false;
    }

    // ... (Rest of your Grapple and Collision methods remain the same)
    
    void ShootGrapplingHook()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxGrappleDistance, hookableLayers))
        {
            grapplePoint = hit.point;
            isGrappling = true;
            currentGrappleLength = Vector3.Distance(transform.position, grapplePoint);

            if (grapplingHookPrefab != null)
            {
                currentHook = Instantiate(grapplingHookPrefab, grapplePoint, Quaternion.identity);
            }

            grappleJoint = gameObject.AddComponent<SpringJoint>();
            grappleJoint.connectedBody = null; 
            grappleJoint.connectedAnchor = grapplePoint;
            grappleJoint.spring = 100f; 
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
            currentGrappleLength = Mathf.Max(currentGrappleLength, 1f); 
            grappleJoint.maxDistance = currentGrappleLength;
        }
    }

    void ReleaseGrapple()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        pendingReleaseBoost = (horizontalVelocity.normalized) * grappleReleaseBoost;
        
        isGrappling = false;
        skipStopThisFixedUpdate = true;

        if (grappleJoint != null) Destroy(grappleJoint);
        if (currentHook != null) Destroy(currentHook);
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
}
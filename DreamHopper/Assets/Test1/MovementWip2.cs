using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

[RequireComponent(typeof(Rigidbody))]
public class MovementWip2 : MonoBehaviour
{
    private const float MinGroundNormalY = 0.6f;

    [Header("Input")]
    public bool useNewInputSystem = false;
    public KeyCode toolSwapKey = KeyCode.Q;

    [Header("References")]
    public Transform cameraTransform;
    public Transform groundProbe;
    public Transform grappleMarker;
    public GameObject grapplingHookVisualPrefab;
    public GrenadeLauncher grenadeLauncher;
    public bool synchronizeGrenadeLauncher = true;

    [Header("Movement")]
    public float maxMoveSpeed = 8f;
    public float groundAcceleration = 35f;
    public float airAcceleration = 12f;
    public float jumpImpulse = 8.5f;
    public float airControlPercent = 0.6f;

    [Header("Ground Check")]
    public float probeRadius = 0.28f;
    public float probeDistance = 0.55f;
    public LayerMask groundLayers = ~0;

    [Header("Platform")]
    [Range(0f, 1f)] public float platformVelocityCarryOnJump = 0.7f;

    [Header("Grapple")]
    public bool grapplingHookUnlocked = false;
    public bool grapplingHookActive = false;
    public LayerMask hookableLayers;
    public float maxGrappleDistance = 35f;
    public float grappleSpring = 55f;
    public float grappleDamper = 7f;
    public float minRopeLength = 2f;
    public float reelSpeed = 10f;
    public float releaseBoostImpulse = 4f;
    public float releaseBoostCooldown = 0.2f;

    private Rigidbody rb;
    private SpringJoint grappleJoint;
    private LineRenderer rope;

    private Vector2 moveInput;
    private bool jumpRequested;
    private bool grappleHeld;

    private bool isGrounded;
    private Rigidbody groundedBody;
    private Vector3 groundedBodyVelocity;

    private bool hasCachedGrapplePoint;
    private Vector3 cachedGrapplePoint;

    private bool isGrappling;
    private float boostReadyAt;
    private float currentRopeLength;
    private GameObject hookVisualInstance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        rope = GetComponent<LineRenderer>();
        if (rope == null)
        {
            rope = gameObject.AddComponent<LineRenderer>();
        }

        rope.positionCount = 2;
        rope.startWidth = 0.04f;
        rope.endWidth = 0.03f;
        rope.enabled = false;

        if (groundProbe == null)
        {
            groundProbe = transform;
        }

        SyncToolStates();
    }

    private void Update()
    {
        ReadInput();
        UpdateGrappleAimCache();
        UpdateMarker();
        UpdateRopeVisual();
    }

    private void FixedUpdate()
    {
        UpdateGroundState();
        HandleMovement();
        HandleJump();
        HandleGrapplePhysics();
    }

    private void ReadInput()
    {
        if (useNewInputSystem)
        {
#if ENABLE_INPUT_SYSTEM
            moveInput = ReadMoveInputNewSystem();

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpRequested = true;
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    TryStartGrapple();
                }

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    StopGrapple(true);
                }

                grappleHeld = Mouse.current.leftButton.isPressed;
            }

            if (WasToolSwapPressedNewSystem())
            {
                ToggleTool();
            }
#else
            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
            return;
        }

        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryStartGrapple();
        }

        if (Input.GetMouseButtonUp(0))
        {
            StopGrapple(true);
        }

        grappleHeld = Input.GetMouseButton(0);

        if (Input.GetKeyDown(toolSwapKey))
        {
            ToggleTool();
        }
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

        Vector3 desiredWorldVelocity = desiredDir * maxMoveSpeed;
        Vector3 targetBaseVelocity = desiredWorldVelocity + groundedBodyVelocity;

        Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 targetHorizontal = new Vector3(targetBaseVelocity.x, 0f, targetBaseVelocity.z);
        Vector3 horizontalDelta = targetHorizontal - currentHorizontal;

        float accel = isGrounded ? groundAcceleration : airAcceleration * airControlPercent;
        Vector3 accelStep = Vector3.ClampMagnitude(horizontalDelta / Time.fixedDeltaTime, accel);

        rb.AddForce(accelStep, ForceMode.Acceleration);
    }

    private void HandleJump()
    {
        if (!jumpRequested)
        {
            return;
        }

        jumpRequested = false;

        if (!isGrounded)
        {
            return;
        }

        float newVelY = Mathf.Max(rb.linearVelocity.y, 0f);

        // Carry part of platform velocity into jump to keep platform movement believable.
        Vector3 carry = groundedBodyVelocity * platformVelocityCarryOnJump;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x + carry.x, newVelY, rb.linearVelocity.z + carry.z);
        rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
    }

    private void HandleGrapplePhysics()
    {
        if (!isGrappling || grappleJoint == null)
        {
            return;
        }

        if (grappleHeld)
        {
            currentRopeLength = Mathf.Max(minRopeLength, currentRopeLength - reelSpeed * Time.fixedDeltaTime);
            grappleJoint.maxDistance = currentRopeLength;
        }
    }

    private void UpdateGrappleAimCache()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Ray aimRay = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(aimRay, out hit, maxGrappleDistance, hookableLayers, QueryTriggerInteraction.Ignore))
        {
            cachedGrapplePoint = hit.point;
            hasCachedGrapplePoint = true;
        }
    }

    private void UpdateMarker()
    {
        if (grappleMarker == null)
        {
            return;
        }

        bool markerVisible = grapplingHookUnlocked && grapplingHookActive && hasCachedGrapplePoint;
        grappleMarker.gameObject.SetActive(markerVisible);

        if (markerVisible)
        {
            grappleMarker.position = cachedGrapplePoint;
        }
    }

    private void UpdateRopeVisual()
    {
        if (!isGrappling || !hasCachedGrapplePoint)
        {
            rope.enabled = false;
            return;
        }

        rope.enabled = true;
        rope.SetPosition(0, transform.position);
        rope.SetPosition(1, cachedGrapplePoint);
    }

    private void TryStartGrapple()
    {
        if (!grapplingHookUnlocked || !grapplingHookActive || !hasCachedGrapplePoint || isGrappling)
        {
            return;
        }

        isGrappling = true;
        currentRopeLength = Vector3.Distance(transform.position, cachedGrapplePoint);

        grappleJoint = gameObject.AddComponent<SpringJoint>();
        grappleJoint.autoConfigureConnectedAnchor = false;
        grappleJoint.connectedAnchor = cachedGrapplePoint;
        grappleJoint.maxDistance = currentRopeLength;
        grappleJoint.minDistance = minRopeLength;
        grappleJoint.spring = grappleSpring;
        grappleJoint.damper = grappleDamper;
        grappleJoint.massScale = 1f;

        if (grapplingHookVisualPrefab != null)
        {
            hookVisualInstance = Instantiate(grapplingHookVisualPrefab, cachedGrapplePoint, Quaternion.identity);
        }
    }

    private void StopGrapple(bool applyBoost)
    {
        if (!isGrappling)
        {
            return;
        }

        isGrappling = false;

        if (grappleJoint != null)
        {
            Destroy(grappleJoint);
            grappleJoint = null;
        }

        if (hookVisualInstance != null)
        {
            Destroy(hookVisualInstance);
            hookVisualInstance = null;
        }

        if (!applyBoost || Time.time < boostReadyAt)
        {
            return;
        }

        // Boost along horizontal swing momentum, blended with facing to keep it fun and controllable.
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 fallback = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        Vector3 boostDir = horizontalVel.sqrMagnitude > 0.01f
            ? horizontalVel.normalized
            : fallback;

        rb.AddForce(boostDir * releaseBoostImpulse, ForceMode.Impulse);
        boostReadyAt = Time.time + releaseBoostCooldown;
    }

    private void ToggleTool()
    {
        if (!grapplingHookUnlocked)
        {
            return;
        }

        grapplingHookActive = !grapplingHookActive;

        if (!grapplingHookActive)
        {
            StopGrapple(false);
        }

        SyncToolStates();
    }

    private void SyncToolStates()
    {
        if (!synchronizeGrenadeLauncher || grenadeLauncher == null)
        {
            return;
        }

        if (grenadeLauncher.grenadelauncherunlocked)
        {
            grenadeLauncher.grenadelauncheractive = !grapplingHookActive;
        }
        else
        {
            grenadeLauncher.grenadelauncheractive = false;
        }
    }

#if ENABLE_INPUT_SYSTEM
    private bool WasToolSwapPressedNewSystem()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        KeyControl key = ResolveKeyControl(Keyboard.current, toolSwapKey);
        return key != null && key.wasPressedThisFrame;
    }

    private static KeyControl ResolveKeyControl(Keyboard keyboard, KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.A: return keyboard.aKey;
            case KeyCode.B: return keyboard.bKey;
            case KeyCode.C: return keyboard.cKey;
            case KeyCode.D: return keyboard.dKey;
            case KeyCode.E: return keyboard.eKey;
            case KeyCode.F: return keyboard.fKey;
            case KeyCode.G: return keyboard.gKey;
            case KeyCode.H: return keyboard.hKey;
            case KeyCode.I: return keyboard.iKey;
            case KeyCode.J: return keyboard.jKey;
            case KeyCode.K: return keyboard.kKey;
            case KeyCode.L: return keyboard.lKey;
            case KeyCode.M: return keyboard.mKey;
            case KeyCode.N: return keyboard.nKey;
            case KeyCode.O: return keyboard.oKey;
            case KeyCode.P: return keyboard.pKey;
            case KeyCode.Q: return keyboard.qKey;
            case KeyCode.R: return keyboard.rKey;
            case KeyCode.S: return keyboard.sKey;
            case KeyCode.T: return keyboard.tKey;
            case KeyCode.U: return keyboard.uKey;
            case KeyCode.V: return keyboard.vKey;
            case KeyCode.W: return keyboard.wKey;
            case KeyCode.X: return keyboard.xKey;
            case KeyCode.Y: return keyboard.yKey;
            case KeyCode.Z: return keyboard.zKey;
            case KeyCode.Alpha0: return keyboard.digit0Key;
            case KeyCode.Alpha1: return keyboard.digit1Key;
            case KeyCode.Alpha2: return keyboard.digit2Key;
            case KeyCode.Alpha3: return keyboard.digit3Key;
            case KeyCode.Alpha4: return keyboard.digit4Key;
            case KeyCode.Alpha5: return keyboard.digit5Key;
            case KeyCode.Alpha6: return keyboard.digit6Key;
            case KeyCode.Alpha7: return keyboard.digit7Key;
            case KeyCode.Alpha8: return keyboard.digit8Key;
            case KeyCode.Alpha9: return keyboard.digit9Key;
            case KeyCode.Space: return keyboard.spaceKey;
            case KeyCode.Tab: return keyboard.tabKey;
            case KeyCode.LeftShift: return keyboard.leftShiftKey;
            case KeyCode.RightShift: return keyboard.rightShiftKey;
            case KeyCode.LeftControl: return keyboard.leftCtrlKey;
            case KeyCode.RightControl: return keyboard.rightCtrlKey;
            case KeyCode.Escape: return keyboard.escapeKey;
            default: return null;
        }
    }

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

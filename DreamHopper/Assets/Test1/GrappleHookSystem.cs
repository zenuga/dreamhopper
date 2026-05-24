using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GrappleHookSystem : MonoBehaviour
{
    [Header("Grapple Toggle")]
    public bool grapplingEnabled = false;

    [Header("References")]
    public Transform cameraTransform;
    public Transform grappleMarker;
    public GameObject grapplingHookVisualPrefab;

    [Header("Grapple")]
    public LayerMask hookableLayers = ~0;
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

    private bool grappleHeld;
    private bool hasCachedGrapplePoint;
    private Vector3 cachedGrapplePoint;

    private bool isGrappling;
    private float boostReadyAt;
    private float currentRopeLength;
    private GameObject hookVisualInstance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rope = GetComponent<LineRenderer>();
        if (rope == null)
        {
            rope = gameObject.AddComponent<LineRenderer>();
        }

        rope.positionCount = 2;
        rope.startWidth = 0.04f;
        rope.endWidth = 0.03f;
        rope.enabled = false;
    }

    private void Update()
    {
        if (!grapplingEnabled && isGrappling)
        {
            StopGrapple(false);
        }

        UpdateGrappleAimCache();
        UpdateMarker();
        UpdateRopeVisual();
    }

    private void FixedUpdate()
    {
        HandleGrapplePhysics();
    }

    private void OnDisable()
    {
        StopGrapple(false);
        if (grappleMarker != null)
        {
            grappleMarker.gameObject.SetActive(false);
        }

        if (rope != null)
        {
            rope.enabled = false;
        }
    }

    public void SetGrappleHeld(bool held)
    {
        grappleHeld = held;
    }

    public void TryStartGrapple()
    {
        if (!grapplingEnabled || !hasCachedGrapplePoint || isGrappling)
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

    public void StopGrapple(bool applyBoost)
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

        if (rope != null)
        {
            rope.enabled = false;
        }

        if (!applyBoost || Time.time < boostReadyAt)
        {
            return;
        }

        // Boost along horizontal swing momentum, blended with facing for controllability.
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 fallback = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        Vector3 boostDir = horizontalVel.sqrMagnitude > 0.01f
            ? horizontalVel.normalized
            : fallback;

        rb.AddForce(boostDir * releaseBoostImpulse, ForceMode.Impulse);
        boostReadyAt = Time.time + releaseBoostCooldown;
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
        hasCachedGrapplePoint = false;

        if (!grapplingEnabled || cameraTransform == null)
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

        bool markerVisible = grapplingEnabled && hasCachedGrapplePoint;
        grappleMarker.gameObject.SetActive(markerVisible);

        if (markerVisible)
        {
            grappleMarker.position = cachedGrapplePoint;
        }
    }

    private void UpdateRopeVisual()
    {
        if (rope == null)
        {
            return;
        }

        if (!isGrappling)
        {
            rope.enabled = false;
            return;
        }

        rope.enabled = true;
        rope.SetPosition(0, transform.position);
        rope.SetPosition(1, cachedGrapplePoint);
    }
}

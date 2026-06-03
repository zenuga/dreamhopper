using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grapplehook : MonoBehaviour
{
    [Header("Grappling Hook Settings")]
    public float grappleReleaseBoost = 2f;
    public float grappleReleaseBoostDuration = 0.15f;
    public bool canUseGrapplingHook = true;
    public GameObject grapplingHookPrefab;
    public float hookSpeed = 20f;
    public float maxGrappleDistance = 20f;
    public float grapplePullSpeed = 2f;
    public LayerMask hookableLayers;

    private bool isGrappling;
    private bool skipStopThisFixedUpdate;
    private Vector3 pendingReleaseBoost;
    private Vector3 grapplePoint;
    private GameObject currentHook;
    private SpringJoint grappleJoint;
    private float currentGrappleLength;
    private LineRenderer lineRenderer;
    private Rigidbody rb;

    public bool IsGrappling => isGrappling;
    public bool ShouldSkipStopThisFixedUpdate => skipStopThisFixedUpdate;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Do not change Rigidbody gravity here; let movement controller decide.

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.enabled = false;
    }

    public void HandleUpdate()
    {
        HandleInput();
        UpdateLineRenderer();

        if (isGrappling)
        {
            ShortenGrapple();
        }
    }

    public void HandleFixedUpdate()
    {
        if (pendingReleaseBoost.sqrMagnitude > 0.0001f)
        {
            float boostStepScale = Time.fixedDeltaTime / Mathf.Max(grappleReleaseBoostDuration, 0.001f);
            Vector3 boostStep = pendingReleaseBoost * boostStepScale;
            rb.linearVelocity += new Vector3(boostStep.x, 0f, boostStep.z);
            pendingReleaseBoost -= boostStep;
        }
    }

    public void ResetSkipStopThisFixedUpdate()
    {
        skipStopThisFixedUpdate = false;
    }

    void HandleInput()
    {
        if (!canUseGrapplingHook)
        {
            return;
        }

        if (Input.GetButtonDown("Fire1") && !isGrappling)
        {
            ShootGrapplingHook();
        }

        if (Input.GetButtonUp("Fire1") && isGrappling)
        {
            ReleaseGrapple();
        }
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
        pendingReleaseBoost = horizontalVelocity.normalized * grappleReleaseBoost;

        isGrappling = false;
        skipStopThisFixedUpdate = true;

        if (grappleJoint != null)
        {
            Destroy(grappleJoint);
        }

        if (currentHook != null)
        {
            Destroy(currentHook);
        }
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


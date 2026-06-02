using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GravityModifier : MonoBehaviour
{
    [Header("Player Gravity")]
    public float fallbackGravityStrength = 9.81f;
    public bool useFallbackGravity = false;
    public float alignmentSpeed = 10f;
    public bool alignToGravity = true;
    public float searchRadius = -1f; // <= 0 searches all GravityBody objects in the scene

    [Header("Ground Detection")]
    public float groundRayDistance = 5f;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayerMask = -1;
    public float groundDrag = 5f; // Friction when grounded
    public float airDrag = 1f; // Friction when airborne

    private Rigidbody rb;
    private Vector3 gravityDirection = Vector3.zero;
    private bool isGrounded = false;

    public Vector3 GravityDirection => gravityDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void FixedUpdate()
    {
        if (FindClosestGravityBody(out GravityBody body))
        {
            gravityDirection = (body.GetGravityPoint(transform.position) - transform.position).normalized;
            float strength = body.GetGravityMagnitude(transform.position);
            ApplyGravity(strength);
        }
        else if (useFallbackGravity)
        {
            gravityDirection = Vector3.down;
            ApplyGravity(fallbackGravityStrength);
        }
        else
        {
            gravityDirection = Vector3.zero;
        }

        CheckGrounded();
        UpdateDrag();

        if (alignToGravity)
            AlignRotationToGravity();
    }

    void CheckGrounded()
    {
        if (gravityDirection.sqrMagnitude < Mathf.Epsilon)
        {
            isGrounded = false;
            return;
        }

        Vector3 origin = transform.position - gravityDirection * groundCheckRadius;
        isGrounded = Physics.SphereCast(origin, groundCheckRadius, gravityDirection, out _, groundRayDistance, groundLayerMask);
    }

    void UpdateDrag()
    {
        // Only apply drag when grounded to prevent sliding
        rb.linearDamping = isGrounded ? groundDrag : 0f;
    }

    bool FindClosestGravityBody(out GravityBody body)
    {
        body = null;
        float closestDistanceSqr = float.MaxValue;
        GravityBody[] bodies = Object.FindObjectsByType<GravityBody>();

        foreach (GravityBody candidate in bodies)
        {
            if (candidate == null)
                continue;

            float distanceSqr = (candidate.GetGravityCenter() - transform.position).sqrMagnitude;
            if (searchRadius > 0f && distanceSqr > searchRadius * searchRadius)
                continue;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                body = candidate;
            }
        }

        return body != null;
    }

    void ApplyGravity(float strength)
    {
        rb.AddForce(gravityDirection * strength, ForceMode.Acceleration);
    }

    void AlignRotationToGravity()
    {
        if (gravityDirection.sqrMagnitude < Mathf.Epsilon)
            return;

        // The player's up direction should point away from the gravity pull
        Vector3 desiredUp = -gravityDirection;
        
        // Get a right vector perpendicular to desired up
        Vector3 currentForward = transform.forward;
        Vector3 right = Vector3.Cross(desiredUp, currentForward);
        
        // If forward is parallel to up, use a different reference
        if (right.sqrMagnitude < Mathf.Epsilon)
        {
            right = Vector3.Cross(desiredUp, transform.right);
        }
        
        right = right.normalized;
        Vector3 forward = Vector3.Cross(right, desiredUp).normalized;
        
        // Create target rotation with desired up and forward directions
        Quaternion targetRotation = Quaternion.LookRotation(forward, desiredUp);
        
        // Smoothly interpolate to the target rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * alignmentSpeed);
    }
}

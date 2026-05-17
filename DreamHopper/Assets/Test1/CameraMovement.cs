using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("References")]
    public Transform target;

    [Header("Orbit")]
    public Vector3 targetThirdPersonOffset = new Vector3(0f, 1.6f, 0f);
    public Vector3 firstPersonOffset = new Vector3(0f, 1.65f, 0f);
    public float minDistance = 0.1f;
    public float maxDistance = 6f;
    public float firstPersonThreshold = 0.55f;
    public float zoomSpeed = 4f;
    public float smoothTime = 0.08f;

    [Header("Look")]
    public float mouseSensitivity = 2.2f;
    public float minPitch = -70f;
    public float maxPitch = 80f;
    public float thirdPersonRotateSpeed = 1f;

    [Header("Collision")]
    public LayerMask cameraCollisionMask = ~0;
    public float cameraRadius = 0.2f;

    private float yaw;
    private float pitch;
    private float distance;
    private Vector3 smoothVelocity;

    private void Start()
    {
        distance = Mathf.Clamp(maxDistance * 0.6f, minDistance, maxDistance);
        Vector3 initialAngles = transform.eulerAngles;
        yaw = initialAngles.y;
        pitch = initialAngles.x > 180f ? initialAngles.x - 360f : initialAngles.x;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        HandleZoom();
        HandleLook();
        UpdateCameraPosition();
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    private void HandleLook()
    {
        bool isFirstPerson = distance <= firstPersonThreshold;

        float lookX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float lookY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        bool canRotate = isFirstPerson || Input.GetMouseButton(1);
        if (!canRotate)
        {
            return;
        }

        yaw += lookX * thirdPersonRotateSpeed;
        pitch -= lookY * thirdPersonRotateSpeed;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (isFirstPerson)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = Input.GetMouseButton(1) ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !Input.GetMouseButton(1);
        }
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        float blend = Mathf.InverseLerp(firstPersonThreshold + 0.5f, firstPersonThreshold, distance);
        blend = Mathf.Clamp01(blend);

        Vector3 anchorThird = target.position + targetThirdPersonOffset;
        Vector3 anchorFirst = target.position + firstPersonOffset;
        Vector3 anchor = Vector3.Lerp(anchorThird, anchorFirst, blend);

        float desiredThirdDistance = Mathf.Max(distance, firstPersonThreshold);
        Vector3 desiredThirdPos = anchor - rotation * Vector3.forward * desiredThirdDistance;

        Vector3 desiredPos = Vector3.Lerp(desiredThirdPos, anchor, blend);
        desiredPos = ResolveCameraCollision(anchor, desiredPos);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref smoothVelocity, smoothTime);
        transform.rotation = rotation;
    }

    private Vector3 ResolveCameraCollision(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 0.001f)
        {
            return to;
        }

        dir /= dist;

        RaycastHit hit;
        if (Physics.SphereCast(from, cameraRadius, dir, out hit, dist, cameraCollisionMask, QueryTriggerInteraction.Ignore))
        {
            return from + dir * Mathf.Max(hit.distance - 0.02f, 0f);
        }

        return to;
    }
}
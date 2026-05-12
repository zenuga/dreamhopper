using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target; // The player transform
    public float distance = 5f;
    public float minDistance = 1f;
    public float maxDistance = 10f;
    public float zoomSpeed = 2f;
    public float rotationSpeed = 3f;
    public float verticalAngleLimit = 80f; // Degrees

    [Header("First Person Settings")]
    public float firstPersonThreshold = 1.5f; // Distance at which we switch to first person
    public Vector3 firstPersonOffset = new Vector3(0, 1.6f, 0); // Eye level offset
    public float mouseSensitivity = 2f;

    private float currentDistance;
    private float currentX = 0f;
    private float currentY = 0f;
    private bool isFirstPerson = false;
    private Vector3 initialLocalPosition;

    void Start()
    {
        currentDistance = distance;
        initialLocalPosition = transform.localPosition;

        // Initially set to third person
        UpdateCameraPosition();
    }

    void Update()
    {
        HandleZoom();
        HandleRotation();
        UpdateCameraPosition();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentDistance -= scroll * zoomSpeed;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        isFirstPerson = currentDistance <= firstPersonThreshold;
    }

    void HandleRotation()
    {
        if (isFirstPerson)
        {
            // First person mouse look
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
            currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            currentY = Mathf.Clamp(currentY, -verticalAngleLimit, verticalAngleLimit);
        }
        else
        {
            // Third person rotation with right mouse button
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (Input.GetMouseButton(1))
            {
                currentX += Input.GetAxis("Mouse X") * rotationSpeed;
                currentY -= Input.GetAxis("Mouse Y") * rotationSpeed;
                currentY = Mathf.Clamp(currentY, -verticalAngleLimit, verticalAngleLimit);
            }
        }
    }

    void UpdateCameraPosition()
    {
        if (isFirstPerson)
        {
            // First person: position at player's eye level, rotate with mouse
            transform.localPosition = firstPersonOffset;
            transform.localRotation = Quaternion.Euler(currentY, currentX, 0);
        }
        else
        {
            // Third person: orbit around player
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
            Vector3 direction = new Vector3(0, 0, -currentDistance);
            transform.localPosition = rotation * direction + initialLocalPosition;
            transform.LookAt(target.position + Vector3.up * 1.6f); // Look at player's head level
        }
    }
}
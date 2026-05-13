using UnityEngine;

public class grenadelauncher : MonoBehaviour
{
    public GameObject grenadePrefab;
    public float launchForce = 10f;
    public Transform launchPoint;
    public bool grenadelauncherunlocked = false;
    public bool grenadelauncheractive = false;

    void Update()
    {
        if (grenadelauncherunlocked == true && grenadelauncheractive == true)
        {
            if (Input.GetMouseButtonDown(0)) // Left click to shoot
            {
                Shoot();
            }
        }
    }

    void Shoot()
    {
        // Get mouse position in world space on the plane at launch height
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, launchPoint != null ? launchPoint.position.y : transform.position.y);
        float enter;
        if (plane.Raycast(ray, out enter))
        {
            Vector3 target = ray.GetPoint(enter);
            Vector3 direction = (target - (launchPoint != null ? launchPoint.position : transform.position)).normalized;
            GameObject grenade = Instantiate(grenadePrefab, launchPoint != null ? launchPoint.position : transform.position, Quaternion.identity);
            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction * launchForce;
            }
        }
    }
}

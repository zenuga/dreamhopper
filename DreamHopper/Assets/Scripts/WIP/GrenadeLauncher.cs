using UnityEngine;

public class GrenadeLauncher : MonoBehaviour
{
    public GameObject grenadePrefab;
    public float launchForce = 10f;
    public Transform launchPoint;
    public bool grenadelauncherunlocked = false;
    public bool grenadelauncheractive = false;
    public float cooldown = 2f; // Cooldown time in seconds
    public LayerMask ignoreLayers;

    void Update()
    {
        if (grenadelauncherunlocked == true && grenadelauncheractive == true)
        {
            if (cooldown == (0)&& Input.GetMouseButtonDown(0)) // Left click to shoot
            {
                StartCoroutine(Cooldown());
                Shoot();
            }
        }
    }

    void Shoot()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 target;
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~ignoreLayers))
        {
            target = hit.point;
        }
        else
        {
            Plane plane = new Plane(Vector3.up, launchPoint != null ? launchPoint.position.y : transform.position.y);
            float enter;
            if (!plane.Raycast(ray, out enter))
            {
                return;
            }
            target = ray.GetPoint(enter);
        }

        Vector3 direction = (target - (launchPoint != null ? launchPoint.position : transform.position)).normalized;
        GameObject grenade = Instantiate(grenadePrefab, launchPoint != null ? launchPoint.position : transform.position, Quaternion.identity);
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * launchForce;
        }
    }
    System.Collections.IEnumerator Cooldown()
    {
        cooldown = 2f; // Reset cooldown
        while (cooldown > 0)
        {
            cooldown -= Time.deltaTime;
            yield return null;
        }
        cooldown = 0; // Ensure cooldown is exactly 0 after counting down
    }
}

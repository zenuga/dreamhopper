using UnityEngine;

public class Grenade : MonoBehaviour
{
    public float explosionForce = 100f;
    public float explosionRadius = 5f;
    public float jumpMultiplier = 1.5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    void Explode()
    {
        Vector3 explosionPos = transform.position;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 directionToPlayer = player.transform.position - explosionPos;
                float distanceToPlayer = directionToPlayer.magnitude;

                if (distanceToPlayer <= explosionRadius)
                {
                    Ray ray = new Ray(explosionPos, directionToPlayer.normalized);
                    if (Physics.Raycast(ray, out RaycastHit hit, explosionRadius))
                    {
                        Rigidbody hitBody = hit.collider.attachedRigidbody;
                        if (hitBody == playerRb)
                        {
                            Vector3 hitDirection = (hit.point - explosionPos).normalized;
                            float distanceFactor = 1f - (hit.distance / explosionRadius);
                            float force = explosionForce * Mathf.Max(distanceFactor, 0.1f);

                            // Keep a stronger horizontal component so the launch isn't only upward
                            Vector3 launchDirection = new Vector3(hitDirection.x, Mathf.Clamp(hitDirection.y, 0.2f, 1f), hitDirection.z).normalized;

                            // Reset vertical velocity for consistent launch behavior
                            playerRb.linearVelocity = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
                            playerRb.AddForce(launchDirection * force, ForceMode.Impulse);
                        }
                    }
                }
            }
        }

        Destroy(gameObject);
    }
}
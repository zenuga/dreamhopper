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
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 explosionPos = transform.position;
                Vector3 playerPos = player.transform.position;
                float distance = Vector3.Distance(explosionPos, playerPos);
                if (distance < explosionRadius)
                {
                    Vector3 direction = Vector3.up; // Rocket jump upward
                    float force = explosionForce / (distance * distance + 1f); // Inverse square law with offset
                    if (playerRb.linearVelocity.y > 0) // If player is jumping (has upward velocity)
                    {
                        force *= jumpMultiplier;
                    }
                    playerRb.AddForce(direction * force, ForceMode.Impulse);
                }
            }
        }
        Destroy(gameObject);
    }
}
using UnityEngine;

public class Failed : MonoBehaviour
{
    public Vector3 checkpoint;
    public GameObject player;
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            player.transform.position = checkpoint;
        }
    }
}

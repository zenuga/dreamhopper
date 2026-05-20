using UnityEngine;

public class ItemCollector : MonoBehaviour
{
    [Header("References")]
    public SelectItem selectItem;
    public GrenadeLauncher grenadeLauncher;

    public GameObject portal;
    public GameObject wall;

    public Vector3 location;

    public int dreamOrbsCollected = 0;

    void OnTriggerEnter(Collider trigger)
    {
        // Handle dream orb collection on enter
        if (trigger.gameObject.CompareTag("DreamOrb"))
        {
            dreamOrbsCollected += 1;
            Debug.Log("DreamOrb collected. Total: " + dreamOrbsCollected);
            Destroy(trigger.gameObject);
            goalcheck();
            return;
        }
        if (trigger.gameObject.CompareTag("GrappleHook"))
        {

            Destroy(trigger.gameObject);

            // Unlock item 2 and activate UI
            if (selectItem != null)
            {
                selectItem.UnlockGrappleHook();
                selectItem.SelectItem1(); // Switch to grapple hook
            }
        }
        else if (trigger.gameObject.CompareTag("RocketLauncher"))
        {
            // Collect rocket launcher
            Destroy(trigger.gameObject);

            // Unlock and switch to rocket launcher (item 3)
            if (selectItem != null)
            {
                selectItem.UnlockRocketLauncher();
                selectItem.SelectItem3();
                grenadeLauncher.grenadelauncherunlocked = true;
            }
        }
    }
    void goalcheck()
    {
        if (dreamOrbsCollected >= 12)
        {
            portal.SetActive(true);
        }
    }
    void FixedUpdate()
    {
        if (portal.activeSelf)
        {
            wall.transform.Rotate(Vector3.up * 90f * Time.fixedDeltaTime, Space.Self);
            wall.transform.position = Vector3.MoveTowards(wall.transform.position, location, 5f * Time.fixedDeltaTime);
            if (wall.transform.position == location)
            {
                wall.SetActive(false);
            }
        }
    }

}
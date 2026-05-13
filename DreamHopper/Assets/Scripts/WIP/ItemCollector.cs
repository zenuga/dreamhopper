using UnityEngine;

public class ItemCollector : MonoBehaviour
{
    [Header("References")]
    public SelectItem selectItem;

    void OnTriggerEnter(Collider trigger)
    {
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
            }
        }
    }
}
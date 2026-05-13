using UnityEngine;
using UnityEngine.UI; 

public class Inventory : MonoBehaviour
{
    // Added a reference to the movement script
    public MovementWip MovementWip;
    
    // Fixed: Added the '=' sign to initialize the bool
    public bool ObtainedGrappleHook = false;

    public Image GrappleHookIcon; 
    public Image rocketlauncherIcon;
    public Image emptySlotIcon;

    // Fixed: 'OnCollisionEnter' must start with a capital 'O' to work in Unity
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("GrappleHook"))
        {
            ObtainedGrappleHook = true;
            Destroy(collision.gameObject);
            
            // Fixed: To call a function, use 'FunctionName();' 
            // Do NOT put 'void' in front of it when calling it.
            obtaineditem();
        }
    }

    void obtaineditem()
    {
        // Logic check to ensure MovementWip isn't empty before using it
        if (ObtainedGrappleHook == true && MovementWip != null)
        {
            GrappleHookIcon.enabled = true; // Show the grapple hook icon in the inventory

        }
    }
}
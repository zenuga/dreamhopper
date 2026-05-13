using UnityEngine;
using UnityEngine.UI;

public class SelectItem : MonoBehaviour
{
    [Header("References")]
    public MovementWip movementWip;

    [Header("Item UI Images")]
    public Image itemSlot1UI; // First item slot image
    public Image itemSlot1AdditionalUI; // Additional image for item 1
    public Image itemSlot2UI; // Second item slot image (Grapple Hook)
    public Image itemSlot2AdditionalUI; // Additional image for item 2
    public Image itemSlot3UI; // Third item slot image (Rocket Launcher)

    [Header("Equipped Items GameObjects")]
    public GameObject equippedItem1; // Grappling Hook equipped item
    public GameObject equippedItem2; // Rocket Launcher equipped item

    [Header("Item Selection")]
    public int currentSelectedItem = 1; // 1, 2, or 3, default to 1
    public bool grappleHookUnlocked = false; // Initially locked
    public bool rocketLauncherUnlocked = false; // Initially locked

    [Header("UI Opacity Settings")]
    [Range(0f, 1f)] public float selectedOpacity = 1.0f; // Fully visible when selected
    [Range(0f, 1f)] public float unselectedOpacity = 0.3f; // Dimmed when not selected
    [Range(0f, 1f)] public float lockedOpacity = 0.1f; // Very dimmed when locked

    void Start()
    {
        // Initialize with default state - only item 1 available
        UpdateAllUI();
        SelectItem1();
    }

    void Update()
    {
        // Handle item switching with keyboard input
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Q))
        {
            SelectItem1();
        }

        // Only allow switching to item 2 if grapple hook is unlocked
        if (grappleHookUnlocked && (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.E)))
        {
            SelectItem2();
        }

        // Only allow switching to item 3 if rocket launcher is unlocked
        if (rocketLauncherUnlocked && Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectItem3();
        }
    }

    public void SelectItem1()
    {
        if (currentSelectedItem == 1) return;

        currentSelectedItem = 1;

        // Update UI opacity for all items
        UpdateAllUI();

        // Disable grappling hook ability
        if (movementWip != null)
        {
            movementWip.canUseGrapplingHook = false;
        }

        Debug.Log("Selected Item 1 - Grapple Disabled");
    }

    public void SelectItem2()
    {
        if (currentSelectedItem == 2 || !grappleHookUnlocked) return;

        currentSelectedItem = 2;

        // Update UI opacity for all items
        UpdateAllUI();

        // Enable grappling hook ability
        if (movementWip != null)
        {
            movementWip.canUseGrapplingHook = true;
        }

        Debug.Log("Selected Item 2 (Grapple Hook) - Grapple Enabled");
    }

    public void SelectItem3()
    {
        if (currentSelectedItem == 3 || !rocketLauncherUnlocked) return;

        currentSelectedItem = 3;
        UpdateAllUI();

        // Disable grappling hook ability when switching to rocket launcher
        if (movementWip != null)
        {
            movementWip.canUseGrapplingHook = false;
        }

        Debug.Log("Selected Item 3 (Rocket Launcher) - Rocket Launcher Enabled");
    }

    private void UpdateAllUI()
    {
        // Update Item 1 UI and additional image (same opacity)
        float item1Opacity = (currentSelectedItem == 1) ? selectedOpacity : unselectedOpacity;
        
        if (itemSlot1UI != null)
        {
            Color color = itemSlot1UI.color;
            color.a = item1Opacity;
            itemSlot1UI.color = color;
        }
        
        if (itemSlot1AdditionalUI != null)
        {
            Color color = itemSlot1AdditionalUI.color;
            color.a = item1Opacity;
            itemSlot1AdditionalUI.color = color;
        }

        // Update Item 2 UI and additional image (same opacity)
        float item2Opacity;
        if (grappleHookUnlocked)
        {
            item2Opacity = (currentSelectedItem == 2) ? selectedOpacity : unselectedOpacity;
        }
        else
        {
            item2Opacity = lockedOpacity;
        }
        
        if (itemSlot2UI != null)
        {
            Color color = itemSlot2UI.color;
            color.a = item2Opacity;
            itemSlot2UI.color = color;
        }
        
        if (itemSlot2AdditionalUI != null)
        {
            Color color = itemSlot2AdditionalUI.color;
            color.a = item2Opacity;
            itemSlot2AdditionalUI.color = color;
        }

        // Update Item 3 UI
        if (itemSlot3UI != null)
        {
            Color color = itemSlot3UI.color;
            if (rocketLauncherUnlocked)
            {
                color.a = (currentSelectedItem == 3) ? selectedOpacity : unselectedOpacity;
            }
            else
            {
                color.a = lockedOpacity;
            }
            itemSlot3UI.color = color;
        }
    }

    public void UnlockGrappleHook()
    {
        grappleHookUnlocked = true;
        // Activate equipped item 1 (grappling hook)
        if (equippedItem1 != null)
        {
            equippedItem1.SetActive(true);
        }
        UpdateAllUI();
        Debug.Log("Grapple Hook Unlocked! Item 2 now available.");
    }

    public void UnlockRocketLauncher()
    {
        rocketLauncherUnlocked = true;
        // Deactivate equipped item 1 and activate equipped item 2
        if (equippedItem1 != null)
        {
            equippedItem1.SetActive(false);
        }
        if (equippedItem2 != null)
        {
            equippedItem2.SetActive(true);
        }
        UpdateAllUI();
        Debug.Log("Rocket Launcher Unlocked! Item 3 now available.");
    }
}

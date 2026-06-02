using UnityEngine;

[DisallowMultipleComponent]
public class ScreenSkyboxController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private bool includeChildren = true;

    [Header("Shader Properties")]
    [SerializeField] private string playerPosProperty = "_PlayerPos";

    private MaterialPropertyBlock propertyBlock;
    private Renderer[] targetRenderers;

    private void Awake()
    {
        ResolveReferences();
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }
    private void ResolveReferences()
    {
            //set the renderer properly
        if (targetRenderer == null) //try to get the renderer from the current gameobject if not set
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (includeChildren) //get all renderers in children if includeChildren is true
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }
        else if (targetRenderer != null) //if includeChildren is false and targetRenderer is set, use only that renderer
        {
            targetRenderers = new[] { targetRenderer };
        }
        else //if includeChildren is false and targetRenderer is not set, clear the targetRenderers array
        {
            targetRenderers = null;
        }
    }

    private void LateUpdate() //use LateUpdate to ensure player position is updated before applying to shader
    {
        if (player == null)
        {
            return; //(stops lateupdate if player reference is missing, preventing errors)
        }

        if (targetRenderers == null || targetRenderers.Length == 0) // if targetRenderers is null or empty, try to resolve references again (in case something was changed in the inspector)
        {
            ResolveReferences();
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            return; //(stops lateupdate if targetRenderers array is empty, preventing errors)
        }

        for (int i = 0; i < targetRenderers.Length; i++) // loops this part of the code for each renderer in the targetRenderers array, i++ increments counter by 1 each time
        {
            Renderer rendererItem = targetRenderers[i]; //gets the current renderer from the array and stores it in a variable called rendererItem
            
            rendererItem.GetPropertyBlock(propertyBlock); //reads the current propertys
            propertyBlock.SetVector(playerPosProperty, player.position); //sets player position to shader
            rendererItem.SetPropertyBlock(propertyBlock); //send data to the shader
        }
    }
}
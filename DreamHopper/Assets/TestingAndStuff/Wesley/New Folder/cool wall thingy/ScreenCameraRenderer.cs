using UnityEngine;

[DisallowMultipleComponent] //unity only capable of adding 1 of this script to gameobject :3
public class ScreenCameraRenderer : MonoBehaviour
{
    [Header("Cubemap Settings")]
    [SerializeField] private int cubemapResolution = 1024; 
    [Header("Camera Settings")]
    [SerializeField] private LayerMask cullingMask = -1; //remind me to use this later for some funny stuff
    [SerializeField] private CameraClearFlags clearFlags = CameraClearFlags.Nothing; // clears the flags or sum idk i think this is a funny dialogue
    [SerializeField] private Color backgroundColor = new Vector4(0f, 0f, 0f, 1f); //RGBA
    [Header("Material Assignment")]
    [SerializeField] private Material[] targetMaterials; //what materials to assign to 
    [Header("Follow Target")]
    [SerializeField] private Transform followTarget; //target (player)
    [SerializeField] private Vector3 positionOffset = Vector3.zero; //offset
    private Camera renderCamera; //the camera we create and track
    private RenderTexture cubemapTexture; //the texture the camera renders 

    private void Awake()
    {
        // Create camera
        GameObject camObj = new GameObject("WallCameraRenderer");//creates new gameobject with name "WallCameraRenderer" and assigns it to camObj variable

        camObj.transform.SetParent(transform); //parent the camera
        camObj.transform.localPosition = Vector3.zero; // set local position to 000
        renderCamera = camObj.AddComponent<Camera>(); //add a camera to our gameobject and assign it to renderCamera variable
        //set camera settings
        renderCamera.cullingMask = cullingMask;
        renderCamera.clearFlags = clearFlags;
        renderCamera.backgroundColor = backgroundColor;
        renderCamera.enabled = false; // rendered manually via RenderToCubemap

        // Create cubemap render texture
        cubemapTexture = new RenderTexture(cubemapResolution, cubemapResolution, 24); 
        cubemapTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube; // converts the rendertexture to a cubemap

        // Assign to materials
        foreach (Material mat in targetMaterials)
        {
            if (mat == null) 
            {
                continue;
            }
            if (mat.HasProperty("_CameraRenderCube"))
            {
                mat.SetTexture("_CameraRenderCube", cubemapTexture);
            } 
        }
    }
    private void LateUpdate()
    {
        if (followTarget != null)
        {
            renderCamera.transform.position = followTarget.position + positionOffset;
            renderCamera.transform.rotation = followTarget.rotation;
        }
        renderCamera.RenderToCubemap(cubemapTexture); //renders the camera to the cubemap texture each frame, this is what makes the reflections update in real time
    }
}

using UnityEngine;

public class Options : MonoBehaviour
{
    public bool isFullscreen;
    public void FullscreenToggle(bool isFullscreen)
    {
        if (isFullscreen == true )
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
        
    }

    public void GraphicsQualityForDropdown(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

}


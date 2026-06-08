using UnityEngine;
using UnityEngine.SceneManagement;

public class Raycast : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Interactable"))
                {
                    SceneManager.LoadScene("Level2");
                }
            }
        }
    }
}

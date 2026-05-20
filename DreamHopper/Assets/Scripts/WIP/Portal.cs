using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public string level = "level1";
    public float loadDelay = 1f;
    bool isLoading = false;
    public MonoBehaviour movementWip;
    public MonoBehaviour grenadeLauncher;
    public ParticleSystem portalEffect;

    void OnTriggerEnter(Collider trigger)
    {

        if (isLoading)
            return;

        if (trigger.gameObject.CompareTag("Player"))
        {
            StartCoroutine(LoadSceneDelayed());
            movementWip.enabled = false;
            grenadeLauncher.enabled = false;
            if (portalEffect != null)
            {
                portalEffect.Play();
            }
        }
    }

    IEnumerator LoadSceneDelayed()
    {
        isLoading = true;
        yield return new WaitForSeconds(loadDelay);
        SceneManager.LoadScene(level);
    }
}

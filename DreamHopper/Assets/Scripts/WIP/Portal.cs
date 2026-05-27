using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public string level = "level1";
    public float loadDelay = 1f;
    bool isLoading = false;
    public MonoBehaviour[] behavioursToDisable;
    public GameObject[] objectsToDeactivate;
    public ParticleSystem portalEffect;

    void OnTriggerEnter(Collider trigger)
    {

        if (isLoading)
            return;

        if (trigger.gameObject.CompareTag("Player"))
        {
            StartCoroutine(LoadSceneDelayed());

            if (behavioursToDisable != null)
            {
                foreach (var behaviour in behavioursToDisable)
                {
                    if (behaviour != null)
                        behaviour.enabled = false;
                }
            }

            if (objectsToDeactivate != null)
            {
                foreach (var obj in objectsToDeactivate)
                {
                    if (obj != null)
                        obj.SetActive(false);
                }
            }

            if (portalEffect != null)
            {
                portalEffect.Play();
            }

            enabled = false;
        }
    }

    IEnumerator LoadSceneDelayed()
    {
        isLoading = true;
        yield return new WaitForSeconds(loadDelay);
        SceneManager.LoadScene(level);
    }
}

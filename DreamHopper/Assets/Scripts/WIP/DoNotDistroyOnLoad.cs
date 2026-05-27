using UnityEngine;

public class DoNotDistroyOnLoad : MonoBehaviour
{

public GameObject[] objectsToDontDestroy;
    void Awake()
    {
            DontDestroyOnLoad(gameObject);
    }
}

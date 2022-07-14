using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemManager : MonoBehaviour
{
    public static ParticleSystemManager instance;

    private void Start()
    {

    }
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public void ActivatePS(int PS) 
    {
        foreach (Transform item in transform)
        {
            item.gameObject.SetActive(false);
        }

        transform.GetChild(PS).gameObject.SetActive(true);
    }

    public void DesactivatePS(int PS) 
    {
        StartCoroutine(TurnOffPS(PS));
    }

    IEnumerator TurnOffPS(int PS) 
    {
        yield return new WaitForSeconds(3);
        transform.GetChild(PS).gameObject.SetActive(false);
        foreach (Transform item in transform)
        {
            item.gameObject.SetActive(false);
        }
    }

}

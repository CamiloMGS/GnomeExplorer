using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Niantic.ARDK.Utilities.Input.Legacy;
using UnityEngine.EventSystems;
public class TapToPlace : MonoBehaviour
{
    [SerializeField] private Camera aRCamera;
    [SerializeField] private GameObject nomo;
    [SerializeField] private bool ShouldSpawnNomo;
    // Start is called before the first frame update
    void Start()
    {
        GameManager.instance.OnGameStart += SpawnNomo;
    }

    private void SpawnNomo()
    {
        ShouldSpawnNomo = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (ShouldSpawnNomo)
        {
            if (PlatformAgnosticInput.touchCount <= 0)
            {
                return;
            }

            var touch = PlatformAgnosticInput.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = aRCamera.ScreenPointToRay(touch.position);
                if (Physics.Raycast(ray, out RaycastHit hitMesh, 3.0f))
                {
                    nomo.SetActive(true);
                    nomo.transform.position = hitMesh.point;
                    GamePlayManager.instance.shouldCollect = true;
                }

            }
        }

    }
}

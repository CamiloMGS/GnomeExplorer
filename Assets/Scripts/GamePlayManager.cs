using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
public class GamePlayManager : MonoBehaviour
{
    public event Action OnStartCollect;
    public event Action OnFinishCollect;
    public bool shouldCollect = false;

    public static GamePlayManager instance;
    int Channel;
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

    [Serializable]
    public struct Condition
    {
        public ARSegmentationController.Channels channel;
        public int RequirementToCollect;
        public int QuantityToCollect;

    }

    public Condition[] conditions;

    void Start()
    {
        OnFinishCollect += GenerateRandomChannel;
    }
    public void StartCollect(string channel) 
    {
        Debug.Log("Start Collecting: " + channel);
        OnStartCollect?.Invoke();
    }


    public void FinishCollect(string channel)
    {
        Debug.Log("Finish Collecting: " + channel);
        OnFinishCollect?.Invoke();
        ParticleSystemManager.instance.DesactivatePS(Channel);
    }

    public void GenerateRandomChannel() 
    {
        Channel = Random.Range(0, 5);
        ARSegmentationController.instance.ChangeChannel(Channel);

    }
}

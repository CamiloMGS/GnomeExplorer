using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
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

    public event Action OnGameStart;
    void Start()
    {

    }

    public void GameStart()
    {
        Debug.Log("Game Start");
        OnGameStart?.Invoke();
    }
}

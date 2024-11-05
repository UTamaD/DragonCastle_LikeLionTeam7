using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperManager : MonoBehaviour
{
    public static SuperManager Instance { get; private set; }

    public string playerId;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
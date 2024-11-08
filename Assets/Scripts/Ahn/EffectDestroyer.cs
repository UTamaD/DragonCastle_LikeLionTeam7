using System;
using UnityEngine;
using UnityEngine.Serialization;

public class EffectDestroyer : MonoBehaviour
{
    public float maxLifetime = 15f;
    private float lifetime;
    public GameObject Effect;
    
    private void Start()
    {
        lifetime = 0f;
    }
    
    private void Update()
    {

        lifetime += Time.deltaTime;
        if (lifetime >= maxLifetime)
        {
            DestroyEffect();
        }
    }

    public void DestroyEffect()
    {
        Destroy(Effect);
    }



}
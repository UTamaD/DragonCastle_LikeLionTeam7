using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffectManager : MonoBehaviour
{
    public GameObject LandEffect;
    public GameObject FootstepEffect;
    
    public GameObject MeleeEffect;
    public GameObject SkillEffect;
    public GameObject DashEffect;

    public GameObject DamagedEffect;

    public void PlayLand(Vector3 position, Vector3 normal)
    {
        Quaternion lookRotation = Quaternion.LookRotation(normal);
        Instantiate(LandEffect, position, lookRotation);
    }
    
    public void PlayFootStep(Vector3 position)
    {
        Instantiate(FootstepEffect, position, Quaternion.identity);
    }

    public void PlayMelee(Vector3 position, Vector3 normal)
    {
        Quaternion lookRotation = Quaternion.LookRotation(normal);
        Instantiate(MeleeEffect, position, lookRotation);
    }
    
    public void PlaySkill(Vector3 position, Vector3 normal)
    {
        Quaternion lookRotation = Quaternion.Euler(normal);
        Instantiate(SkillEffect, position, lookRotation);
    }
    
    public void PlayDash(Vector3 position, Vector3 normal)
    {
        Quaternion lookRotation = Quaternion.LookRotation(normal);
        Instantiate(DashEffect, position, lookRotation);
    }
    
    public void PlayDamaged(Vector3 position, Vector3 normal)
    {
        Quaternion lookRotation = Quaternion.LookRotation(normal);
        Instantiate(DamagedEffect, position, lookRotation);
    }
}

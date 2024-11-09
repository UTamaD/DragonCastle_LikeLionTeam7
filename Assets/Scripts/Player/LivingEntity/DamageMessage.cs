using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct DamageMessage
{
    public GameObject damager; 
    
    public DamageType type;
    public float amount;
    
    public Vector3 hitPoint;
    public Vector3 hitNormal;
}

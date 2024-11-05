using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectTargetFollower : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;

    public void Initialize(Transform target)
    {
        this.target = target;
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}

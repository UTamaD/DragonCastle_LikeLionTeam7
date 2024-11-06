using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class EnemyAction : Action
{
    //protected Rigidbody rb;
    protected Animator animator;
    
    [System.Serializable]
    public class EffectTimingInfo
    {
        [Range(0, 1)]
        public float activationTime;
        [Range(0, 1)]
        public float deactivationTime;
        public GameObject effectObject;
    }
    
    

    [SerializeField]
    protected List<EffectTimingInfo> effectTimings = new List<EffectTimingInfo>();
    
    public override void OnAwake()
    {
        //rb = GetComponent<Rigidbody>();
        //animator = gameObject.GetComponentInChildren<Animator>();
        animator = GetComponent<Animator>();
    }
}

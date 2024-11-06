using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class EnemyConditional : Conditional
{
    public int MaxtHealth = 100;
    public SharedInt CurrentHealth = 100;
    protected Animator animator;
    
    public override void OnAwake()
    {
        CurrentHealth = MaxtHealth;
        animator = gameObject.GetComponentInChildren<Animator>();
    }
}

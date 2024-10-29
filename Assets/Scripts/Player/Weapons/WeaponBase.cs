using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase
{
    public PlayerController Owner { get; private set; }
    
    public WeaponBase(PlayerController owner)
    {
        Owner = owner;
    }
    
    public abstract void Attack();
    public abstract void DashAttack();
    public abstract void Skill();
    public abstract void OnFixedUpdateState();
}

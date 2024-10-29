using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillBase
{
    public PlayerController Owner { get; private set; }
    
    public DamageType Type;
    public float AttackMultiplier;

    public SkillBase(PlayerController owner)
    {
        Owner = owner;
    }

    public abstract void Attack();
}

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

    public abstract bool IsAvailable();
    public abstract void SetComboCount();
    public abstract void Active();
    public abstract void Inactive();
}

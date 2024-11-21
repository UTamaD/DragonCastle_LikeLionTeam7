using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class LivingEntity : MonoBehaviour, IDamageable
{
    [Header("== Basic Status ==")]
    public float HpMax = 100;

    public float Hp { get; protected set; }

    public bool IsDead { get; protected set; }

    public UnityAction<Vector3> OnDamagedEvent;
    public UnityAction OnDeathEvent;

    private void Start()
    {
        Hp = HpMax;
    }

    public virtual void ChangeHp(float value)
    {
        Hp += value;
        UIManager.Instance.SetHpBar(Hp, HpMax);

        if (Hp <= 0)
            Die();
    }
    
    private void OnHpChanged()
    {
    }
    
    public virtual void ApplyDamage(float dmgAmount, Vector3 dir)
    {
        ChangeHp(-dmgAmount);
        
        OnDamagedEvent?.Invoke(dir);
    }
    
    public virtual void ApplyDamage(DamageMessage dmgMsg)
    {
        ChangeHp(-dmgMsg.amount);
        OnDamagedEvent?.Invoke(dmgMsg.hitNormal);
    }
    
    public virtual void Die()
    {
    }
}
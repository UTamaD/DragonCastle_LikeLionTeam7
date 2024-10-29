using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class LivingEntity : MonoBehaviour, IDamageable
{
    [Header("== Basic Status ==")]
    public float HpMax = 100;    
    public float MpMax = 100;

    public float Hp { get; protected set; }
    public float Mp { get; protected set; }

    public bool IsDead { get; protected set; }

    public UnityAction OnDamagedEvent;
    public UnityAction OnDeathEvent;

    public virtual void ChangeHp(float value)
    {
        Hp += value;
        OnHpChanged();
    }
    
    public virtual void ChangeMp(float value)
    {
        Mp += value;
        OnMpChanged();
    }

    private void OnHpChanged()
    {
    }

    private void OnMpChanged()
    {
    }
    
    public virtual void ApplyDamage(DamageMessage dmgMsg)
    {
        ChangeHp(-dmgMsg.amount);
        OnDamagedEvent?.Invoke();
    }
    
    public virtual void Die()
    {
    }
}
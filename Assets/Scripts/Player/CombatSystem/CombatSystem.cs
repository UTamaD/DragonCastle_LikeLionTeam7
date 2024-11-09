using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DamageType
{
    
}

public class CombatSystem : MonoBehaviour
{
    public PlayerController Owner { get; private set; }

    private List<SkillBase> _skills;

    private float _atk;
    private float _def;
    
    //debug 변수
    public bool drawGizmo = false;
    
    public void Start()
    {
        Owner = GetComponent<PlayerController>();
    }

    public void ActiveDamageField(PlayerSkillName skillName, float dist)
    {
        drawGizmo = true;
        Invoke(nameof(DeleteGizmos), 1f);
        
        bool success = Physics.SphereCast(transform.position, 2f,transform.forward, out var hit, dist);
        if (!success)
            return;
        
        SkillBase skill = Owner.SkillManager.GetSkill(skillName);
        
        Collider damagedObj = hit.collider;
        MonsterController monster = damagedObj.GetComponentInParent<MonsterController>();
        if (!monster)
            return;
        
        float def = 0;
        
        DamageMessage msg = new DamageMessage
        {
            damager = Owner.gameObject,
            type = skill.Type,
            amount = CalculateDamage(_atk * skill.AttackMultiplier, def),
            hitPoint = hit.point,
            hitNormal = hit.normal
        };

        float dmg = CalculateDamage(_atk * skill.AttackMultiplier, def);
        monster.TakeDamage(dmg);
    }

    private void DeleteGizmos()
    {
        drawGizmo = false;
    }

    private void OnDrawGizmos()
    {
        if (drawGizmo)
        {
            if (Physics.SphereCast(transform.position, 2f, transform.forward, out RaycastHit hit, 2f))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, transform.forward * hit.distance);
                Gizmos.DrawWireSphere(transform.position + transform.forward * hit.distance, 2.0f);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, transform.forward * hit.distance);
                Gizmos.DrawWireSphere(transform.position + transform.forward * hit.distance, 2.0f);
            }
        }
    }

    // 최종 데미지 계산
    public float CalculateDamage(float atk, float def)
    {
        float result = atk * (1 - def);
        
        return result;
    }

    // 최종 방어력 계산
    public float CalculateDefense()
    {
        return _def;
    }
}

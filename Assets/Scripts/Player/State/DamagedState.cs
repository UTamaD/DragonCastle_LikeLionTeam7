using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagedState : BaseState
{
    private float knockbackForce = 50f; // 넉백 강도
    private float knockbackDuration = 0.2f; // 넉백 지속
    
    private Vector3 knockbackDirection;
    private float knockbackTimer = 0f;
    
    private readonly int _animIDDamaged;
    private readonly int _animIDKnockBack;
    
    public DamagedState(PlayerController owner) : base(owner)
    {
        if (!Owner)
            return;
        
        _animIDDamaged = Animator.StringToHash("Damaged");
        _animIDKnockBack = Animator.StringToHash("KnockBack");
    }

    public override void OnEnterState()
    {
        Owner.Animator.applyRootMotion = true;
        Owner.Animator.SetBool(_animIDKnockBack, true);
        Owner.Animator.SetTrigger(_animIDDamaged);
        ApplyKnockback(Owner.transform.forward);
    }
    
    public void ApplyKnockback(Vector3 sourcePosition)
    {
        // 넉백 방향 계산
        knockbackDirection = (Owner.transform.position - sourcePosition).normalized * knockbackForce;
        knockbackDirection.y = 0; // 넉백 시 Y축을 무시하고 평면에서만 적용 (필요 시 조정)

        knockbackTimer = knockbackDuration; // 넉백 지속 시간 설정
    }

    public override void OnUpdateState()
    {
        if (knockbackTimer > 0)
        {
            // 넉백 타이머 감소
            knockbackTimer -= Time.deltaTime;

            // 넉백 이동
            Owner.Controller.Move(knockbackDirection * Time.deltaTime);

            // 점진적으로 힘 줄이기 (감속 효과)
            knockbackDirection = Vector3.Lerp(knockbackDirection, Vector3.zero, Time.deltaTime / knockbackDuration);
        }
    }

    public override void OnLateUpdateState()
    {
        
    }

    public override void OnFixedUpdateState()
    {
        
    }

    public override void OnExitState()
    {
        Owner.Animator.applyRootMotion = false;
        Owner.Animator.SetBool(_animIDKnockBack, false);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpAttack : SkillBase
{
    private readonly int _animIDJumpAttack;
    
    private readonly float _coolTime;
    
    public JumpAttack(PlayerController owner) : base(owner)
    {
        AttackMultiplier = 1.2f;
        _coolTime = 5f;
        
        _animIDJumpAttack = Animator.StringToHash("JumpAttack");
    }

    public override bool IsAvailable()
    {
        return true;
    }

    public override void SetComboCount()
    {
        return;
    }

    public override void Active()
    {
        Owner.Animator.applyRootMotion = true;
        Owner.Animator.SetBool(_animIDJumpAttack, true);
        TcpProtobufClient.Instance.SendApplyRootMotion(Owner.Player.PlayerId, true);
        TcpProtobufClient.Instance.SendAnimatorCondision(Owner.Player.PlayerId,"JumpAttack", true);

        Owner.IsSkillCoolTime = true;
        UIManager.Instance.SetCoolTime(0, _coolTime);
        CheckAttackCoolTime(_coolTime);
    }

    public override void Inactive()
    {
        Owner.Animator.applyRootMotion = false;
        Owner.Animator.SetBool(_animIDJumpAttack, false);
        TcpProtobufClient.Instance.SendApplyRootMotion(Owner.Player.PlayerId, false);
        TcpProtobufClient.Instance.SendAnimatorCondision(Owner.Player.PlayerId,"JumpAttack", false);
    }
    
    private void CheckAttackCoolTime(float coolTime)
    {
        Owner.CoroutineStarter(CheckSkillAttackInput(coolTime));
    }

    private IEnumerator CheckSkillAttackInput(float coolTime)
    {
        float curTime = 0f;
        while (true)
        {
            curTime += Time.deltaTime;
            UIManager.Instance.SetCoolTime(curTime, coolTime);
            if (curTime >= coolTime)
                break;
            
            yield return null;
        }
        
        UIManager.Instance.SetCoolTime(coolTime, coolTime);
        Owner.IsSkillCoolTime = false;
    }
}

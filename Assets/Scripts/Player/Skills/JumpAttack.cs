using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpAttack : SkillBase
{
    private readonly int _animIDJumpAttack;
    
    public JumpAttack(PlayerController owner) : base(owner)
    {
        AttackMultiplier = 1.2f;
        
        _animIDJumpAttack = Animator.StringToHash("JumpAttack");
    }

    public override bool IsAvailable()
    {
        return true;
    }

    public override void Active()
    {
        Owner.Animator.applyRootMotion = true;
        Owner.Animator.SetBool(_animIDJumpAttack, true);
        TcpProtobufClient.Instance.SendApplyRootMotion(Owner.Player.PlayerId, true);
        TcpProtobufClient.Instance.SendAnimatorCondision(Owner.Player.PlayerId,"JumpAttack", true);
    }

    public override void Inactive()
    {
        Owner.Animator.applyRootMotion = false;
        Owner.Animator.SetBool(_animIDJumpAttack, false);
        TcpProtobufClient.Instance.SendApplyRootMotion(Owner.Player.PlayerId, false);
        TcpProtobufClient.Instance.SendAnimatorCondision(Owner.Player.PlayerId,"JumpAttack", false);
    }
}

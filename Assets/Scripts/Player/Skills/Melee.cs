using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Melee : SkillBase
{
    private int _comboCount;
    private readonly float _reInputTime;
    private Coroutine _checkAttackReInputCor;
    
    private readonly int _animIDMelee;
    private readonly int _animIDCombo;
    
    public Melee(PlayerController owner) : base(owner)
    {
        AttackMultiplier = 1f;
            
        _comboCount = 0;
        _reInputTime = 3f;
        
        _animIDMelee = Animator.StringToHash("Melee");
        _animIDCombo = Animator.StringToHash("Combo");
    }

    public override bool IsAvailable()
    {
        return (_comboCount < (Owner.Combo + 1));
    }

    public override void SetComboCount()
    {
        _comboCount = 0;
    }

    public override void Active()
    {
        _comboCount++;
        Owner.Animator.applyRootMotion = true;
        Owner.Animator.SetBool(_animIDMelee, true);
        Owner.Animator.SetInteger(_animIDCombo, _comboCount);
        TcpProtobufClient.Instance.SendApplyRootMotion(Owner.Player.PlayerId, true);
        TcpProtobufClient.Instance.SendAnimatorCondision(Owner.Player.PlayerId,"Melee", true);
        TcpProtobufClient.Instance.SendAnimatorCondision(Owner.Player.PlayerId,"Combo", _comboCount);
        
        CheckAttackReInput(_reInputTime);
    }

    public override void Inactive()
    {
        Owner.Animator.applyRootMotion = false;
        Owner.Animator.SetBool(_animIDMelee, false);
        TcpProtobufClient.Instance.SendApplyRootMotion(Owner.Player.PlayerId, false);
        TcpProtobufClient.Instance.SendAnimatorCondision(Owner.Player.PlayerId, "Melee", false);
    }

    private void CheckAttackReInput(float reInputTime)
    {
        if (_checkAttackReInputCor != null)
            Owner.CoroutineStopper(_checkAttackReInputCor);
        
        _checkAttackReInputCor = Owner.CoroutineStarter(CheckAttackInput(reInputTime));
    }
    
    private IEnumerator CheckAttackInput(float reInputTime)
    {
        float curTime = 0f;
        while (true)
        {
            curTime += Time.deltaTime;
            if (curTime >= reInputTime)
                break;

            yield return null;
        }

        _comboCount = 0;
        Owner.Animator.SetInteger(_animIDCombo, 0);
        TcpProtobufClient.Instance.SendAnimatorCondision(Owner.Player.PlayerId,"Combo", 0);
    }
}

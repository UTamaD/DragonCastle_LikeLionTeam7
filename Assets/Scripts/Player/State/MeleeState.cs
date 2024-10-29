using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeState : BaseState
{
    private int _comboCount;
    private readonly float _reInputTime;
    private Coroutine _checkAttackReInputCor;
    
    private readonly int _animIDMelee;
    private readonly int _animIDCombo;

    public bool IsAvailableMelee => (_comboCount < 4);

    public MeleeState(PlayerController owner) : base(owner)
    {
        _comboCount = 0;
        _reInputTime = 3f;
        
        _animIDMelee = Animator.StringToHash("Melee");
        _animIDCombo = Animator.StringToHash("Combo");
    }

    public override void OnEnterState()
    {
        _comboCount++;
        Owner.Animator.applyRootMotion = true;
        Owner.Animator.SetBool(_animIDMelee, true);
        Owner.Animator.SetInteger(_animIDCombo, _comboCount);
        
        CheckAttackReInput(_reInputTime);
    }

    public override void OnUpdateState()
    {
    }

    public override void OnLateUpdateState()
    {
    }

    public override void OnFixedUpdateState()
    {
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
    }

    public override void OnExitState()
    {
        Owner.Animator.applyRootMotion = false;
        Owner.Animator.SetBool(_animIDMelee, false);
        Owner.Animator.SetInteger(_animIDCombo, 0);
    }
}

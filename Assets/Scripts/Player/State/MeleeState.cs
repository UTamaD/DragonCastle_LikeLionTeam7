using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeState : BaseState
{
    private SkillBase _curSkill;
    
    public MeleeState(PlayerController owner) : base(owner)
    {
        
    }

    public void SetCurSkill(SkillBase skill)
    {
        _curSkill = skill;
    }

    public override void OnEnterState()
    {
        _curSkill.Active();
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

    public override void OnExitState()
    { 
        _curSkill.Inactive();
    }
}

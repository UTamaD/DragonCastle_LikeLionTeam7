using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IFrameState : BaseState
{
    private readonly int _animIDIFrame;
    
    public IFrameState(PlayerController owner) : base(owner)
    {
        if (!Owner)
            return;
        
        _animIDIFrame = Animator.StringToHash("IFrame");
    }

    public override void OnEnterState()
    {
        Owner.Animator.SetTrigger(_animIDIFrame);
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
    }
}

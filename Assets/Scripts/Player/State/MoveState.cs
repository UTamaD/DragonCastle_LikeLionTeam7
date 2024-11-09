using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveState : BaseState
{
    private readonly float _moveSpeed;
    private readonly float _sprintSpeed;
    
    private readonly int _animIDSpeed;
    
    private float _rotationVelocity;
    
    public MoveState(PlayerController owner) : base(owner)
    {
        if (!Owner)
            return;
        
        _moveSpeed = Owner.MoveSpeed;
        _sprintSpeed = Owner.SprintSpeed;
        
        _animIDSpeed = Animator.StringToHash("Speed");
    }

    public override void OnEnterState()
    {
        
    }

    public override void OnUpdateState()
    {
        float targetSpeed = Owner.IsSprint? _sprintSpeed : _moveSpeed;
        if (Owner.Inputs.move == Vector2.zero)
        {
            Owner.IsSprint = false;
            targetSpeed = 0f;
        }
        
        Vector3 inputDirection = new Vector3(Owner.Inputs.move.x, 0.0f, Owner.Inputs.move.y).normalized;
        float targetRotation = 0f;
        if (Owner.Inputs.move != Vector2.zero)
        {
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                   Owner.MainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(Owner.transform.eulerAngles.y, targetRotation, 
                ref _rotationVelocity, Owner.RotationSmoothTime);
        
            Owner.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
        
        Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;
        Owner.Controller.Move(targetDirection.normalized * (targetSpeed * Time.deltaTime) +
                             new Vector3(0.0f, Owner._verticalVelocity, 0.0f) * Time.deltaTime);

        Owner.Animator.SetFloat(_animIDSpeed, targetSpeed);
    }

    public override void OnLateUpdateState()
    {
        
    }

    public override void OnFixedUpdateState()
    {
        
    }

    public override void OnExitState()
    {
        Owner.Animator.SetFloat(_animIDSpeed, 0f);
    }
}

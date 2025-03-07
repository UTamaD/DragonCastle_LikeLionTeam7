using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerInputs : MonoBehaviour
{
	[Header("Character Input Values")]
    public Vector2 move;
    public Vector2 look;
    public bool jump;

    [Header("Movement Settings")]
    public bool analogMovement;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;

    public UnityAction OnIFrameInput;
    public UnityAction OnMeleeInput;
    public UnityAction OnJumpAttackInput;

    public void OnMove(InputValue value)
    {
    	MoveInput(value.Get<Vector2>());
    }

    public void OnLook(InputValue value)
    {
    	if(cursorInputForLook)
    	{
    		LookInput(value.Get<Vector2>());
    	}
    }

    public void OnJump(InputValue value)
    {
    	JumpInput(value.isPressed);
    }

    public void OnIFrame(InputValue value)
    {
	    OnIFrameInput.Invoke();
    }

    public void OnMelee(InputValue value)
    {
	    OnMeleeInput.Invoke();
    }
    
    public void OnJumpAttack(InputValue value)
    {
	    OnJumpAttackInput.Invoke();
    }
    
    public void MoveInput(Vector2 newMoveDirection)
    {
    	move = newMoveDirection;
    } 

    public void LookInput(Vector2 newLookDirection)
    {
    	look = newLookDirection;
    }

    public void JumpInput(bool newJumpState)
    {
    	jump = newJumpState;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
    	SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
    	Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}


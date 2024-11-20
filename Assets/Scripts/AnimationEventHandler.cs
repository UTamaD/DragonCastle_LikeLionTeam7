using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class AnimationEventHandler : MonoBehaviour
{
    private PlayerController Owner;

    private void Start()
    {
        Owner = GetComponent<PlayerController>();
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (Owner.FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, Owner.FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(Owner.FootstepAudioClips[index], transform.TransformPoint(Owner.Controller.center), Owner.FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(Owner.LandingAudioClip, transform.TransformPoint(Owner.Controller.center), Owner.FootstepAudioVolume);
        }
    }

    private void OnDash(AnimationEvent animationEvent)
    {
        object info = animationEvent.objectReferenceParameter;
        DashInfo dashInfo = info.ConvertTo<DashInfo>();
        if (!dashInfo)
        {
            Debug.LogWarning("fail to object type cast to " + dashInfo.name);
            return;
        }

        if (!Owner)
            return;
        StartCoroutine(Owner.MoveForDistanceAndTime(dashInfo.dashDis, dashInfo.dashTime));
    }
    
    private void EndIFrame(AnimationEvent animationEvent)
    {
        if (!Owner)
            return;
        Owner.StateMachine.ChangeState(StateName.Move);
    }
    
    private void OnEndMelee()
    {
        if (!Owner)
            return;
        Owner.StateMachine.ChangeState(StateName.Move);
    }

    private void OnEndDamaged()
    {
        if (!Owner)
            return;
        Owner.StateMachine.ChangeState(StateName.Move);
    }

    private void OnCreateDamageField(AnimationEvent animationEvent)
    {
        if (!Owner)
            return;
        object info = animationEvent.objectReferenceParameter;
        SkillInfo skillInfo = info.ConvertTo<SkillInfo>();
        if (!skillInfo)
        {
            Debug.LogWarning("fail to object type cast to " + skillInfo.name);
            return;
        }
        
        Owner.CombatSystem.ActiveDamageField(skillInfo.skillName, skillInfo.dist);
    }
}

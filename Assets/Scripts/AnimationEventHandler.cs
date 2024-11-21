using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class AnimationEventHandler : MonoBehaviour
{
    public Transform FootL;
    public Transform FootR;

    public Transform SkillTr;
    
    private PlayerController _owner;

    private PlayerEffectManager _effect;
    private PlayerSoundManager _sound;

    private CharacterController _controller;

    private void Start()
    {
        _owner = GetComponent<PlayerController>();
        _effect = GetComponent<PlayerEffectManager>();
        _sound = GetComponent<PlayerSoundManager>();
        _controller = GetComponent<CharacterController>();
    }

    private void OnFootstepL(AnimationEvent animationEvent)
    {
        _effect.PlayFootStep(FootL.position);
        _sound.PlayFootStep(animationEvent, transform.TransformPoint(_controller.center));
    }
    
    private void OnFootstepR(AnimationEvent animationEvent)
    {
        _effect.PlayFootStep(FootR.position);
        _sound.PlayFootStep(animationEvent, transform.TransformPoint(_controller.center));
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        //_effect.PlayLand();
        _sound.PlayLand(animationEvent, transform.TransformPoint(_controller.center));
    }

    private void OnDash(AnimationEvent animationEvent)
    {
        //_effect.PlayDash();
        _sound.PlayDash(transform.TransformPoint(_controller.center));
            
        if (!_owner)
            return;
        
        object info = animationEvent.objectReferenceParameter;
        DashInfo dashInfo = info.ConvertTo<DashInfo>();
        if (!dashInfo)
        {
            Debug.LogWarning("fail to object type cast to " + dashInfo.name);
            return;
        }
        
        StartCoroutine(_owner.MoveForDistanceAndTime(dashInfo.dashDis, dashInfo.dashTime));
    }

    private void OnMeleeStart(AnimationEvent animationEvent)
    {
        //_effect.PlayMelee();
        _sound.PlayMelee(transform.TransformPoint(_controller.center), animationEvent.intParameter);
    }
    
    private void OnSkill()
    {
        _effect.PlaySkill(SkillTr.position, transform.eulerAngles);
        _sound.PlaySkill(transform.TransformPoint(_controller.center));
    }

    public void OnDamaged()
    {
        _sound.PlayDamaged(transform.TransformPoint(_controller.center));
    }
    
    private void EndIFrame(AnimationEvent animationEvent)
    {
        if (!_owner)
            return;
        
        _owner.StateMachine.ChangeState(StateName.Move);
    }
    
    private void OnEndMelee()
    {
        if (!_owner)
            return;
        
        _owner.StateMachine.ChangeState(StateName.Move);
    }

    private void OnEndDamaged()
    {
        if (!_owner)
            return;
        
        _owner.StateMachine.ChangeState(StateName.Move);
    }

    private void OnCreateDamageField(AnimationEvent animationEvent)
    {
        if (!_owner)
            return;
        
        object info = animationEvent.objectReferenceParameter;
        SkillInfo skillInfo = info.ConvertTo<SkillInfo>();
        if (!skillInfo)
        {
            Debug.LogWarning("fail to object type cast to " + skillInfo.name);
            return;
        }
        
        _owner.CombatSystem.ActiveDamageField(skillInfo.skillName, skillInfo.dist);
    }
}

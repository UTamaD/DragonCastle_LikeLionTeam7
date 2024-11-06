using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections.Generic;

public class BossBreathAttackAction : EnemyAction
{
    public string breathAnimationStateName = "Breath Attack";
    public string breathAnimationTrigger = "BreathAttack";
    
    public Transform breathOrigin;
    public GameObject breathDamageFieldPrefab;

    private GameObject spawnedBreathEffect;
    private GameObject spawnedDamageField;
    private bool isPerformingBreath = false;

    public override void OnStart()
    {
        base.OnStart();

        if (animator != null)
        {
            animator.SetTrigger(breathAnimationTrigger);
        }

        
        // 데미지 필드 생성
        if (breathDamageFieldPrefab != null && breathOrigin != null)
        {
            spawnedDamageField = Object.Instantiate(breathDamageFieldPrefab, breathOrigin.position, breathOrigin.rotation);
            spawnedDamageField.transform.SetParent(breathOrigin);
            spawnedDamageField.SetActive(false);
        }

        // 모든 이펙트 초기 비활성화
        foreach (var timing in effectTimings)
        {
            if (timing.effectObject != null)
            {
                timing.effectObject.SetActive(false);
            }
        }

        isPerformingBreath = true;
    }

    public override TaskStatus OnUpdate()
    {
        if (!isPerformingBreath)
        {
            return TaskStatus.Success;
        }

        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(breathAnimationStateName))
            {
                float normalizedTime = stateInfo.normalizedTime;

                // 이펙트 및 데미지 필드 활성화/비활성화
                UpdateEffectsAndDamageField(normalizedTime);

                if (normalizedTime >= 0.99f)
                {
                    isPerformingBreath = false;
                    return TaskStatus.Success;
                }
            }
        }
        else
        {
            isPerformingBreath = false;
            return TaskStatus.Failure;
        }

        return TaskStatus.Running;
    }

    private void UpdateEffectsAndDamageField(float normalizedTime)
    {
        bool shouldActivate = false;

        foreach (var timing in effectTimings)
        {
            if (timing.effectObject != null)
            {
                if (normalizedTime >= timing.activationTime && normalizedTime < timing.deactivationTime)
                {
                    timing.effectObject.SetActive(true);
                    shouldActivate = true;
                }
                else
                {
                    timing.effectObject.SetActive(false);
                }
            }
        }

        // 브레스 이펙트와 데미지 필드 활성화/비활성화
        if (spawnedBreathEffect != null)
        {
            spawnedBreathEffect.SetActive(shouldActivate);
        }
        if (spawnedDamageField != null)
        {
            spawnedDamageField.SetActive(shouldActivate);
        }
    }

    public override void OnEnd()
    {
        // 액션이 종료될 때 모든 이펙트 비활성화
        foreach (var timing in effectTimings)
        {
            if (timing.effectObject != null)
            {
                timing.effectObject.SetActive(false);
            }
        }

        // 브레스 이펙트와 데미지 필드 제거를 위한 지연 삭제 요청
        if (spawnedBreathEffect != null)
        {
            RequestDestroy(spawnedBreathEffect);
        }
        if (spawnedDamageField != null)
        {
            RequestDestroy(spawnedDamageField);
        }

        isPerformingBreath = false;
    }

    private void RequestDestroy(GameObject obj)
    {

        Object.Destroy(obj);
    }
}
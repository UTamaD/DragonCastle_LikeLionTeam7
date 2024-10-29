using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class BossStompAction : EnemyAction
{
    public string stompAnimationStateName = "Attack 2";
    public string stompAnimationTrigger = "Stomp";
    public float stompDuration = 1.5f;

    public bool useNewEffectSystem = false; // 새로운 이펙트 시스템 사용 여부

    private float actionTimer;
    private GameObject spawnedEffect;
    public Transform effctSpawnPoint;

    public override void OnStart()
    {
        base.OnStart();

        if (animator != null)
        {
            animator.SetTrigger(stompAnimationTrigger);
        }

        actionTimer = 0f;
        
        if (useNewEffectSystem)
        {
            spawnedEffect = null;
        }
        else
        {
            // 기존 방식: 모든 이펙트 초기 비활성화
            foreach (var timing in effectTimings)
            {
                if (timing.effectObject != null)
                {
                    timing.effectObject.SetActive(false);
                }
            }
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(stompAnimationStateName))
            {
                float normalizedTime = stateInfo.normalizedTime;

                foreach (var timing in effectTimings)
                {
                    if (useNewEffectSystem)
                    {
                        if (normalizedTime >= timing.activationTime && spawnedEffect == null)
                        {
                            SpawnStompEffect(timing.effectObject);
                        }
                    }
                    else
                    {
                        // 기존 방식: 이펙트 활성화/비활성화
                        if (timing.effectObject != null)
                        {
                            if (normalizedTime >= timing.activationTime && normalizedTime < timing.deactivationTime)
                            {
                                timing.effectObject.SetActive(true);
                            }
                            else
                            {
                                timing.effectObject.SetActive(false);
                            }
                        }
                    }
                }

                if (normalizedTime >= 0.99f)
                {
                    return TaskStatus.Success;
                }
            }
        }
        else
        {
            return TaskStatus.Failure;
        }

        return TaskStatus.Running;
    }

    private void SpawnStompEffect(GameObject stompEffect)
    {
        if (stompEffect != null && effctSpawnPoint != null)
        {
            spawnedEffect = GameObject.Instantiate(stompEffect, effctSpawnPoint.position, Quaternion.identity);
            
            if (spawnedEffect != null)
            {
               
            }
        }
    }
    public override void OnEnd()
    {
        if (useNewEffectSystem)
        {
            // 새로운 방식: 필요한 경우 생성된 이펙트 정리
            // foreach (var effect in spawnedEffects)
            // {
            //     Destroy(effect);
            // }
            // spawnedEffects.Clear();
        }
        else
        {
            // 기존 방식: 모든 이펙트 비활성화
            foreach (var timing in effectTimings)
            {
                if (timing.effectObject != null)
                {
                    timing.effectObject.SetActive(false);
                }
            }
        }
    }
}
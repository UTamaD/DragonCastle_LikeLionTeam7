using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections.Generic;

public class BossFireballAction : EnemyAction
{
    public string fireballAnimationStateName = "Fireball Attack";
    public string fireballAnimationTrigger = "FireballAttack";
    public GameObject fireballPrefab;
    public Transform fireballSpawnPoint;
    public SharedTransform targetTransform;
    public float fireballLaunchtime = 0.4f;

    
    private GameObject spawnedFireball;

    public override void OnStart()
    {
        base.OnStart();

        if (animator != null)
        {
            animator.SetTrigger(fireballAnimationTrigger);
        }

        // 모든 이펙트 초기 비활성화
        foreach (var timing in effectTimings)
        {
            if (timing.effectObject != null)
            {
                timing.effectObject.SetActive(false);
            }
        }

        spawnedFireball = null;
        
    }

    public override TaskStatus OnUpdate()
    {
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(fireballAnimationStateName))
            {
                float normalizedTime = stateInfo.normalizedTime;

                // 이펙트 활성화/비활성화
                foreach (var timing in effectTimings)
                {
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

                // 파이어볼 발사 로직
                if (normalizedTime >= fireballLaunchtime && spawnedFireball == null)
                {
                    SpawnAndLaunchFireball();
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

    private void SpawnAndLaunchFireball()
    {
        if (fireballPrefab != null && fireballSpawnPoint != null && targetTransform.Value != null)
        {
            spawnedFireball = GameObject.Instantiate(fireballPrefab, fireballSpawnPoint.position, Quaternion.identity);
            RFX1_TransformMotion fireballProjectile = spawnedFireball.GetComponentInChildren<RFX1_TransformMotion>();
            if (fireballProjectile != null)
            {
                fireballProjectile.Initialize(targetTransform.Value.position,true);
            }
            else
            {
                Debug.LogWarning("Fireball prefab does not have a FireballProjectile component.");
            }
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
    }
}
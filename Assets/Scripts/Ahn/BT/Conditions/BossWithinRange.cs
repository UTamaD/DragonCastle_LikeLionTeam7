using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class BossWithinRange : Conditional
{
    // 보스의 공격 사거리
    public float attackRange = 10f;

    // 타깃의 태그
    public string targetTag = "Player";

    // 타깃이 발견되면 target 변수를 설정하여 후속 태스크가 target인 오브젝트를 알 수 있도록 합니다.
    public SharedTransform target;

    // 타깃이 될 수 있는 모든 대상에 대한 캐시
    private Transform[] possibleTargets;

    public override void OnAwake()
    {
        // targetTag 태그가 있는 모든 Transform을 캐싱합니다.
        var targets = GameObject.FindGameObjectsWithTag(targetTag);
        possibleTargets = new Transform[targets.Length];
        for (int i = 0; i < targets.Length; ++i)
        {
            possibleTargets[i] = targets[i].transform;
        }
    }

    public override TaskStatus OnUpdate()
    {
        // 타깃이 공격 사거리 내에 있으면 성공을 반환
        for (int i = 0; i < possibleTargets.Length; ++i)
        {
            if (WithinRange(possibleTargets[i]))
            {
                // 다른 태스크가 사거리 내에 있는 Transform을 알 수 있도록 타깃을 설정합니다.
                target.Value = possibleTargets[i];
                return TaskStatus.Success;
            }
        }
        return TaskStatus.Failure;
    }

    // targetTransform이 현재 Transform의 공격 사거리 내에 있으면 true를 반환합니다.
    private bool WithinRange(Transform targetTransform)
    {
        float distance = Vector3.Distance(transform.position, targetTransform.position);
        return distance <= attackRange;
    }
}
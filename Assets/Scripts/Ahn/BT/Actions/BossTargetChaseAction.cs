using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class BossTargetChaseAction : EnemyAction
{
    public SharedTransform targetTransform;
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.1f;
    public string moveAnimationBoolName = "Chase";
    public float maxChaseTime = 5f; // 최대 추격 시간 (초)

    private Vector3 targetPosition;
    private bool isMoving = false;
    private float chaseTimer = 0f; // 추격 시간을 측정하는 타이머

    public override void OnStart()
    {
        base.OnStart();

        if (targetTransform.Value == null)
        {
            return;
        }

        // 이동을 시작할 때의 타겟 위치 저장
        targetPosition = targetTransform.Value.position;

        // 이동 애니메이션 시작
        StartMoveAnimation();

        // 타이머 초기화
        chaseTimer = 0f;
    }

    public override TaskStatus OnUpdate()
    {
        if (targetTransform.Value == null)
        {
            StopMoveAnimation();
            return TaskStatus.Failure;
        }

        // 추격 시간 증가
        chaseTimer += Time.deltaTime;

        // 최대 추격 시간을 초과했는지 확인
        if (chaseTimer >= maxChaseTime)
        {
            StopMoveAnimation();
            return TaskStatus.Success;
        }

       
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget > stoppingDistance)
        {
            
            Vector3 moveDirection = (targetPosition - transform.position).normalized;

            
            Vector3 newPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;
            newPosition.y = transform.position.y; 

            transform.position = newPosition;

            return TaskStatus.Running;
        }
        else
        {
            
            StopMoveAnimation();
            return TaskStatus.Success;
        }
    }

    private void StartMoveAnimation()
    {
        if (animator != null && !isMoving)
        {
            animator.SetBool(moveAnimationBoolName, true);
            isMoving = true;
        }
    }

    private void StopMoveAnimation()
    {
        if (animator != null && isMoving)
        {
            animator.SetBool(moveAnimationBoolName, false);
            isMoving = false;
        }
    }

    public override void OnEnd()
    {
        StopMoveAnimation();
    }
}
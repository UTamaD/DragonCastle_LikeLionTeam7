using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class BossRotateAction : Action
{
    public SharedTransform target;
    public float rotationSpeed = 120f; // 초당 회전 속도
    public float angleThreshold = 5f; // 목표 각도에 도달했다고 간주할 임계값

    public string rotationAnimationTrigger = "StartRotation";
    public string idleAnimationTrigger = "Idle";

    private Animator animator;
    private bool isRotating = false;

    public override void OnAwake()
    {
        animator = GetComponent<Animator>();
    }

    public override void OnStart()
    {
        isRotating = false;
    }

    public override TaskStatus OnUpdate()
    {
        if (target.Value == null)
        {
            return TaskStatus.Failure;
        }

        Vector3 targetDirection = target.Value.position - transform.position;
        targetDirection.y = 0; // 수평 회전만 고려

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            float angle = Quaternion.Angle(transform.rotation, targetRotation);

            if (angle > angleThreshold)
            {
                // 목표 방향으로 회전
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                if (!isRotating)
                {
                    // 회전 애니메이션 시작
                    animator.SetTrigger(rotationAnimationTrigger);
                    isRotating = true;
                }

                return TaskStatus.Running;
            }
            else
            {
                if (isRotating)
                {
                    // 회전 완료, 대기 애니메이션으로 전환
                    animator.SetTrigger(idleAnimationTrigger);
                    isRotating = false;
                }
                return TaskStatus.Success;
            }
        }

        return TaskStatus.Success;
    }
}
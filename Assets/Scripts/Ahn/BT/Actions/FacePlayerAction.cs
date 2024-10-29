using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class FacePlayerAction : EnemyAction
{
    public SharedTransform targetTransform;
    public float rotationSpeed = 5f;
    public string rotateAnimationTrigger = "Rotate";

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isRotating = false;

    public override void OnStart()
    {
        base.OnStart();

        if (targetTransform.Value == null)
        {
            return;
        }

        // 회전을 시작할 때의 타겟 위치 저장
        targetPosition = targetTransform.Value.position;
        
        // 타겟을 향한 회전 계산
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0; // Y축 회전만 고려
        targetRotation = Quaternion.LookRotation(direction);

        // 회전 애니메이션 시작
        if (animator != null)
        {
            animator.SetBool(rotateAnimationTrigger,true);
            isRotating = true;
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (targetTransform.Value == null)
        {
            StopRotationAnimation();
            return TaskStatus.Failure;
        }

        // 현재 회전과 목표 회전 사이의 각도 계산
        float angle = Quaternion.Angle(transform.rotation, targetRotation);

        // 목표 회전으로 부드럽게 회전
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 회전이 거의 완료되면 태스크 완료
        if (angle < 0.1f)
        {
            StopRotationAnimation();
            return TaskStatus.Success;
        }

        return TaskStatus.Running;
    }

    private void StopRotationAnimation()
    {
        if (isRotating && animator != null)
        {
            animator.SetBool(rotateAnimationTrigger,false);
            isRotating = false;
        }
    }

    public override void OnEnd()
    {
        StopRotationAnimation();
    }
}
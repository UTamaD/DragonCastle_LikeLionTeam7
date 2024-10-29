using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class BossFrontAttackAction : EnemyAction
{
    public string attackAnimationTrigger = "Attack";
    public float attackDuration = 1f;

    [SerializeField]
    private GameObject damageFieldObject;

    private float attackTimer;
    private bool isAttackComplete = false;

    public override void OnStart()
    {
        base.OnStart();

        // 공격 애니메이션 시작
        if (animator != null)
        {
            animator.SetTrigger(attackAnimationTrigger);
        }

        attackTimer = 0f;
        isAttackComplete = false;

        // 데미지 필드 초기 비활성화
        if (damageFieldObject != null)
        {
            damageFieldObject.SetActive(false);
        }
    }

    public override TaskStatus OnUpdate()
    {
        attackTimer += Time.deltaTime;

        // 공격 지속 시간이 끝나거나 공격이 완료되면 액션 완료
        if (attackTimer >= attackDuration || isAttackComplete)
        {
            return TaskStatus.Success;
        }

        return TaskStatus.Running;
    }

    // 애니메이션 이벤트에서 호출될 메서드
    public void ToggleDamageField()
    {
        if (damageFieldObject != null)
        {
            damageFieldObject.SetActive(!damageFieldObject.activeSelf);
        }
    }

    // 애니메이션 이벤트에서 호출될 메서드
    public void OnAttackComplete()
    {
        isAttackComplete = true;
        // 공격 완료 시 데미지 필드 비활성화
        if (damageFieldObject != null)
        {
            damageFieldObject.SetActive(false);
        }
    }

    public override void OnEnd()
    {
        // 액션이 종료될 때 데미지 필드를 반드시 비활성화
        if (damageFieldObject != null)
        {
            damageFieldObject.SetActive(false);
        }
    }
}
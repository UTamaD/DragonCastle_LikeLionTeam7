using BehaviorDesigner.Runtime.Tasks;
using DG.Tweening;
using UnityEngine;


public class DestroyBoss : EnemyAction
{
    public float bleedTime = 2.0f;

    private bool isDestroyed;
    
    public override void OnStart()
    {
        //사망 이펙트,카메라 효과 추가 예정
        DOVirtual.DelayedCall(bleedTime, () =>
        {
            isDestroyed = true;
            Object.Destroy(gameObject);
        }, false);
    }

    public override TaskStatus OnUpdate()
    {
        return isDestroyed ? TaskStatus.Success : TaskStatus.Running;
    }
}

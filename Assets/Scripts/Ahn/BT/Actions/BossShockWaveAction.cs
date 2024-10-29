using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class BossShockWaveAction : EnemyAction
{
    public string shockWaveAnimationTrigger = "ShockWave";
    public float shockWaveDuration = 2f;
    public float shockWaveSpeed = 5f;
    public float shockWaveMaxRadius = 10f;

    [SerializeField]
    private GameObject shockWaveObject;

    private float actionTimer;
    private bool isShockWaveComplete = false;
    private Vector3 initialShockWaveScale;

    public override void OnStart()
    {
        base.OnStart();
        

        actionTimer = 0f;
        isShockWaveComplete = false;

        // 충격파 오브젝트 초기화
        if (shockWaveObject != null)
        {
            initialShockWaveScale = shockWaveObject.transform.localScale;
            shockWaveObject.transform.localScale = Vector3.zero;
            shockWaveObject.SetActive(true);
        }
    }

    public override TaskStatus OnUpdate()
    {
        actionTimer += Time.deltaTime;

        // 충격파 확장
        if (shockWaveObject != null && !isShockWaveComplete)
        {
            float currentRadius = shockWaveSpeed * actionTimer;
            float scaleFactor = currentRadius / shockWaveMaxRadius;
            shockWaveObject.transform.localScale = initialShockWaveScale * scaleFactor;

            if (currentRadius >= shockWaveMaxRadius)
            {
                isShockWaveComplete = true;
            }
        }

        // 충격파 지속 시간이 끝나거나 충격파가 완료되면 액션 완료
        if (actionTimer >= shockWaveDuration || isShockWaveComplete)
        {
            return TaskStatus.Success;
        }

        return TaskStatus.Running;
    }

    public override void OnEnd()
    {
        // 액션이 종료될 때 충격파 오브젝트를 반드시 비활성화
        if (shockWaveObject != null)
        {
            shockWaveObject.SetActive(false);
        }
    }
}
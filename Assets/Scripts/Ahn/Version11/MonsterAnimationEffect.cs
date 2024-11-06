using UnityEngine;
using System;

[Serializable]
public class EffectData
{
    public string effectName;           // 인스펙터에서 구분하기 위한 이름
    public GameObject effectPrefab;     // 이펙트 프리팹
    public Transform spawnPoint;        // 이펙트 생성 위치 (null이면 attackPoint 사용)
    public Vector3 positionOffset;      // 위치 오프셋
    public Vector3 rotationOffset;      // 회전 오프셋
    public float duration = 2f;         // 지속 시간
    public bool followTarget = false;   // 대상 추적 여부
}

public class MonsterAnimationEffect : MonoBehaviour
{
    public EffectData[] effects;        // 사용 가능한 이펙트 목록
    public Transform defaultSpawnPoint; // 기본 생성 위치 (attackPoint)
    
    private Transform currentTarget;     // 현재 타겟
    private MonsterController monsterController;
    private EffectData[] currentEffects;
    
    public void SetEffects(EffectData[] effects)
    {
        currentEffects = effects;
    }


    private void Awake()
    {
        monsterController = GetComponent<MonsterController>();
    }

    // 애니메이션 이벤트에서 호출할 메서드
    public void PlayEffect(string effectName)
    {
        if (currentEffects == null) return;

        EffectData effectData = Array.Find(currentEffects, effect => effect.effectName == effectName);
        if (effectData == null)
        {
            Debug.LogWarning($"Effect not found: {effectName}");
            return;
        }
        

        Transform spawnPoint = effectData.spawnPoint != null ? effectData.spawnPoint : defaultSpawnPoint;
        if (spawnPoint == null)
        {
            Debug.LogWarning("No spawn point specified for effect: " + effectName);
            return;
        }

        // 이펙트 생성 위치와 회전 계산
        Vector3 position = spawnPoint.position + spawnPoint.TransformDirection(effectData.positionOffset);
        Quaternion rotation = spawnPoint.rotation * Quaternion.Euler(effectData.rotationOffset);

        // 이펙트 생성
        GameObject effect = EffectManager.Instance.PlayEffect(
            effectData.effectPrefab,
            position,
            rotation,
            effectData.followTarget ? null : spawnPoint,
            effectData.duration
        );

        // 타겟 추적 설정
        if (effectData.followTarget && effect != null && currentTarget != null)
        {
            var follower = effect.GetComponent<EffectTargetFollower>();
            if (follower == null)
                follower = effect.AddComponent<EffectTargetFollower>();
            follower.Initialize(currentTarget);
        }
    }

    // 현재 타겟 설정 (MonsterController에서 호출)
    public void SetCurrentTarget(Transform target)
    {
        currentTarget = target;
    }
}
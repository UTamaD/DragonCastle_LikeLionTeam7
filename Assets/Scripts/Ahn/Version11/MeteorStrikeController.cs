using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorStrikeController : MonoBehaviour
{
    [System.Serializable]
    public class MeteorStrikeConfig
    {
        public GameObject warningEffectPrefab;
        public GameObject meteorPrefab;
        public float warningDuration = 2f;
        public float meteorHeight = 50f;
        public float initialSpeed = 10f;     // 초기 속도
        public float acceleration = 20f;      // 가속도
        public float maxSpeed = 50f;         // 최대 속도
        public float impactRadius = 5f;
    }

    public MeteorStrikeConfig config;
    private Animator animator;
    private static readonly int MeteorAttackTrigger = Animator.StringToHash("MeteorAttack");
    private static readonly int IsCastingMeteor = Animator.StringToHash("IsCastingMeteor");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void StartMeteorStrike(Vector3[] positions)
    {
        StartCoroutine(MeteorStrikeSequence(positions));
    }

    private IEnumerator MeteorStrikeSequence(Vector3[] positions)
    {
        // 시전 애니메이션 시작
        if (animator != null)
        {
            animator.SetTrigger(MeteorAttackTrigger);
            animator.SetBool(IsCastingMeteor, true);
        }

        List<GameObject> warningEffects = new List<GameObject>();

        // 모든 위치에 경고 이펙트 생성
        foreach (Vector3 pos in positions)
        {
            GameObject warning = Instantiate(config.warningEffectPrefab, 
                new Vector3(pos.x, 0, pos.z), 
                Quaternion.identity);
            warningEffects.Add(warning);
        }

        // 경고 시간 대기
        yield return new WaitForSeconds(config.warningDuration);

        // 각 위치에 순차적으로 메테오 생성
        foreach (var position in positions)
        {
            SpawnMeteor(position);
            yield return new WaitForSeconds(0.5f);
        }

        // 경고 이펙트 제거
        foreach (var warning in warningEffects)
        {
            Destroy(warning);
        }

        // 시전 애니메이션 종료
        if (animator != null)
        {
            animator.SetBool(IsCastingMeteor, false);
        }
    }

    private void SpawnMeteor(Vector3 targetPosition)
    {
        Vector3 spawnPos = new Vector3(targetPosition.x, config.meteorHeight, targetPosition.z);
        GameObject meteor = Instantiate(config.meteorPrefab, spawnPos, Quaternion.identity);
        
        MeteorBehavior meteorBehavior = meteor.GetComponent<MeteorBehavior>();
        if (meteorBehavior != null)
        {
            meteorBehavior.Initialize(
                targetPosition,
                config.initialSpeed,
                config.impactRadius
            );
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorBehavior : MonoBehaviour
{
    public GameObject impactEffectPrefab;
    public AudioClip fallSound;
    public AudioClip impactSound;
    
    [Header("Fall Settings")]
    public float initialSpeed = 10f;     // 초기 속도
    public float acceleration = 20f;      // 가속도
    public float maxSpeed = 50f;         // 최대 속도
    public AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 속도 변화 곡선

    private Vector3 targetPosition;
    private float currentSpeed;
    private float impactRadius;
    private AudioSource audioSource;
    private float travelDistance;        // 총 이동 거리
    private float currentDistance;       // 현재까지 이동한 거리
    private Vector3 startPosition;

    public void Initialize(Vector3 target, float speed, float radius)
    {
        targetPosition = target;
        currentSpeed = initialSpeed;
        impactRadius = radius;
        audioSource = GetComponent<AudioSource>();
        startPosition = transform.position;
        
        // 총 이동 거리 계산
        travelDistance = Vector3.Distance(startPosition, targetPosition);
        currentDistance = 0;

        if (audioSource != null && fallSound != null)
        {
            audioSource.clip = fallSound;
            audioSource.Play();
        }

        // 타겟을 향해 회전
        transform.LookAt(targetPosition);
    }

    private void Update()
    {
        // 진행률 계산
        float progress = currentDistance / travelDistance;

        // 가속도 적용
        currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, maxSpeed);

        // 커브를 통한 속도 조정
        float speedMultiplier = falloffCurve.Evaluate(progress);
        float adjustedSpeed = currentSpeed * speedMultiplier;

        // 이동 거리 계산
        float moveDistance = adjustedSpeed * Time.deltaTime;
        currentDistance += moveDistance;

        // 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveDistance
        );

        // 충돌 체크
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            OnImpact();
        }
    }
    
    private void OnImpact()
    {
        // 충격 이펙트
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, targetPosition, Quaternion.identity);
        }

        // 충격 사운드
        if (audioSource != null && impactSound != null)
        {
            audioSource.PlayOneShot(impactSound);
        }

        // 범위 데미지 처리
        Collider[] colliders = Physics.OverlapSphere(targetPosition, impactRadius);
        foreach (var collider in colliders)
        {
            var damageable = collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float distance = Vector3.Distance(targetPosition, collider.transform.position);
                float damageRatio = 1 - (distance / impactRadius);
                float damage = 50 * damageRatio; // 기본 데미지 50
                //damageable.TakeDamage(damage);
            }
        }

        // 메테오 제거
        Destroy(gameObject);
    }
}
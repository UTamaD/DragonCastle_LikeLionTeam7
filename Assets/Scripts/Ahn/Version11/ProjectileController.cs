using UnityEngine;
using System.Collections;

public class ProjectileController : MonoBehaviour
{
    public float lifeTime = 5f;         // 최대 생존 시간
    public float collisionRadius = 0.5f; // 충돌 체크 반경
    public LayerMask collisionLayers;   // 충돌 체크할 레이어
    public GameObject hitEffectPrefab;  // 충돌 이펙트 프리팹
    
    private Vector3 direction;
    private float speed;
    private bool isInitialized = false;
    
    private void Start()
    {
        // 일정 시간 후 자동 제거
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector3 direction, float speed)
    {
        this.direction = direction.normalized;
        this.speed = speed;
        isInitialized = true;
        
        // 진행 방향으로 회전
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 이동
        Vector3 moveAmount = direction * (speed * Time.deltaTime);
        
        // 충돌 체크
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, collisionRadius, direction, out hit, moveAmount.magnitude, collisionLayers))
        {
            OnHit(hit);
            return;
        }

        // 이동 적용
        transform.position += moveAmount;
    }

    private void OnHit(RaycastHit hit)
    {
        // 충돌 이펙트 생성
        if (hitEffectPrefab != null)
        {
            Quaternion rotation = Quaternion.LookRotation(hit.normal);
            GameObject effect = Instantiate(hitEffectPrefab, hit.point, rotation);
            Destroy(effect, 2f); // 이펙트는 2초 후 제거
        }

        // 데미지 처리 (필요한 경우)
        
        // 투사체 제거
        Destroy(gameObject);
    }

    // 디버그용 기즈모
    private void OnDrawGizmos()
    {
        if (isInitialized)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
        }
    }
}


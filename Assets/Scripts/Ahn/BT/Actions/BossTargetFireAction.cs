using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class BossTargetFireAction : Action
{
    public float attackRange = 20f;
    public float attackCooldown = 3f;
    public int fireDamage = 15;
    public float fireballSpeed = 10f;
    public SharedTransform target;
    public GameObject fireballPrefab;

    private float lastAttackTime;
   
    
    // ActionScript에서 사용할 액션의 인덱스
    public int fireballActionIndex = 0;

    public override void OnAwake()
    {

    }

    public override void OnStart()
    {
        lastAttackTime = -attackCooldown;  // 시작 시 즉시 공격할 수 있도록 설정
    }

    public override TaskStatus OnUpdate()
    {
        if (target.Value == null)
        {
            return TaskStatus.Failure;
        }

        Vector3 directionToTarget = target.Value.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        // 타겟이 공격 범위 내에 있는지 확인
        if (distanceToTarget <= attackRange)
        {
            // 쿨다운 확인
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                FireAtTarget();
                lastAttackTime = Time.time;
                return TaskStatus.Success;
            }
        }

        // 타겟을 향해 회전
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360 * Time.deltaTime);

        return TaskStatus.Running;
    }

    private void FireAtTarget()
    {
        Debug.Log("Boss fires a fireball at the target!");

        // ActionController를 통해 애니메이션과 사운드 실행

        // 파이어볼 생성 및 발사 로직
        GameObject fireball = Object.Instantiate(fireballPrefab, transform.position + transform.forward * 2, Quaternion.identity);
        
        FireballBehavior fireballBehavior = fireball.AddComponent<FireballBehavior>();
        fireballBehavior.target = target.Value.position;
        fireballBehavior.speed = fireballSpeed;
        fireballBehavior.damage = fireDamage;
    }
}

// FireballBehavior 클래스는 이전과 동일하게 유지
public class FireballBehavior : MonoBehaviour
{
    public Vector3 target;
    public float speed;
    public int damage;

    private void Update()
    {
        // 타겟 방향으로 이동
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // 목표 지점에 도달하면 폭발
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            Explode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 충돌 시 폭발
        Explode();
    }

    private void Explode()
    {
        // 폭발 효과 (파티클 시스템 등) 재생
        // ParticleSystem explosionEffect = GetComponent<ParticleSystem>();
        // if (explosionEffect != null)
        // {
        //     explosionEffect.Play();
        // }

        // 주변 오브젝트에 데미지 적용
        // 데미지 적용 로직 구현

        // 파이어볼 오브젝트 제거
        Destroy(gameObject);
    }
}
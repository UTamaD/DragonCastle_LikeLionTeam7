using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DamageField : MonoBehaviour
{
    public enum DamageType
    {
        Melee,          // 근접 공격
        Projectile,     // 투사체
        Meteor,         // 메테오
        PersistentAoE   // 지속성 범위 공격
    }

    public enum DeactivationType
    {
        Timer,          // 시간이 지나면 자동으로 비활성화
        Collision,      // 충돌시 비활성화
        Manual          // 수동으로 비활성화 (애니메이션 이벤트 등에서 사용)
    }

    [System.Serializable]
    public class DamageFieldConfig
    {
        [Header("Basic Settings")]
        public DamageType damageType = DamageType.Melee;
        public DeactivationType deactivationType = DeactivationType.Timer;
        
        [Header("Timing")]
        [Tooltip("데미지 필드가 활성화되기까지 대기하는 시간")]
        public float activationDelay = 0f;
        [Tooltip("데미지 필드가 활성화되어 있는 시간 (DeactivationType이 Timer일 때만 사용)")]
        public float duration = 0.5f;
        
        [Header("Damage Settings")]
        public float damageAmount = 10f;
        [Tooltip("연속 데미지를 주는 간격 (0이면 한번만 데미지)")]
        public float damageInterval = 0f;
        
        [Header("Area Settings")]
        [Tooltip("구체 형태의 데미지 필드 반경")]
        public float radius = 1f;
        
        [Header("Effect Settings")]
        public GameObject hitEffect;
        public AudioClip hitSound;
    }

    [Header("Configuration")]
    public DamageFieldConfig config = new DamageFieldConfig();
    
    [Header("Target Settings")]
    public LayerMask targetLayers;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);

    // Private fields
    private float damageTimer;
    private HashSet<Player> hitPlayers = new HashSet<Player>();
    private Collider damageCollider;
    private bool isActive = false;
    private float currentDuration;

    private void Awake()
    {
        InitializeCollider();
        // 시작시 콜라이더 비활성화
        SetColliderState(false);
    }

    private void OnEnable()
    {
        // 컴포넌트가 활성화될 때 자동으로 데미지 필드 시작
        StartDamageField();
    }

    private void InitializeCollider()
    {
        damageCollider = GetComponent<Collider>();
        if (damageCollider == null)
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = config.radius;
            sphereCollider.isTrigger = true;
            damageCollider = sphereCollider;
        }
        else
        {
            damageCollider.isTrigger = true;
        }
    }

    public void StartDamageField()
    {
        StartCoroutine(DamageFieldSequence());
    }

    private IEnumerator DamageFieldSequence()
    {
        // 활성화 대기 시간
        if (config.activationDelay > 0)
        {
            yield return new WaitForSeconds(config.activationDelay);
        }

        // 데미지 필드 활성화
        ActivateDamageField();

        // Timer 타입인 경우 자동 비활성화
        if (config.deactivationType == DeactivationType.Timer)
        {
            yield return new WaitForSeconds(config.duration);
            DeactivateDamageField();
        }
    }

    private void ActivateDamageField()
    {
        isActive = true;
        currentDuration = 0f;
        damageTimer = 0f;
        hitPlayers.Clear();
        SetColliderState(true);
    }
    

    private void SetColliderState(bool state)
    {
        if (damageCollider != null)
        {
            damageCollider.enabled = state;
        }
    }

    private void Update()
    {
        if (!isActive) return;

        if (config.deactivationType == DeactivationType.Timer)
        {
            currentDuration += Time.deltaTime;
            if (currentDuration >= config.duration)
            {
                DeactivateDamageField();
                return;
            }
        }

        // 지속 데미지 처리
        if (config.damageInterval > 0)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= config.damageInterval)
            {
                ApplyDamageToTargetsInField();
                damageTimer = 0f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        Player player = other.GetComponent<Player>();
        if (player != null && !hitPlayers.Contains(player))
        {
            ApplyDamageToPlayer(player);
            
            if (config.damageInterval <= 0)
            {
                hitPlayers.Add(player);
            }

            // 충돌시 비활성화 타입이고 근접 공격이 아닌 경우
            if (config.deactivationType == DeactivationType.Collision 
                && config.damageType != DamageType.Melee)
            {
                DeactivateDamageField();
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isActive || config.damageInterval <= 0) return;

        Player player = other.GetComponent<Player>();
        if (player != null && !hitPlayers.Contains(player))
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= config.damageInterval)
            {
                ApplyDamageToPlayer(player);
                damageTimer = 0f;
            }
        }
    }

    private void ApplyDamageToPlayer(Player player)
    {
        
        if (player == null) return;
        
        // 데미지 적용
        if (player != null)
        {
            // 효과 재생
            if (config.hitEffect != null)
            {
                Instantiate(config.hitEffect, player.transform.position, Quaternion.identity);
            }

            // 사운드 재생
            if (config.hitSound != null)
            {
                AudioSource.PlayClipAtPoint(config.hitSound, player.transform.position);
            }

            // 서버에 데미지 전송
            TcpProtobufClient.Instance.SendPlayerDamage(
                player.PlayerId, 
                config.damageAmount,
                (int)config.damageType,
                transform.position.x,
                transform.position.y,
                transform.position.z
            );
        }
    }

    private void ApplyDamageToTargetsInField()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, config.radius, targetLayers);
        foreach (var hitCollider in hitColliders)
        {
            Player player = hitCollider.GetComponent<Player>();
            if (player != null)
            {
                ApplyDamageToPlayer(player);
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = gizmoColor;
        if (damageCollider is SphereCollider sphereCollider)
        {
            Gizmos.DrawSphere(transform.position, config.radius);
        }
        else if (damageCollider is BoxCollider boxCollider)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
    }
    
    public void DeactivateDamageField()
    {
        isActive = false;
        SetColliderState(false);
        hitPlayers.Clear();

        // 근접 공격이 아닌 경우에만 오브젝트 파괴
        if (config.damageType != DamageType.Melee)
        {
            StartCoroutine(DestroyAfterDelay());
        }
    }
    
    private IEnumerator DestroyAfterDelay()
    {
        float destroyDelay = 5f; // 5초 후 파괴
        
        // 파티클 시스템이 있다면 새로운 파티클 생성 중지
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particleSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        
        // Trail Renderer가 있다면 페이드아웃
        TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>();
        foreach (var trail in trails)
        {
            trail.time = destroyDelay; // 트레일이 destroyDelay 동안 페이드아웃
            trail.emitting = false;
        }

        yield return new WaitForSeconds(destroyDelay);

        // 부모 오브젝트가 있고 이 데미지 필드가 자식인 경우
        if (transform.parent != null)
        {
            // 부모 오브젝트를 파괴
            Destroy(transform.parent.gameObject);
        }
        else
        {
            // 독립적인 오브젝트인 경우 자신을 파괴
            Destroy(gameObject);
        }
    }
    
    
    private void OnDisable()
    {
        // 스크립트가 비활성화될 때 실행 중인 모든 코루틴 정지
        StopAllCoroutines();
    }

    // 수동으로 데미지 필드를 파괴하기 위한 함수
    public void ForceDestroy()
    {
        StopAllCoroutines();
        StartCoroutine(DestroyAfterDelay());
    }
    
    

    
}

public interface IDamageable
{
    void TakeDamage(float amount, DamageField.DamageType damageType);
}

public interface IKnockbackable
{
    void ApplyKnockback(Vector3 direction, float force, float duration);
}

public interface IDebuffable
{
    void ApplyDebuff(string debuffType, float duration, float intensity);
}
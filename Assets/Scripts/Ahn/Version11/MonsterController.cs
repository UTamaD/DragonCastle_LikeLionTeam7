using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game;

[System.Serializable]
public class MonsterAttackEffect
{
    public GameObject effectPrefab;
    public float duration = 2f;
}


public class MonsterController : MonoBehaviour
{
    
    // 공격 상태 열거형
    private enum AttackState
    {
        None,
        MeleeAttack,
        RangedAttack,
        MeteorAttack
    }
    
    private AttackState currentAttackState = AttackState.None;
    
    private int monsterId;
    private Vector3 currentServerPosition;
    private string currentTargetPlayerId;
    private bool hasTarget;
    
    [System.Serializable]
    public class MonsterAttackConfig
    {
        public string animationTrigger;
        public EffectData[] effects;
        public AudioClip sound;
    }
    
    [System.Serializable]
    public class ProjectileConfig
    {
        public GameObject projectilePrefab;
        public Transform spawnPoint;
        public float speed = 10f;
        public float maxDistance = 30f;
        [Header("Animation Settings")]
        public string fireAnimationTrigger = "RangedAttack";
        public GameObject chargingEffect;    // 차징 시 표시할 이펙트
    }
    
  
    
    [Header("Attack Configs")]
    public MonsterAttackConfig meleeAttackConfig;
    public MonsterAttackConfig rangedAttackConfig;
    
    // 투사체 관련
    [Header("Projectile Settings")]
    public ProjectileConfig projectileConfig;
    public float projectileSpeed = 10f;
    
    [SerializeField]
    private Transform currentTarget;
    private bool isChargingProjectile;
    private GameObject activeChargingEffect;
    
    // 애니메이션 관련
    private Animator animator;
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsTurningLeft = Animator.StringToHash("IsTurningLeft");
    private static readonly int IsTurningRight = Animator.StringToHash("IsTurningRight");
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");

    // 공격 관련
    private float attackRange = 2.0f;
    private float attackDuration = 1.0f;
    private float attackTimer;

    // 이동 관련
    private float moveThreshold = 0.01f;        // 이동으로 간주할 최소 거리
    private float positionLerpSpeed = 15f;      // 위치 보간 속도

    // 회전 관련
    private float rotationThreshold = 5f;       // 회전으로 간주할 최소 각도 (도)
    private float maxRotationSpeed = 360f;      // 최대 회전 속도 (도/초)
    private float rotationAcceleration = 720f;  // 회전 가속도 (도/초^2)
    private float currentRotationVelocity;      // 현재 회전 속도
    private float currentYAngle;                // 현재 Y축 회전 각도
    private float targetYAngle;                 // 목표 Y축 회전 각도
    private float rotationAnimThreshold = 30f;  // 회전 애니메이션 재생 임계값
    
    
    public Transform attackPoint;
    private ParticleSystem attackParticleSystem;
    
    public MonsterAttackEffect attackEffect;

    
    // 공격 사운드 관련
    public AudioClip attackSound;
    private AudioSource audioSource;

    // 공격 애니메이션 관련
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
   
    private MonsterAnimationEffect animationEffect;
    
    private bool isAttacking = false;

    [Header("Meteor Strike")]
    public MeteorStrikeController meteorStrikeController;

    private void Awake()
    {
        
        currentYAngle = transform.eulerAngles.y;
        
        animator = GetComponent<Animator>();
        audioSource = gameObject.AddComponent<AudioSource>();
        animationEffect = GetComponent<MonsterAnimationEffect>();
        
        
    }
    
    public void HandleMeteorStrike(MeteorStrike meteorStrike)
    {
        // 공격 중이면 새로운 공격을 시작하지 않음
        if (isAttacking) return;
        
        if (meteorStrikeController != null)
        {
            Vector3[] positions = meteorStrike.Positions
                .Select(p => new Vector3(p.X, 0, p.Z))
                .ToArray();
                
            meteorStrikeController.StartMeteorStrike(positions);
        }
    }
    public void PerformAttack(string targetPlayerId, int attackType, float damage)
    {
        // 공격 중이면 새로운 공격을 시작하지 않음
        if (isAttacking) return;

        
        
        Transform targetTransform = PlayerController.Instance.GetPlayerTransform(targetPlayerId);

        currentTarget = targetTransform;
        if (targetTransform == null)
        {
            Debug.LogWarning("PerformAttack no target");
            return;
        }
        
     
        // 타겟 방향으로 회전
        Vector3 directionToTarget = targetTransform.position - transform.position;
        if (directionToTarget != Vector3.zero)
        {
            Vector3 currentRotation = transform.rotation.eulerAngles;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Euler(
                currentRotation.x,
                targetRotation.eulerAngles.y,
                currentRotation.z
            );
        }

        // 공격 타입에 따른 처리
        MonsterAttackConfig currentConfig = null;
        switch (attackType)
        {
            case 0: // Melee
                currentConfig = meleeAttackConfig;
                break;
                
            case 1: // Ranged
                currentConfig = rangedAttackConfig;
                break;
        }

        if (currentConfig != null)
        {
            // 애니메이션 트리거
            if (animator != null)
            {
                animator.SetTrigger(currentConfig.animationTrigger);
            }

            // 이펙트 설정
            if (animationEffect != null)
            {
                animationEffect.SetCurrentTarget(targetTransform);
                animationEffect.SetEffects(currentConfig.effects);
            }

            // 사운드 재생
            if (audioSource != null && currentConfig.sound != null)
            {
                audioSource.PlayOneShot(currentConfig.sound);
            }
        }

    }

    public void OnStartCharging()
    {
        if (projectileConfig.chargingEffect != null && projectileConfig.spawnPoint != null)
        {
            activeChargingEffect = Instantiate(projectileConfig.chargingEffect, 
                projectileConfig.spawnPoint.position, 
                projectileConfig.spawnPoint.rotation, 
                projectileConfig.spawnPoint);
        }
        isChargingProjectile = true;
    }

    // 투사체 발사
    public void OnFireProjectile()
    {
        Debug.LogWarning("OnFireProjectile");
        if (currentTarget == null) return;

        // 차징 이펙트 제거
        if (activeChargingEffect != null)
        {
            Destroy(activeChargingEffect);
            activeChargingEffect = null;
        }

        FireProjectile(currentTarget.position);
        isChargingProjectile = false;
    }

    
    private void StartProjectileAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger(projectileConfig.fireAnimationTrigger);
        }
    }
    
    
    private void Update()
    {

        UpdateMovementAndRotation();
        UpdateAttackTimer();
    }
    
    private void FireProjectile(Vector3 targetPosition)
    {
        if (projectileConfig.projectilePrefab == null)
        {
            Debug.LogWarning("Projectile prefab is not set!");
            return;
        }

        Transform spawnPoint = projectileConfig.spawnPoint != null ? projectileConfig.spawnPoint : attackPoint;
        
        GameObject projectile = Instantiate(projectileConfig.projectilePrefab, spawnPoint.position, Quaternion.identity);
        
        RFX1_TransformMotion projectileMotion = projectile.GetComponentInChildren<RFX1_TransformMotion>();
        if (projectileMotion != null)
        {
            projectileMotion.Distance = projectileConfig.maxDistance;
            projectileMotion.Speed = projectileConfig.speed;
            projectileMotion.Initialize(targetPosition, true);
        }
        else
        {
            Debug.LogWarning("Projectile prefab does not have RFX1_TransformMotion component!");
            Destroy(projectile);
        }
    }
    
    private void OnDisable()
    {
        // 차징 이펙트 정리
        if (activeChargingEffect != null)
        {
            Destroy(activeChargingEffect);
            activeChargingEffect = null;
        }
    }


    private void UpdateMovementAndRotation()
    {
        float distanceToServerPos = Vector3.Distance(transform.position, currentServerPosition);
        Vector3 directionToTarget = currentServerPosition - transform.position;

        if (directionToTarget != Vector3.zero)
        {
            // 목표 회전 각도 계산
            targetYAngle = Quaternion.LookRotation(directionToTarget).eulerAngles.y;
            
            // 현재 각도와 목표 각도의 차이 계산 (항상 최단 경로로 회전)
            float angleDifference = Mathf.DeltaAngle(currentYAngle, targetYAngle);
            
            if (Mathf.Abs(angleDifference) > rotationThreshold)
            {
                // 부드러운 회전 속도 계산
                float targetRotationSpeed = Mathf.Sign(angleDifference) * Mathf.Min(Mathf.Abs(angleDifference), maxRotationSpeed);
                currentRotationVelocity = Mathf.MoveTowards(
                    currentRotationVelocity,
                    targetRotationSpeed,
                    rotationAcceleration * Time.deltaTime
                );

                // 회전 적용
                currentYAngle = Mathf.MoveTowardsAngle(
                    currentYAngle,
                    targetYAngle,
                    Mathf.Abs(currentRotationVelocity) * Time.deltaTime
                );

                // 회전 애니메이션 제어
                bool isSignificantRotation = Mathf.Abs(angleDifference) > rotationAnimThreshold;
                if (isSignificantRotation)
                {
                    animator.SetBool(IsTurningLeft, angleDifference > 0);
                    animator.SetBool(IsTurningRight, angleDifference < 0);
                    animator.SetBool(IsMoving, false);
                }
                else
                {
                    animator.SetBool(IsTurningLeft, false);
                    animator.SetBool(IsTurningRight, false);
                }

                // 실제 회전 적용
                transform.rotation = Quaternion.Euler(0, currentYAngle, 0);
            }
            else
            {
                // 회전이 완료되면 회전 애니메이션 중지
                animator.SetBool(IsTurningLeft, false);
                animator.SetBool(IsTurningRight, false);
                currentRotationVelocity = 0f;
            }

            // 이동 처리
            if (distanceToServerPos > moveThreshold)
            {
                // 큰 회전 중에는 이동하지 않음
                bool canMove = Mathf.Abs(Mathf.DeltaAngle(currentYAngle, targetYAngle)) < 45f;
                
                if (canMove)
                {
                    transform.position = Vector3.Lerp(
                        transform.position, 
                        currentServerPosition, 
                        positionLerpSpeed * Time.deltaTime
                    );
                    animator.SetBool(IsMoving, true);
                }
                else
                {
                    animator.SetBool(IsMoving, false);
                }
            }
            else
            {
                animator.SetBool(IsMoving, false);
            }
        }
        else
        {
            // 모든 이동/회전 애니메이션 중지
            animator.SetBool(IsMoving, false);
            animator.SetBool(IsTurningLeft, false);
            animator.SetBool(IsTurningRight, false);
            currentRotationVelocity = 0f;
        }
    }

    private void UpdateAttackTimer()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0 && animator != null)
            {
                //animator.SetBool(IsAttacking, false);
            }
        }
    }

    public void Initialize(int id)
    {
        monsterId = id;
        hasTarget = false;
        currentServerPosition = transform.position;
        currentYAngle = transform.eulerAngles.y;
        currentRotationVelocity = 0f;
    
        
        if (animator != null)
        {
            animator.SetBool(IsMoving, false);
            animator.SetBool(IsTurningLeft, false);
            animator.SetBool(IsTurningRight, false);
            animator.SetBool(IsAttacking, false);
        }
    }
    

    public void UpdatePosition(Vector3 newPosition)
    {
        currentServerPosition = newPosition;
    }

    public void UpdateTarget(string targetPlayerId, bool hasTargetFlag)
    {
        
        Debug.LogWarning("UpdateTarget");
        currentTargetPlayerId = targetPlayerId;
        hasTarget = hasTargetFlag;
      

        if (hasTarget)
        {
            Transform targetTransform = PlayerController.Instance.GetPlayerTransform(targetPlayerId);
            if (targetTransform != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
                if (distanceToTarget <= attackRange && attackTimer <= 0)
                {
                    //PerformAttack();
                }
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetBool(IsAttacking, false);
            }
            attackTimer = 0;
        }
    }

    private void PerformAttack()
    {
        if (animator != null)
        {
            animator.SetBool(IsAttacking, true);
        }
        attackTimer = attackDuration;
    }
    
    public void PlaySound(string soundId)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(soundId, transform.position);
        }
    }
    
    
    public void OnAttackStart()
    {
        isAttacking = true;
    }

    public void OnAttackEnd()
    {
        isAttacking = false;
        
        
        
        // 차징 이펙트 정리
        if (activeChargingEffect != null)
        {
            Destroy(activeChargingEffect);
            activeChargingEffect = null;
        }
    }

    
    
}
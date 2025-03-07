using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game;
using Random = UnityEngine.Random;

[System.Serializable]
public class MonsterAttackEffect
{
    public GameObject effectPrefab;
    public float duration = 2f;
}

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
    public GameObject chargingEffect;
}

public class MonsterController : MonoBehaviour
{
    #region State Enums
    private enum AttackState
    {
        None,
        MeleeAttack,
        RangedAttack,
        MeteorAttack
    }
    #endregion

    #region Component References
    private Animator animator;
    private AudioSource audioSource;
    private MonsterAnimationEffect animationEffect;
    #endregion

    #region Core Properties
    private int monsterId;
    private Vector3 currentServerPosition;
    private string currentTargetPlayerId;
    private bool hasTarget;
    private Transform currentTarget;
    private AttackState currentAttackState = AttackState.None;
    #endregion

    #region Configuration Settings
    [Header("Attack Configs")]
    public MonsterAttackConfig meleeAttackConfig1;  // 기본 근접 공격
    public MonsterAttackConfig meleeAttackConfig2;  // 강력한 근접 공격
    public MonsterAttackConfig meleeAttackConfig3;  // 빠른 근접 공격
    public MonsterAttackConfig rangedAttackConfig; 

    [Header("Projectile Settings")]
    public ProjectileConfig projectileConfig;

    [Header("Rotation Settings")]
    [SerializeField] private float minRotationSpeed = 0.5f;
    [SerializeField] private float rotationThreshold = 5f;
    [SerializeField] private float maxRotationSpeed = 2f;

    [Header("Movement Settings")]
    [SerializeField] private float moveThreshold = 0.01f;
    [SerializeField] private float positionLerpSpeed = 15f;
    [SerializeField] private float moveStopDelay = 0.5f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 추가: 이동 보간 커브
    private Vector3 previousPosition;
    private float currentMoveSpeed;
    private float targetMoveSpeed;
    private float moveSpeedVelocity;
    
    [SerializeField] private float baseMovementSpeed = 15f;        // 기본 이동 속도
    [SerializeField] private float maxMovementSpeed = 20f;         // 최대 이동 속도
    [SerializeField] private float accelerationTime = 0.3f;        // 가속 시간
    private Vector3 currentVelocity;
    private float currentSpeed;
    
    [Header("Stats")]
    public float maxHealth = 50f;
    private float currentHealth;

    [Header("Combat References")]
    public Transform attackPoint;
    public MonsterAttackEffect attackEffect;
    public MeteorStrikeController meteorStrikeController;

    [Header("MeleeDamageFields")]
    [SerializeField] private DamageField meleeAttackField1;
    [SerializeField] private DamageField meleeAttackField2;
    [SerializeField] private DamageField meleeAttackField3;
    [Header("HitFX")]
    public GameObject MeleeHitVfx;
    public string MeleeHitSfx;
    #endregion

    #region State Variables
    private bool isChargingProjectile;
    private GameObject activeChargingEffect;
    private bool useRootMotion = false;
    private bool wasRootMotionEnabled = false;
    private float currentRotationVelocity;
    private float targetYAngle;
    private bool isRotating = false;
    private float currentRotation;
    private float targetRotation;
    private float rotationStartTime;
    private float rotationDuration;
    private Coroutine currentRotationCoroutine;
    private bool isMovingCommand = false;
    private float lastMoveTime;
    private bool isAttacking = false;
    #endregion

    #region Animation Hash IDs
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsTurningLeft = Animator.StringToHash("IsTurningLeft");
    private static readonly int IsTurningRight = Animator.StringToHash("IsTurningRight");
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int MovementSpeed = Animator.StringToHash("MovementSpeed");
    #endregion
    
    
    [SerializeField]
    private string[] footstepSoundIds = new string[] 
    { 
        "footstep_1", 
        "footstep_2", 
        "footstep_3", 
        "footstep_4" 
    };



    #region Unity Lifecycle Methods
    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
        audioSource = gameObject.AddComponent<AudioSource>();
        animationEffect = GetComponent<MonsterAnimationEffect>();
        previousPosition = transform.position;
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        UpdateMovementAndRotation();
    }

    private void OnDisable()
    {
        CleanupEffects();
    }

    private void OnAnimatorMove()
    {
        if (!useRootMotion) return;

        if (animator && isRotating)
        {
            transform.rotation *= animator.deltaRotation;
        }
    }

  

    #endregion

    #region Initialization
    public void Initialize(int id)
    {
        monsterId = id;
        hasTarget = false;
        currentServerPosition = transform.position;
        currentRotationVelocity = 0f;
        ResetAnimatorState();
    }

    private void ResetAnimatorState()
    {
        if (animator != null)
        {
            animator.SetBool(IsMoving, false);
            animator.SetBool(IsTurningLeft, false);
            animator.SetBool(IsTurningRight, false);
            animator.SetBool(IsAttacking, false);
        }
    }
    #endregion

    #region Movement and Rotation
    private void UpdateMovementAndRotation()
    {
        float distanceToServerPos = Vector3.Distance(transform.position, currentServerPosition);

        if (distanceToServerPos > moveThreshold)
        {
            float baseSpeed = 2f;
        
            // 거리에 따른 속도 변화를 최소화하되 유지
            float speedMultiplier = Mathf.Clamp(distanceToServerPos / 30f, 1f, 1.05f);
        
            float currentLerpSpeed = baseSpeed * speedMultiplier;

            // 실제 이동 속도 계산
            Vector3 moveDirection = (currentServerPosition - transform.position).normalized;
            Vector3 previousPos = transform.position;
        
            transform.position = Vector3.Lerp(
                transform.position,
                currentServerPosition,
                currentLerpSpeed * Time.deltaTime
            );

            // 실제 이동 거리 기반으로 애니메이션 속도 계산
            float actualSpeed = Vector3.Distance(previousPos, transform.position) / Time.deltaTime;
            float normalizedSpeed = Mathf.Lerp(0.5f, 1f, actualSpeed / (baseSpeed * 1.05f));
            // 0.5 ~ 1 범위로 정규화된 애니메이션 속도

            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    Quaternion.LookRotation(moveDirection),
                    currentLerpSpeed * Time.deltaTime * 5f
                );
            }

            animator?.SetFloat("MovementSpeed", normalizedSpeed);

            lastMoveTime = Time.time;
            isMovingCommand = true;
        }
        else if (isMovingCommand)
        {
            if (Time.time - lastMoveTime > moveStopDelay)
            {
                isMovingCommand = false;
                animator?.SetBool(IsMoving, false);
                animator?.SetFloat("MovementSpeed", 0f);
            }
        }

        UpdateMovementAnimation();
    }
    

    private void UpdateMovementAnimation()
    {

        
            
        if (animator != null && isMovingCommand && !isAttacking)
        {
            RestoreRootMotionState();
            animator.SetBool(IsMoving, true);
        }
    }

    public void UpdatePosition(Vector3 newPosition)
    {
        currentServerPosition = newPosition;
        isMovingCommand = true;
        lastMoveTime = Time.time;
    }

    public void UpdateRotation(float rotation, float duration)
    {
        if (currentRotationCoroutine != null)
        {
            StopCoroutine(currentRotationCoroutine);
            RestoreRootMotionState();
        }
        currentRotationCoroutine = StartCoroutine(RootMotionRotation(rotation, duration));
    }
    #endregion

    #region Combat
    public void UpdateTarget(string targetPlayerId, bool hasTargetFlag)
    {
        currentTargetPlayerId = targetPlayerId;
        hasTarget = hasTargetFlag;

        if (hasTarget)
        {
            currentTarget = PlayerSpawner.Instance.GetPlayerTransform(targetPlayerId);
        }
    }

    public void PerformAttack(string targetPlayerId, int attackType, float damage)
    {
        if (isAttacking) return;

        Transform targetTransform = PlayerSpawner.Instance.GetPlayerTransform(targetPlayerId);
        currentTarget = targetTransform;
        if (targetTransform == null) return;

        RotateTowardsTarget(targetTransform);
        ExecuteAttack(attackType);
    }

    private void RotateTowardsTarget(Transform target)
    {
        Vector3 directionToTarget = target.position - transform.position;
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
    }

    private void ExecuteAttack(int attackType)
    {
        MonsterAttackConfig currentConfig = null;

        switch (attackType)
        {
            case 0: // MeleeAttackType1
                currentConfig = meleeAttackConfig1;
                break;
            case 1: // MeleeAttackType2
                currentConfig = meleeAttackConfig2;
                break;
            case 2: // MeleeAttackType3
                currentConfig = meleeAttackConfig3;
                break;
            case 3: // RangedAttackType
                currentConfig = rangedAttackConfig;
                break;
        }

        if (currentConfig != null)
        {
            PlayAttackAnimation(currentConfig.animationTrigger);
            SetupAttackEffects(currentConfig);
            PlayAttackSound(currentConfig.sound);
        }
    }
    #endregion

    #region Effects and Animation
    private void PlayAttackAnimation(string trigger)
    {
        if (animator != null)
        {
            animator.SetTrigger(trigger);
        }
    }

    private void SetupAttackEffects(MonsterAttackConfig config)
    {
        if (animationEffect != null)
        {
            animationEffect.SetCurrentTarget(currentTarget);
            animationEffect.SetEffects(config.effects);
        }
    }

    private void PlayAttackSound(AudioClip sound)
    {
        if (audioSource != null && sound != null)
        {
            audioSource.PlayOneShot(sound);
        }
    }

    public void OnStartCharging()
    {
        if (projectileConfig.chargingEffect != null && projectileConfig.spawnPoint != null)
        {
            activeChargingEffect = Instantiate(
                projectileConfig.chargingEffect,
                projectileConfig.spawnPoint.position,
                projectileConfig.spawnPoint.rotation,
                projectileConfig.spawnPoint
            );
        }
        isChargingProjectile = true;
    }

    public void OnFireProjectile()
    {
        if (currentTarget == null) return;

        if (activeChargingEffect != null)
        {
            Destroy(activeChargingEffect);
            activeChargingEffect = null;
        }

        FireProjectile(currentTarget.position);
        isChargingProjectile = false;
    }

    private void FireProjectile(Vector3 targetPosition)
    {
        if (projectileConfig.projectilePrefab == null) return;

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
            Destroy(projectile);
        }
    }
    #endregion

    #region Health System
    public void TakeDamage(DamageMessage msg)
    {
        currentHealth -= msg.amount;
        currentHealth = Mathf.Max(0, currentHealth);

        // 서버에 데미지와 피격 효과 정보 전송
        TcpProtobufClient.Instance.SendMonsterDamage(monsterId, msg.amount, currentHealth, 
            msg.hitPoint, msg.hitNormal, msg.type);
        

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void SetHealth(float health)
    {
        currentHealth = health;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        Destroy(gameObject, 15f);
        StartCoroutine(StartEnd());

    }

    IEnumerator StartEnd()
    {

        yield return new WaitForSeconds(5.0f);
        UIManager.Instance.GameEnd();
    }
    #endregion

    #region Utility Methods
    private void CleanupEffects()
    {
        if (activeChargingEffect != null)
        {
            Destroy(activeChargingEffect);
            activeChargingEffect = null;
        }
        
        if (currentRotationCoroutine != null)
        {
            StopCoroutine(currentRotationCoroutine);
            RestoreRootMotionState();
        }
    }

    public void PlayHitEffect(Vector3 hitPoint, Vector3 hitNormal, DamageType skillType)
    {
        PlayHitSound(hitPoint, skillType);
        SpawnHitEffect(hitPoint, hitNormal, skillType);
    }
    
    public void PlaySound(string soundId)
    {
        SoundManager.Instance?.PlaySound(soundId, transform.position);
    }
    
    private void PlayRandomFootstep()
    {
        int randomIndex = Random.Range(0, footstepSoundIds.Length);
        string selectedSoundId = footstepSoundIds[randomIndex];
        SoundManager.Instance.PlaySound(selectedSoundId, transform.position);
    }

    private void PlayHitSound(Vector3 hitPoint, DamageType skillType)
    {
        switch(skillType)
        {
            case DamageType.melee:
            case DamageType.fire:
                SoundManager.Instance.PlaySound(MeleeHitSfx, hitPoint);
                break;
        }
    }

    private void SpawnHitEffect(Vector3 hitPoint, Vector3 hitNormal, DamageType skillType)
    {
        GameObject effectPrefab = null;
        switch(skillType)
        {
            case DamageType.melee:
            case DamageType.fire:
                effectPrefab = MeleeHitVfx;
                break;
        }
        
        if (effectPrefab != null)
        {
            Quaternion rotation = Quaternion.LookRotation(hitNormal);
            GameObject effect = Instantiate(effectPrefab, hitPoint, rotation);
            Destroy(effect, 2f);
        }
    }
    #endregion

    #region Animation Events
    public void OnAttackStart(string AttackType)
    {
        isAttacking = true;
        switch (AttackType)
        {
            case "MeleeAttack1":
                meleeAttackField1?.StartDamageField();
                break;
            case "MeleeAttack2":
                meleeAttackField2?.StartDamageField();
                break;
            case "MeleeAttack3":
                meleeAttackField3?.StartDamageField();
                break;
        }
    }

    public void OnAttackEnd(string AttackType)
    {
        isAttacking = false;
        switch (AttackType)
        {
            case "MeleeAttack1":
                meleeAttackField1?.DeactivateDamageField();
                break;
            case "MeleeAttack2":
                meleeAttackField2?.DeactivateDamageField();
                break;
            case "MeleeAttack3":
                meleeAttackField3?.DeactivateDamageField();
                break;
            default:
                if (activeChargingEffect != null)
                {
                    Destroy(activeChargingEffect);
                    activeChargingEffect = null;
                }
                break;
        }
    }
    #endregion

    #region Special Attacks
    public void HandleMeteorStrike(MeteorStrike meteorStrike)
    {
        if (isAttacking) return;
        
        if (meteorStrikeController != null)
        {
            Vector3[] positions = meteorStrike.Positions
                .Select(p => new Vector3(p.X, 0, p.Z))
                .ToArray();
                
            meteorStrikeController.StartMeteorStrike(positions);
        }
    }
    #endregion

    #region Root Motion Handling
    private void EnableRotationRootMotion()
    {
        wasRootMotionEnabled = animator.applyRootMotion;
        useRootMotion = true;
        animator.applyRootMotion = true;
    }

    private void RestoreRootMotionState()
    {
        useRootMotion = false;
        animator.applyRootMotion = wasRootMotionEnabled;
        
        animator.SetBool(IsTurningLeft, false);
        animator.SetBool(IsTurningRight, false);
        //animator.SetFloat("RotationSpeed", 0f);
    }
    
    private IEnumerator RootMotionRotation(float targetRotationRad, float duration)
    {
        isRotating = true;
        float startRotation = transform.eulerAngles.y;
        targetRotation = targetRotationRad * Mathf.Rad2Deg;
        float rotationDiff = Mathf.Abs(Mathf.DeltaAngle(startRotation, targetRotation));

        if (rotationDiff >= 25f) // 25도 이상일 때만 애니메이션 회전
        {
            yield return HandleLargeRotation(startRotation, rotationDiff);
        }
        else // 그 외에는 빠른 회전
        {
            yield return HandleSmallRotation(startRotation, duration);
        }

        RestoreRootMotionState();
        currentRotationCoroutine = null;
        isRotating = false;
    }

    private IEnumerator HandleLargeRotation(float startRotation, float rotationDiff)
    {
        EnableRotationRootMotion();
    
        float angleDiff = Mathf.DeltaAngle(startRotation, targetRotation);
        float normalizedRotation = rotationDiff / 180f;
    
        // 회전 속도를 0.5~1.5 범위로 유지
        float animationSpeed = Mathf.Lerp(0.5f, 1.5f, normalizedRotation) * maxRotationSpeed;
    
        animator.SetBool(IsTurningLeft, angleDiff > 0);
        animator.SetBool(IsTurningRight, angleDiff < 0);
        animator.SetFloat("RotationSpeed", animationSpeed);

        while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetRotation)) > 0.1f)
        {
            yield return null;
        }
    }

    private IEnumerator HandleSmallRotation(float startRotation, float duration)
    {
        float elapsedTime = 0f;
        float rotationDiff = Mathf.Abs(Mathf.DeltaAngle(startRotation, targetRotation));
        float rotationSpeed = rotationDiff / duration;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            t = t * t * t * (t * (6f * t - 15f) + 10f);
            
            float currentAngle = Mathf.LerpAngle(startRotation, targetRotation, t);
            transform.rotation = Quaternion.Euler(0f, currentAngle, 0f);
            
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0f, targetRotation, 0f);
    }
    #endregion
}
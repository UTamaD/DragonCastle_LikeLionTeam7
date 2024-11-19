using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    #region Public Variable

    [Header("Player")] 
    public int Combo = 3;
    
    [Space(10)]
    public float MoveSpeed = 2.0f;
    public float SprintSpeed = 5.335f;
    
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Space(10)]
    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;

    [Space(10)]
    public float JumpTimeout = 0.50f;
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;
    
    #endregion

    // player
    public float _verticalVelocity;
    private float _terminalVelocity = 53.0f;
    
    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animation IDs
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    
    public StateMachine StateMachine { get; private set; }
    public SkillManager SkillManager { get; private set; }
    public LivingEntity LivingEntity { get; private set; }
    public CombatSystem CombatSystem { get; private set; }
    public Player Player { get; private set; }
    public CharacterController Controller  { get; private set; }
    public PlayerInputs Inputs { get; private set; }
    public GameObject MainCamera { get; private set; }
    public Animator Animator { get; private set; }

    // bool 변수
    public bool IsSprint;
    private bool _isInvincible;
    private bool _isMelee;
    private bool _isCombo;

    private void Awake()
    {
        // get a reference to our main camera
        if (MainCamera == null)
        {
            MainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        SkillManager = GetComponent<SkillManager>();
        LivingEntity = GetComponent<LivingEntity>();
        CombatSystem = GetComponent<CombatSystem>();
        Player = GetComponent<Player>();
        Controller = GetComponent<CharacterController>();
        Inputs = GetComponent<PlayerInputs>();
        Animator = GetComponentInChildren<Animator>();

        AssignAnimationIDs();

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
        
        // Input Event
        Inputs.OnIFrameInput += IFrame;
        Inputs.OnMeleeInput += Melee;
        Inputs.OnJumpAttackInput += JumpAttack;
        
        // Living Event
        LivingEntity.OnDamagedEvent += Damaged;
        
        InitStateMachine();
        InitSkills();
    }
    
    private void InitStateMachine()
    {
        StateMachine = new StateMachine(StateName.Move, new MoveState(this));
        StateMachine.AddState(StateName.IFrame, new IFrameState(this));
        StateMachine.AddState(StateName.Melee, new AttackState(this));
        StateMachine.AddState(StateName.Damaged, new DamagedState(this));
    }
    
    private void InitSkills()
    {
        SkillManager.AddSkill(PlayerSkillName.Default, new Melee(this));
        SkillManager.AddSkill(PlayerSkillName.Skill, new JumpAttack(this));
    }
    
    private void AssignAnimationIDs()
    {
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
    }

    private void Update()
    {
        StateMachine?.UpdateState();
        JumpAndGravity();
        GroundedCheck();
    }
    
    private void LateUpdate()
    {
        StateMachine?.LateUpdateState();
    }

    private void FixedUpdate()
    {
        StateMachine?.FixedUpdateState();
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);
        
        Animator.SetBool(_animIDGrounded, Grounded);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;
            
            // Animator.SetBool(_animIDJump, false);
            Animator.SetBool(_animIDFreeFall, false);
            
            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }
        }
        else
        {
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                Animator.SetBool(_animIDFreeFall, true);
            }
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    #region Attack

    private void Melee()
    {
        if (!(StateMachine.CurrentState is MoveState))
            return;

        SkillManager.SetCurrentSkill(PlayerSkillName.Default);
        AttackState state = (AttackState)(StateMachine.GetState(StateName.Melee));
        if (state != null && SkillManager.CurrentSkill.IsAvailable())
        {
            state.SetCurSkill(SkillManager.CurrentSkill);
            StateMachine.ChangeState(StateName.Melee);
        }
    }
    
    private void JumpAttack()
    {
        if (!(StateMachine.CurrentState is MoveState))
            return;

        SkillManager.SetCurrentSkill(PlayerSkillName.Skill);
        AttackState state = (AttackState)(StateMachine.GetState(StateName.Melee));
        if (state != null && SkillManager.CurrentSkill.IsAvailable())
        {
            state.SetCurSkill(SkillManager.CurrentSkill);
            StateMachine.ChangeState(StateName.Melee);
        }
    }

    #endregion

    #region IFrame
    private void IFrame()
    {   
        if (!(StateMachine.CurrentState is MoveState))
            return;
        
        StateMachine.ChangeState(StateName.IFrame);
    }
    
    private void EndIFrame(AnimationEvent animationEvent)
    {
        StateMachine.ChangeState(StateName.Move);
    }

    #endregion

    #region Damaged
    private void Damaged()
    {
        if ((StateMachine.CurrentState is IFrameState))
            return;
        
        StateMachine.ChangeState(StateName.Damaged);
    }
    
    #endregion

    #region coroutineCtrl
    public Coroutine CoroutineStarter(IEnumerator coroutine)
    {
        return StartCoroutine(coroutine);
    }

    public void CoroutineStopper(Coroutine coroutine)
    {
        StopCoroutine(coroutine);
    }
    #endregion
    
    private IEnumerator MoveForDistanceAndTime(float distance, float duration)
    {
        Vector3 direction = transform.forward;  // 이동 방향
        
        // 일정한 속도로 이동하기 위한 거리
        float speed = distance / duration;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // 현재 시간에 따른 이동 거리 계산
            float currentDistance = speed * Time.deltaTime;
            //transform.Translate(direction * currentDistance, Space.World); // 균일한 속도로 이동
            Controller.Move(direction * currentDistance); // 균일한 속도로 이동

            elapsedTime += Time.deltaTime; // 경과 시간 증가
            yield return null; // 다음 프레임까지 대기
        }
    }

    #region Debug

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }

    #endregion

    #region Animation Event

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(Controller.center), FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(Controller.center), FootstepAudioVolume);
        }
    }

    private void OnDash(AnimationEvent animationEvent)
    {
        object info = animationEvent.objectReferenceParameter;
        DashInfo dashInfo = info.ConvertTo<DashInfo>();
        if (!dashInfo)
        {
            Debug.LogWarning("fail to object type cast to " + dashInfo.name);
            return;
        }

        StartCoroutine(MoveForDistanceAndTime(dashInfo.dashDis, dashInfo.dashTime));
    }
    
    private void OnEndMelee()
    {
        StateMachine.ChangeState(StateName.Move);
    }

    private void OnEndDamaged()
    {
        StateMachine.ChangeState(StateName.Move);
    }

    private void OnCreateDamageField(AnimationEvent animationEvent)
    {
        object info = animationEvent.objectReferenceParameter;
        SkillInfo skillInfo = info.ConvertTo<SkillInfo>();
        if (!skillInfo)
        {
            Debug.LogWarning("fail to object type cast to " + skillInfo.name);
            return;
        }
        
        CombatSystem.ActiveDamageField(skillInfo.skillName, skillInfo.dist);
    }

    #endregion
}

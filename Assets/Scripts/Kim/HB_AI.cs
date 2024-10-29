using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class HB_AI : MonoBehaviour
{
    [SerializeField] private Transform target;
    private NavMeshAgent _naveMeshAgent;

    
    [FormerlySerializedAs("chaseRange")]
    [Header("Range")]
    [SerializeField] 
    private float _detectRange = 10f;
    [SerializeField] 
    private float _meleeAttackRange = 5.0f;

    
    
    [Header("MoveSpeed")] 
    [SerializeField] private float moveSpeed = 10.0f;
    
    private Vector3 _originPos = default;
    private BehaviorTreeRunner _BTRunner = null;
    private Transform _detectedPlayer = null;
    private Animator _animator = null;

    private const string _ATTACK_ANIM_STATE_NAME = "Attack";
    private const string _STTACK_ANIM_TRIGGER_NAME = "attack";
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        
        _BTRunner = new BehaviorTreeRunner(SettingBT());

        _originPos = transform.position;
    }

    private void FixedUpdate()
    {
        _BTRunner.Operate();
    }

    INode SettingBT()
    {
        return new SelectorNode(new List<INode>()
            {
                new SequenceNode
                (
                    new List<INode>()
                    {
                        new ActionNode(CheckMeleeAttacking),
                        new ActionNode(CheckEnemyWithMeleeAttackRange),
                        new ActionNode(DoMeleeAttack),
                    }
                ),
                new SelectorNode
                (
                    new List<INode>()
                    {
                        new ActionNode(CheckDetectEnemy),
                        new ActionNode(MoveToDetectEnemy),
                    }
                ),
                new ActionNode(MoveToOriginPosition)
            }
        );
    }

    bool IsAnimationRunning(string stateName)
    {
        if (_animator != null)
        {
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            {
                var normalizedTime = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

                return normalizedTime != 0 && normalizedTime < 1.0f;
            }
        }

        return false;
    }

    #region Attack Node

    INode.ENodeState CheckMeleeAttacking()
    {
        if (IsAnimationRunning(_ATTACK_ANIM_STATE_NAME))
        {
            return INode.ENodeState.ENS_Running;
        }

        return INode.ENodeState.ENS_Success;
    }

    INode.ENodeState CheckEnemyWithMeleeAttackRange()
    {
        if (_detectedPlayer != null)
        {
            if (Vector3.SqrMagnitude(_detectedPlayer.position - transform.position) <
                (_meleeAttackRange * _meleeAttackRange))
            {
                return INode.ENodeState.ENS_Success;
            }
        }

        return INode.ENodeState.ENS_Failure;
    }

    INode.ENodeState DoMeleeAttack()
    {
        if (_detectedPlayer != null)
        {
            _animator.SetTrigger(_STTACK_ANIM_TRIGGER_NAME);
            return INode.ENodeState.ENS_Success;
        }

        return INode.ENodeState.ENS_Failure;
    }

    #endregion

    #region Detect & Move Node
    INode.ENodeState CheckDetectEnemy()
    {
        var overlapColliders = Physics.OverlapSphere(transform.position, _detectRange, LayerMask.GetMask("Player"));
        
        if (overlapColliders != null & overlapColliders.Length > 0)
        {
            float closestDistance = Mathf.Infinity;

            foreach (var collider in overlapColliders)
            {
                float distance = Vector3.SqrMagnitude(transform.position - collider.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    _detectedPlayer = collider.transform;
                }
            }
            
            Debug.Log("Player Detected : " + _detectedPlayer);
            return INode.ENodeState.ENS_Success;
        }

        //_detectedPlayer = null;
        Debug.Log("There's no detectedPlayer");
        return INode.ENodeState.ENS_Failure;
    }

    INode.ENodeState MoveToDetectEnemy()
    {
        
        if (_detectedPlayer != null)
        {
            Debug.Log("Move To Player");
            if (Vector3.SqrMagnitude(_detectedPlayer.position - transform.position) < _meleeAttackRange)
            {
                Debug.Log("Success");
                return INode.ENodeState.ENS_Success;
            }
            
            transform.position = Vector3.MoveTowards(transform.position, _detectedPlayer.position, Time.deltaTime * moveSpeed);
            return INode.ENodeState.ENS_Running;
        }

        return INode.ENodeState.ENS_Failure;
    }
    #endregion

    #region Move Origin Pos Node
    INode.ENodeState MoveToOriginPosition()
    {
        if (Vector3.SqrMagnitude(_originPos - transform.position) < float.Epsilon * float.Epsilon)
        {
            return INode.ENodeState.ENS_Success;
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, _originPos, Time.deltaTime * moveSpeed);
            return INode.ENodeState.ENS_Running;
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(this.transform.position, _detectRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(this.transform.position, _meleeAttackRange);
    }

}


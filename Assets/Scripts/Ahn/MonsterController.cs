using System.Collections.Generic;
using UnityEngine;
using Game;


public class MonsterController : MonoBehaviour
{
    public int monsterId;
    private float x, z;
    public Transform target; // 추적할 타겟 플레이어
    private string targetPlayerId;
    private bool hasTarget;

    
    private float attackRange = 2f; // 공격 범위
    private float chaseSpeed = 3f; // 추적 속도
    private float patrolSpeed = 2f;

    
    private Dictionary<string, Transform> players = new Dictionary<string, Transform>();
    private void Start()
    {
        
    }

    
    private void UpdateTargetVisuals()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = hasTarget ? Color.red : Color.white;
        }
    }
    
    public void SetTarget(MonsterTarget targetMsg)
    {
        Debug.Log("SetTarget Ent");
        if (targetMsg.MonsterId != monsterId) return;
        
        Debug.Log("SetTarget 2");
        hasTarget = targetMsg.HasTarget;
        targetPlayerId = targetMsg.TargetPlayerId;
        
        if (hasTarget && !string.IsNullOrEmpty(targetPlayerId))
        {
            // PlayerController에서 해당 ID의 플레이어 찾기
            target = PlayerController.Instance.GetPlayerTransform(targetPlayerId);
            Debug.Log("SetTarget 3");
        }
        else
        {
            target = null;
            Debug.Log("SetTarget 4");
        }
        Debug.Log("SetTarget 5");
        // 타겟이 변경되었을 때의 시각적 피드백 (선택사항)
        //UpdateTargetVisuals();
    }
    
    
    private void UpdateVisuals()
    {
        // 타겟을 향해 바라보기
        if (target != null)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
    
    public void Spawn(SpawnMonster spawnMsg)
    {
        // 서버에서 받은 스폰 메시지를 처리하여 몬스터 오브젝트 생성
        monsterId = (int)spawnMsg.MonsterId;
        x = spawnMsg.X;
        z = spawnMsg.Z;
        transform.position = new Vector3(x, 0f, z);
    }

    public void Move(MoveMonster moveMsg)
    {
        // 서버에서 받은 이동 메시지를 처리하여 몬스터 오브젝트 이동
       // Debug.Log("X : " + moveMsg.X + " Z : " + moveMsg.Z);
        x = moveMsg.X;
        z = moveMsg.Z;
        transform.position = new Vector3(x, 0f, z);
    }

    private void Update()
    {
        // 몬스터 AI 업데이트
        UpdateAI();
    }
    
    
    private void UpdateAI()
    {
        if (hasTarget && target != null)
        {
            //UpdateVisuals();
            //Chase();
        }
    }
    


    private void Attack()
    {
        // 공격 로직 구현
    }

    private void Chase()
    {
        // 추적 로직 구현
        transform.position = Vector3.MoveTowards(transform.position, target.position, chaseSpeed * Time.deltaTime);
    }

    private void Patrol()
    {
        // 순찰 로직 구현
        transform.position = Vector3.MoveTowards(transform.position, target.position, chaseSpeed * Time.deltaTime);
    }


}
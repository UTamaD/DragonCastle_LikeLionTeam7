using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class BossInCombat : Conditional
{
    // 전투 상태를 저장하는 정적 변수
    private static bool isInCombat = false;

    // 보스의 체력
    public SharedFloat bossHealth;

    // 플레이어의 태그
    public string playerTag = "Player";

    // 보스의 시야각
    public float fieldOfViewAngle = 90f;

    // 시야 거리
    public float viewDistance = 20f;

    public override void OnStart()
    {
        // 게임 시작 시 전투 상태 초기화
        isInCombat = false;
    }

    public override TaskStatus OnUpdate()
    {
        // 이미 전투 중이면 Success 반환
        if (isInCombat)
        {
            return TaskStatus.Success;
        }

        // 보스가 데미지를 받았는지 확인
        if (HasTakenDamage())
        {
            isInCombat = true;
            return TaskStatus.Success;
        }

        // 플레이어가 보스의 시야에 들어왔는지 확인
        if (IsPlayerInSight())
        {
            isInCombat = true;
            return TaskStatus.Success;
        }

        // 전투 중이 아니면 Failure 반환
        return TaskStatus.Failure;
    }

    private bool HasTakenDamage()
    {
        // bossHealth가 최대 체력보다 작으면 데미지를 받은 것으로 간주
        return bossHealth.Value < bossHealth.Value;
    }

    private bool IsPlayerInSight()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return false;

        Vector3 directionToPlayer = player.transform.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle <= fieldOfViewAngle * 0.5f)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, directionToPlayer, out hit, viewDistance))
            {
                if (hit.collider.gameObject == player)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // 게임 재시작 시 호출할 정적 메서드
    public static void ResetCombatState()
    {
        isInCombat = false;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using Unity.VisualScripting;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameObject monsterPrefab; 
    private MonsterController[] monsters;
    private void OnApplicationQuit()
    { 
        TcpProtobufClient.Instance.SendPlayerLogout(SuperManager.Instance.playerId);
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        while (UnityMainThreadDispatcher.Instance.ExecutionQueue.Count > 0)
        {
            Debug.Log("Queue");
            GameMessage msg = UnityMainThreadDispatcher.Instance.ExecutionQueue.Dequeue();
            if (msg.MessageCase == GameMessage.MessageOneofCase.Chat)
            {
               // 작동
            }
            else if (msg.MessageCase == GameMessage.MessageOneofCase.PlayerPosition)
            {
                PlayerController.Instance.OnOtherPlayerPositionUpdate(msg.PlayerPosition);
            }
            else if (msg.MessageCase == GameMessage.MessageOneofCase.SpawnMyPlayer)
            {
                Vector3 spawnPos = new Vector3(msg.SpawnMyPlayer.X, msg.SpawnMyPlayer.Y, msg.SpawnMyPlayer.Z);
                
                PlayerController.Instance.OnSpawnMyPlayer(spawnPos);
            }
            else if (msg.MessageCase == GameMessage.MessageOneofCase.SpawnOtherPlayer)
            {
                Vector3 spawnPos = new Vector3(msg.SpawnOtherPlayer.X, msg.SpawnOtherPlayer.Y, msg.SpawnOtherPlayer.Z);
                
                Debug.Log(spawnPos);
                
                PlayerController.Instance.SpawnOtherPlayer(msg.SpawnOtherPlayer.PlayerId, spawnPos);
            }
            else if (msg.MessageCase == GameMessage.MessageOneofCase.Logout)
            {
                PlayerController.Instance.DestroyOtherPlayer(msg.Logout.PlayerId);
            }
            else if (msg.MessageCase == GameMessage.MessageOneofCase.PathTest)
            {
                foreach (var pathTestPath in msg.PathTest.Paths)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.position = new Vector3(pathTestPath.X, pathTestPath.Z, pathTestPath.Y);
                    go.transform.localScale = new Vector3(30,30,30);
                }
            }
            else if(msg.MessageCase == GameMessage.MessageOneofCase.SpawnMonster)
            {
                Debug.Log("SpawnMonster");
                SpawnMonster(msg.SpawnMonster);
            }
            else if(msg.MessageCase == GameMessage.MessageOneofCase.MoveMonster)
            {
                Debug.Log("MoveMonster");
                MoveMonster(msg.MoveMonster);
            }
            else if(msg.MessageCase == GameMessage.MessageOneofCase.MonsterTarget)
            {
                Debug.Log("MonsterTarget");
                UpdateMonsterTarget(msg.MonsterTarget);
            }
        }
    }
    private void SpawnMonster(SpawnMonster spawnMsg)
    {
        // 몬스터 오브젝트 생성 및 MonsterController 할당
        GameObject monsterGO = Instantiate(monsterPrefab, Vector3.zero, Quaternion.identity);
        MonsterController monsterController = monsterGO.AddComponent<MonsterController>();
        monsterController.Spawn(spawnMsg);
    }
    
    private void UpdateMonsterTarget(MonsterTarget targetMsg)
    {
        // 해당 몬스터를 찾아서 타겟 설정
        MonsterController monster = GetMonsterById(targetMsg.MonsterId);
        
        if (monster != null)
        {
            Debug.Log("setTarget");
            monster.SetTarget(targetMsg);
        }
    }

    
    private void MoveMonster(MoveMonster moveMsg)
    {
        // 기존 몬스터 오브젝트 찾아서 이동 처리
        MonsterController monsterController = GetMonsterById(moveMsg.MonsterId);
        if (monsterController != null)
        {
            monsterController.Move(moveMsg);
        }
    }
    
    private MonsterController GetMonsterById(int monsterId)
    {
        // 씬에서 모든 MonsterController를 찾아서 ID가 일치하는 것을 반환
        MonsterController[] monsters = FindObjectsOfType<MonsterController>();
        return Array.Find(monsters, m => m.monsterId == monsterId);
    }
    
    

}

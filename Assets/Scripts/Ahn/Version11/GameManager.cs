using UnityEngine;
using System.Collections.Generic;
using Game;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameObject monsterPrefab;
    private Dictionary<int, MonsterController> monsters = new Dictionary<int, MonsterController>();

    // NavMesh visualization
    public LineRenderer pathRenderer;
    private List<Vector3> currentPath = new List<Vector3>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        TcpProtobufClient.Instance.SendPlayerLogout(SuperManager.Instance.playerId);
    }

    private void Update()
    {
        ProcessMessageQueue();
    }

    private void ProcessMessageQueue()
    {
        while (UnityMainThreadDispatcher.Instance.ExecutionQueue.Count > 0)
        {
            GameMessage msg = UnityMainThreadDispatcher.Instance.ExecutionQueue.Dequeue();
            ProcessMessage(msg);
        }
    }

    private void ProcessMessage(GameMessage msg)
    {
        Debug.Log("msg : " + msg.MessageCase);
        switch (msg.MessageCase)
        {
            case GameMessage.MessageOneofCase.PlayerPosition:
                PlayerController.Instance.OnOtherPlayerPositionUpdate(msg.PlayerPosition);
                break;
            case GameMessage.MessageOneofCase.SpawnMyPlayer:
                var mySpawnPos = new Vector3(msg.SpawnMyPlayer.X, msg.SpawnMyPlayer.Y, msg.SpawnMyPlayer.Z);
                PlayerController.Instance.OnSpawnMyPlayer(mySpawnPos);
                break;
            case GameMessage.MessageOneofCase.SpawnOtherPlayer:
                var otherSpawnPos = new Vector3(msg.SpawnOtherPlayer.X, msg.SpawnOtherPlayer.Y, msg.SpawnOtherPlayer.Z);
                PlayerController.Instance.SpawnOtherPlayer(msg.SpawnOtherPlayer.PlayerId, otherSpawnPos);
                break;
            case GameMessage.MessageOneofCase.Logout:
                PlayerController.Instance.DestroyOtherPlayer(msg.Logout.PlayerId);
                break;
            case GameMessage.MessageOneofCase.SpawnMonster:
                HandleSpawnMonster(msg.SpawnMonster);
                break;
            case GameMessage.MessageOneofCase.MoveMonster:
                HandleMoveMonster(msg.MoveMonster);
                break;
            case GameMessage.MessageOneofCase.MonsterTarget:
                HandleMonsterTarget(msg.MonsterTarget);
                break;
            case GameMessage.MessageOneofCase.PathTest:
                HandlePathTest(msg.PathTest);
                break;
            case GameMessage.MessageOneofCase.Chat:
                PlayerController.Instance.OnRecevieChatMsg(msg.Chat);
                break;
            case GameMessage.MessageOneofCase.MonsterAttack:
                HandleMonsterAttack(msg.MonsterAttack);
                break;
            case GameMessage.MessageOneofCase.MeteorStrike:
                if (monsters.TryGetValue(msg.MeteorStrike.MonsterId, out MonsterController monster))
                {
                    monster.HandleMeteorStrike(msg.MeteorStrike);
                }
                break;
            case GameMessage.MessageOneofCase.MonsterDamage:
                HandleMonsterDamage(msg.MonsterDamage);
                break;
        }
    }

    private void HandleSpawnMonster(SpawnMonster spawnData)
    {
        Vector3 spawnPosition = new Vector3(spawnData.X, 0, spawnData.Z);
        GameObject monsterObj = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);
        MonsterController controller = monsterObj.GetComponent<MonsterController>();
        controller.Initialize(spawnData.MonsterId);
        monsters[spawnData.MonsterId] = controller;
    }

    private void HandleMoveMonster(MoveMonster moveData)
    {
        if (monsters.TryGetValue(moveData.MonsterId, out MonsterController monster))
        {
            monster.UpdatePosition(new Vector3(moveData.X, 0, moveData.Z));
        }
    }

    private void HandleMonsterTarget(MonsterTarget targetData)
    {
        if (monsters.TryGetValue(targetData.MonsterId, out MonsterController monster))
        {
            monster.UpdateTarget(targetData.TargetPlayerId, targetData.HasTarget);
        }
    }

    private void HandlePathTest(PathTest pathTest)
    {
        currentPath.Clear();
        foreach (var point in pathTest.Paths)
        {
            currentPath.Add(new Vector3(point.X, point.Y, point.Z));
        }

        if (pathRenderer != null && currentPath.Count > 0)
        {
            pathRenderer.positionCount = currentPath.Count;
            pathRenderer.SetPositions(currentPath.ToArray());
        }
    }
    
    private void HandleMonsterAttack(MonsterAttack attackData)
    {
        if (monsters.TryGetValue(attackData.MonsterId, out MonsterController monster))
        {
            monster.PerformAttack(
                attackData.TargetPlayerId,
                attackData.AttackType,  // 공격 타입
                attackData.Damage       // 데미지
            );
        }
    }
    
    private void HandleMonsterDamage(MonsterDamage damageMsg)
    {
        if (monsters.TryGetValue(damageMsg.MonsterId, out MonsterController monster))
        {
            monster.SetHealth(damageMsg.CurrentHp);
        }
    }
}
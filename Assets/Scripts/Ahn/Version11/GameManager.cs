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
                PlayerSpawner.Instance.OnOtherPlayerPositionUpdate(msg.PlayerPosition);
                break;
            case GameMessage.MessageOneofCase.SpawnMyPlayer:
                var mySpawnPos = new Vector3(msg.SpawnMyPlayer.X, msg.SpawnMyPlayer.Y, msg.SpawnMyPlayer.Z);
                PlayerSpawner.Instance.SpawnMyPlayer(mySpawnPos);
                break;
            case GameMessage.MessageOneofCase.SpawnOtherPlayer:
                var otherSpawnPos = new Vector3(msg.SpawnOtherPlayer.X, msg.SpawnOtherPlayer.Y, msg.SpawnOtherPlayer.Z);
                var otherSpawnRot = msg.SpawnOtherPlayer.RotationY;
                PlayerSpawner.Instance.SpawnOtherPlayer(msg.SpawnOtherPlayer.PlayerId, otherSpawnPos, otherSpawnRot);
                break;
            case GameMessage.MessageOneofCase.Logout:
                PlayerSpawner.Instance.DestroyOtherPlayer(msg.Logout.PlayerId);
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
            case GameMessage.MessageOneofCase.PlayerDamage:
                HandlePlayerDamage(msg.PlayerDamage);
                break;
        }
    }
    
    private void HandlePlayerDamage(PlayerDamage playerDamage)
    {
        Debug.Log($"Received damage for player: {playerDamage.PlayerId}");
        Debug.Log($"Current player ID: {SuperManager.Instance.playerId}");
        
        // 내 플레이어가 맞은 경우
        if (playerDamage.PlayerId == SuperManager.Instance.playerId)
        {
            Player myPlayer = PlayerSpawner.Instance.GetMyPlayer();
            PlayerController myPlayerCtrl = PlayerSpawner.Instance.GetMyPlayerController();
            if (myPlayer != null)
            {
                Debug.Log($"Applying {playerDamage.AttackType} damage: {playerDamage.Damage} to player: {myPlayer.PlayerId}");
                myPlayerCtrl.LivingEntity.ApplyDamage(playerDamage.Damage);
            }
        }
        else
        {
            if (PlayerSpawner.Instance.TryGetOtherPlayer(playerDamage.PlayerId, out OtherPlayer otherPlayer))
            {
                Vector3 hitPoint = new Vector3(
                    playerDamage.HitPointX,
                    playerDamage.HitPointY,
                    playerDamage.HitPointZ
                );

                if (EffectManager.Instance != null)
                {
                    // 피격 효과 재생
                    otherPlayer.LivingEntity.ApplyDamage(playerDamage.Damage);
                }
            }
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
using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Game;
using Unity.Collections;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    private Player myPlayer;
    private Dictionary<string, OtherPlayer> _otherPlayers = new();

    public Player MyPlayerTemplate;
    public OtherPlayer OtherPlayerTemplate;

    public CinemachineVirtualCamera cam;
    
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

    void Start()
    {
        
    }
    


    void SpawnMyPlayer( Vector3 spawnPos)
    {
        GameObject SpawnPlayer = Instantiate(MyPlayerTemplate.gameObject, spawnPos, Quaternion.identity);
        myPlayer = SpawnPlayer.GetComponent<Player>();
        cam.Follow = SpawnPlayer.transform.GetChild(0);
        cam.LookAt = SpawnPlayer.transform.GetChild(0);
        myPlayer.Initialize(SuperManager.Instance.playerId);
    }

    public void SpawnOtherPlayer(string playerId, Vector3 spawnPos)
    {
        GameObject spawnPlayer = Instantiate(OtherPlayerTemplate.gameObject, spawnPos, Quaternion.identity);

        OtherPlayer otherPlayer = spawnPlayer.GetComponent<OtherPlayer>();
        Player playerComponent = spawnPlayer.GetComponent<Player>();
        playerComponent.Initialize(playerId);
        
        otherPlayer.transform.position = spawnPos;
        
        var vector3 = otherPlayer.transform.position;
        vector3.y = 1;
        otherPlayer.transform.position = vector3;
        
        Camera otherPlayerCamera = spawnPlayer.GetComponentInChildren<Camera>();
        if (otherPlayerCamera != null)
        {
            otherPlayerCamera.gameObject.SetActive(false);
        }
        
        _otherPlayers.Add(playerId, otherPlayer);
    }
    
    public void DestroyOtherPlayer(string playerId)
    {
        if (_otherPlayers.TryGetValue(playerId, out OtherPlayer otherPlayer))
        {
            Destroy(otherPlayer.gameObject);
            _otherPlayers.Remove(playerId);
        }
    }

    public void OnSpawnMyPlayer( Vector3 spawnPos)
    {
        SpawnMyPlayer(spawnPos);
    }

    public void OnRecevieChatMsg(ChatMessage chatmsg)
    {
       
    }
    
    public Transform GetPlayerTransform(string playerId)
    {
        // 자신의 플레이어인 경우
        if (myPlayer != null && playerId == myPlayer.PlayerId)
        {
            return myPlayer.transform;
        }
        
        // 다른 플레이어인 경우
        if (_otherPlayers.TryGetValue(playerId, out OtherPlayer otherPlayer))
        {
            return otherPlayer.transform;
        }
        
        return null;
    }
    
    public void OnOtherPlayerPositionUpdate(PlayerPosition playerPosition)
    {
        if (_otherPlayers.TryGetValue(
             playerPosition.PlayerId, out OtherPlayer otherPlayer))
        {
            otherPlayer.UpdatePlayerPosition(playerPosition);
        }
    }
    
    
    public Player GetMyPlayer()
    {
        return myPlayer;
    }

    public bool TryGetOtherPlayer(string playerId, out OtherPlayer otherPlayer)
    {
        return _otherPlayers.TryGetValue(playerId, out otherPlayer);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string PlayerId { get; private set; }

    private PlayerController _playerController;
    
    private Vector3 prevMoveForward;
    
    private float lastUpdateTime = 0f;
    private const float UpdateInterval = 0.3f; // 0.3초마다 업데이트
    
    private void Start()
    {
        _playerController = GetComponent<PlayerController>();
    }
    
    public void SendPositionToServer(Vector3 moveForward, float rotation, float speed)
    {
        if (prevMoveForward != moveForward || Time.time - lastUpdateTime >= UpdateInterval && moveForward != Vector3.zero)
        {
            TcpProtobufClient.Instance.SendPlayerPosition(SuperManager.Instance.playerId,
                transform.position.x, transform.position.y, transform.position.z, 
                moveForward.x, moveForward.y, moveForward.z, speed, rotation);
            
            lastUpdateTime = Time.time;
        }
        prevMoveForward = moveForward;
    }
    
    public void Initialize(string playerId)
    {
        PlayerId = playerId; 
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Transform player;
    public Camera playerCamera;
    public Transform bodyObject;
    
    public float Speed = 5.0f;
    public float mouseSensitivity = 1f;
    private float yaw = 0f;
    
    private float lastUpdateTime = 0f;
    private const float UpdateInterval = 0.3f; // 0.3초마다 업데이트

    private Vector3 prevMoveForward;
    
    private Quaternion initRot;
    private void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        initRot = bodyObject.localRotation;
        
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moveForward = CalculateMoveForward();
        ApplyMovement(moveForward);
        UpdateAim();
        UpdateCamera();
        SendPositionToServer(moveForward);
    }
    
    private void ApplyMovement(Vector3 moveForward)
    {
        Vector3 movement = player.TransformDirection(moveForward) * Speed * Time.deltaTime;
        transform.position += movement;
    }

    private void UpdateAim()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;

        yaw += mouseX;

        player.rotation = Quaternion.Euler(0, yaw, 0);
    }
    
    private void UpdateCamera()
    {
        
    }

    private void SendPositionToServer(Vector3 moveForward)
    {
        if (prevMoveForward != moveForward || Time.time - lastUpdateTime >= UpdateInterval)
        {
            Vector3 worldForward = player.TransformDirection(moveForward);
            
            TcpProtobufClient.Instance.SendPlayerPosition(SuperManager.Instance.playerId,
                transform.position.x, transform.position.y, transform.position.z, 
                worldForward.x, worldForward.y, worldForward.z, Speed, yaw);
            
            lastUpdateTime = Time.time;
        }
        prevMoveForward = moveForward;
    }
    
    private Vector3 CalculateMoveForward()
    {
        Vector3 moveForward = Vector3.zero;
        if (Input.GetKey(KeyCode.A)) moveForward += Vector3.left;
        if (Input.GetKey(KeyCode.D)) moveForward += Vector3.right;
        if (Input.GetKey(KeyCode.W)) moveForward += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) moveForward += Vector3.back;
        return moveForward.normalized;
    }
    
    

    private void LateUpdate()
    {
        bodyObject.rotation = Quaternion.Euler(0, player.eulerAngles.y, 0);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed;

    //player
    private float _speed;
    private float _rotationSpeed;
    
    //input
    private Vector2 _moveInput;
    private Vector2 _lookInput;

    private Rigidbody _rigidbody;
    private Transform _cameraFocus;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _cameraFocus = transform.GetChild(0).GetComponent<Transform>();
    }

    private void FixedUpdate()
    {
        Move();
    }
    
    private void Move()
    {
        if (_moveInput == Vector2.zero) return;
        
        _speed = moveSpeed;
        Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

        float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, 
                                            ref _rotationSpeed, Time.deltaTime);
        angle += _cameraFocus.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
        
        Vector3 moveDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        _rigidbody.MovePosition(_rigidbody.position + moveDirection.normalized * (_speed * Time.deltaTime));
    }

    private void Look()
    {
        
    }

    private void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }

    private void OnLook(InputValue value)
    {
        _lookInput = value.Get<Vector2>();
    }
}

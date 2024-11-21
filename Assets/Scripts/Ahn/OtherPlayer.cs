using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using UnityEngine;

public class OtherPlayer : MonoBehaviour
{
    public PlayerController PlayerController { get; private set; }
    public LivingEntity LivingEntity { get; private set; }
    public Animator Animator { get; private set; }
    
    // position
    private Vector3 Destination;
    private Vector3 Direction;
    private float Speed;
    
    private float interpTime = 0.2f;
    private float checkDistance = 1f;
    
    private float rotationY;
    private float _rotationVelocity;
    
    private Coroutine interpDestinationCoroutine = null;

    public string PlayerId { get; private set; }
    public void Initialize(string playerId)
    {
        PlayerId = playerId; 
    }

    private void Awake()
    {
        LivingEntity = GetComponent<LivingEntity>();
        Animator = GetComponent<Animator>();
    }

    void Start()
    {
        LivingEntity.OnDamagedEvent += Damaged;
    }
    
    // Update is called once per frame
    void Update()
    {
        if (interpDestinationCoroutine != null)
            return;
        
        float timeSinceLastUpdate = Time.deltaTime;
        transform.position += Direction * (Speed * timeSinceLastUpdate);
    }

    private void LateUpdate()
    {
        UpdateRotation();
    }

    public void UpdatePlayerPosition(PlayerPosition p)
    {
        Direction = new Vector3(p.Fx, p.Fy, p.Fz);
        Speed = p.Speed;
        
        Destination = new Vector3(p.X, p.Y, p.Z);
        rotationY = p.RotationY;

        float distance = (transform.position - Destination).magnitude;
        if (interpDestinationCoroutine == null &&  distance > checkDistance )
        {
            interpDestinationCoroutine = StartCoroutine(InterpDestination());
        }
    }

    public void SetAnimatorApplyRootMotion(bool rootMotion)
    {
        Animator.applyRootMotion = rootMotion;
    }

    public void SetAnimatorCondition(string animId, int condition)
    {
        Animator.SetInteger(animId, condition);
    }
    
    public void SetAnimatorCondition(string animId, float condition)
    {
        Animator.SetFloat(animId, condition);
    }
    
    public void SetAnimatorCondition(string animId, bool condition)
    {
        Animator.SetBool(animId, condition);
    }
    
    public void SetAnimatorCondition(string animId)
    {
        Animator.SetTrigger(animId);
    }
    
    private void Damaged(Vector3 dir)
    {
        Animator.applyRootMotion = true;
        Animator.SetBool("KnockBack", true);
        Animator.SetTrigger("Damaged");
    }
    
    private void UpdateRotation()
    {
        // float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationY, 
        //     ref _rotationVelocity, PlayerController.RotationSmoothTime);
       
        if(rotationY != 0)
            transform.rotation = Quaternion.Euler(0, rotationY, 0);
    }
    
    IEnumerator InterpDestination()
    {
        Vector3 initPosition = transform.position;
        float lerp = 0.0f;
        while (true)
        {
            if (lerp >= 1.0f)
            {
                lerp = 1.0f;
            }
            
            transform.position = Vector3.Lerp(initPosition, Destination, lerp);
            if (lerp >= 1.0f)
            {
                interpDestinationCoroutine = null;
                yield break;
            }

            lerp += Time.deltaTime / interpTime;
            yield return null;
        }
    }
}
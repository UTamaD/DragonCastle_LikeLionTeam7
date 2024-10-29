using System.Collections;
using System.Collections.Generic;
using Game;
using UnityEngine;

public class OtherPlayer : MonoBehaviour
{
    private float lastUpdateTime;
    private Vector3 Destination;
    private Vector3 Direction;
    private float rotationY;
    private float currentRotationY;
    private float Speed;
    private float interpTime = 0.2f;
    private float checkDistanceTime = 10.0f;
    private float rotationSpeed = 720f; // 초당 회전 속도 (도)
    

    private Coroutine interpDestinationCoroutine = null;

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
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
    }

    public void UpdatePlayerPosition(PlayerPosition p)
    {
        Destination = new Vector3(p.X, p.Y, p.Z);
        Direction = new Vector3(p.Fx, p.Fy, p.Fz);
        Speed = p.Speed;
        rotationY = p.RotationY;

        float distance = (transform.position - Destination).magnitude;
        if (interpDestinationCoroutine == null &&  distance > checkDistanceTime )
        {
            interpDestinationCoroutine = StartCoroutine(InterpDestination());
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if (interpDestinationCoroutine != null)
        {
            UpdateRotation();
            return;
        }
        
        float timeSinceLastUpdate = Time.deltaTime;
        transform.position += Direction * (Speed * timeSinceLastUpdate);
        
        UpdateRotation();
    }
    
    void UpdateRotation()
    {
        // 현재 회전과 목표 회전 사이의 각도 차이 계산
        float angleDifference = Mathf.DeltaAngle(currentRotationY, rotationY);
        
        // 회전 속도에 따라 현재 프레임에서 회전할 수 있는 최대 각도 계산
        float maxRotation = rotationSpeed * Time.deltaTime;
        
        // 부드러운 회전을 위해 SmoothDamp 사용
        currentRotationY = Mathf.MoveTowards(currentRotationY, rotationY, maxRotation);

        // Y축을 기준으로 회전 적용
        transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
    }
}
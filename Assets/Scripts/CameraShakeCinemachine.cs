using UnityEngine;
using Cinemachine;

public class CameraShakeCinemachine : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    private CinemachineBasicMultiChannelPerlin noise;
    private float shakeTimer;

    void Start()
    {
        // Noise 프로필에 접근
        noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    public void ShakeCamera(float intensity, float duration)
    {
        // 쉐이킹 강도 및 지속 시간 설정
        noise.m_AmplitudeGain = intensity;
        shakeTimer = duration;
    }

    void Update()
    {
        // 타이머를 사용해 쉐이킹 종료
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0f)
            {
                noise.m_AmplitudeGain = 0f; // 쉐이킹 종료
            }
        }
    }
}
using UnityEngine;
using Cinemachine;

public class CameraShakeCinemachine : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    private CinemachineBasicMultiChannelPerlin noise;
    private float shakeTimer;
    private float shakeIntensity;
    private float shakeDuration;

    void Start()
    {
        // Noise 프로필에 접근
        noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    public void ShakeCamera(float intensity = 5f, float duration = 0.5f)
    {
        // 쉐이킹 강도 및 지속 시간 설정
        noise.m_AmplitudeGain = intensity;
        shakeIntensity = intensity;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    void Update()
    {
        // 타이머를 사용해 쉐이킹 종료
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            // 시간에 따라 흔들림 강도 감소
            noise.m_AmplitudeGain = Mathf.Lerp(shakeIntensity, 0f, 1 - (shakeTimer / shakeDuration));
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSoundManager : MonoBehaviour
{
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
    
    public AudioClip[] MeleeAudioClips;
    [Range(0, 1)] public float MeleeAudioVolume = 0.5f;
    
    public AudioClip SkillAudioClip;
    [Range(0, 1)] public float SkillAudioVolume = 0.5f;
    
    public AudioClip DashAudioClip;
    [Range(0, 1)] public float DashAudioVolume = 0.5f;
    
    public AudioClip DamagedAudioClip;
    [Range(0, 1)] public float DamagedAudioVolume = 0.5f;
    
    public void PlayFootStep(AnimationEvent animationEvent, Vector3 position)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], position, FootstepAudioVolume);
            }
        }
    }

    public void PlayLand(AnimationEvent animationEvent, Vector3 position)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, position, FootstepAudioVolume);
        }
    }
    
    public void PlayMelee(Vector3 position, int comboNum)
    {
        AudioSource.PlayClipAtPoint(MeleeAudioClips[comboNum], position, MeleeAudioVolume);
    }
    
    public void PlaySkill(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(SkillAudioClip, position, SkillAudioVolume);
    }
    
    public void PlayDash(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(DashAudioClip, position, DashAudioVolume);
    }
    
    public void PlayDamaged(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(DamagedAudioClip, position, DamagedAudioVolume);
    }
}

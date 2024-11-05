using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [System.Serializable]
    public class SoundData
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        public bool loop = false;
    }

    [Header("Sound Settings")]
    public SoundData[] sounds;
    
    // 캐시된 오디오 소스들
    private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();
    // 캐시된 사운드 데이터
    private Dictionary<string, SoundData> soundDatabase = new Dictionary<string, SoundData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSoundDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSoundDatabase()
    {
        soundDatabase.Clear();
        foreach (var sound in sounds)
        {
            if (!string.IsNullOrEmpty(sound.id) && sound.clip != null)
            {
                soundDatabase[sound.id] = sound;
            }
        }
    }

    public void PlaySound(string soundId, Vector3 position)
    {
        if (!soundDatabase.TryGetValue(soundId, out SoundData soundData))
        {
            Debug.LogWarning($"Sound with ID {soundId} not found!");
            return;
        }

        // 기존 오디오 소스가 있는지 확인
        if (!audioSources.TryGetValue(soundId, out AudioSource audioSource))
        {
            // 없으면 새로 생성
            GameObject soundObject = new GameObject($"Sound_{soundId}");
            soundObject.transform.parent = transform;
            audioSource = soundObject.AddComponent<AudioSource>();
            audioSources[soundId] = audioSource;
        }

        // 오디오 소스 설정
        audioSource.clip = soundData.clip;
        audioSource.volume = soundData.volume;
        audioSource.pitch = soundData.pitch;
        audioSource.loop = soundData.loop;
        audioSource.spatialBlend = 1f; // 3D 사운드
        audioSource.transform.position = position;

        audioSource.Play();

        // 루프가 아닌 사운드는 자동 정리
        if (!soundData.loop)
        {
            StartCoroutine(CleanupSound(soundId, soundData.clip.length));
        }
    }

    public void StopSound(string soundId)
    {
        if (audioSources.TryGetValue(soundId, out AudioSource audioSource))
        {
            audioSource.Stop();
        }
    }

    public void StopAllSounds()
    {
        foreach (var audioSource in audioSources.Values)
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
    }

    private System.Collections.IEnumerator CleanupSound(string soundId, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (audioSources.TryGetValue(soundId, out AudioSource audioSource))
        {
            if (!audioSource.isPlaying)
            {
                audioSources.Remove(soundId);
                Destroy(audioSource.gameObject);
            }
        }
    }
}
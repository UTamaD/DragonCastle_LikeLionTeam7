using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 액션 설정을 위한 ScriptableObject
[CreateAssetMenu(fileName = "ActionConfig", menuName = "Monster/ActionConfig")]
public class ActionConfig : ScriptableObject 
{
    public string NodeType;
    public string AnimationState;
    public float BlendTime = 0.2f;
    
    [System.Serializable]
    public class EffectConfig 
    {
        public GameObject Prefab;
        public Transform Socket;
        public float Timing;
    }
    
    public EffectConfig[] Effects;
    public AudioClip SoundEffect;
    public UnityEvent OnActionPoint;
    
    [System.Serializable]
    public class ParameterConfig 
    {
        public string Key;
        public string DefaultValue;
    }
    
    public ParameterConfig[] Parameters;
}
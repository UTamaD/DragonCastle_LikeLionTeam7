using UnityEngine;
using System.Collections.Generic;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }
    
    // 현재 재생 중인 이펙트를 추적하기 위한 딕셔너리
    private Dictionary<string, GameObject> activeEffects = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public GameObject PlayEffect(
        GameObject effectPrefab, 
        Vector3 position, 
        Quaternion rotation, 
        Transform parent = null,
        float duration = 2f)
    {
        
        Debug.Log("PlayEffect EffectManager : " + effectPrefab);
        if (effectPrefab == null) return null;

        // 이펙트의 고유 키 생성 (프리팹 이름 + 위치 기반)
        string effectKey = $"{effectPrefab.name}_{position}";

        // 이미 재생 중인 동일한 이펙트가 있는지 확인
        if (activeEffects.TryGetValue(effectKey, out GameObject existingEffect) && existingEffect != null)
        {
            return existingEffect;
        }

        // 새 이펙트 생성
        GameObject effect = Instantiate(effectPrefab, position, rotation);
        
        // 부모 설정
        if (parent != null)
        {
            effect.transform.SetParent(parent);
        }

        // 활성 이펙트 목록에 추가
        activeEffects[effectKey] = effect;

        // 지정된 시간 후 제거
        Destroy(effect, duration);

        // 딕셔너리에서도 제거
        StartCoroutine(RemoveFromDictionary(effectKey, duration));

        return effect;
    }

    private System.Collections.IEnumerator RemoveFromDictionary(string effectKey, float delay)
    {
        yield return new WaitForSeconds(delay);
        activeEffects.Remove(effectKey);
    }

    private void OnDestroy()
    {
        // 모든 활성 이펙트 정리
        foreach (var effect in activeEffects.Values)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }
        activeEffects.Clear();
    }
}
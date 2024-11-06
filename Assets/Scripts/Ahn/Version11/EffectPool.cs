using UnityEngine;
using System.Collections.Generic;

public class EffectPool : MonoBehaviour
{
    public static EffectPool Instance { get; private set; }

    [System.Serializable]
    public class EffectPoolItem
    {
        public string effectId;
        public GameObject prefab;
        public int poolSize = 10;
        public bool expandable = true;
    }

    public List<EffectPoolItem> effectPrefabs;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, EffectPoolItem> effectDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        effectDictionary = new Dictionary<string, EffectPoolItem>();

        foreach (var item in effectPrefabs)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            effectDictionary[item.effectId] = item;

            for (int i = 0; i < item.poolSize; i++)
            {
                GameObject obj = CreateNewEffect(item.prefab);
                objectPool.Enqueue(obj);
            }

            poolDictionary[item.effectId] = objectPool;
        }
    }

    private GameObject CreateNewEffect(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        return obj;
    }

    public GameObject SpawnEffect(string effectId, Vector3 position, Quaternion rotation, float duration = 2f)
    {
        if (!poolDictionary.ContainsKey(effectId))
        {
            Debug.LogWarning($"Effect pool for ID {effectId} doesn't exist.");
            return null;
        }

        Queue<GameObject> pool = poolDictionary[effectId];
        GameObject obj;

        if (pool.Count == 0 && effectDictionary[effectId].expandable)
        {
            obj = CreateNewEffect(effectDictionary[effectId].prefab);
        }
        else if (pool.Count == 0)
        {
            Debug.LogWarning($"No available effects in pool {effectId}");
            return null;
        }
        else
        {
            obj = pool.Dequeue();
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        StartCoroutine(ReturnToPool(obj, effectId, duration));
        return obj;
    }

    private System.Collections.IEnumerator ReturnToPool(GameObject obj, string effectId, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null && poolDictionary.ContainsKey(effectId))
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            poolDictionary[effectId].Enqueue(obj);
        }
    }
}
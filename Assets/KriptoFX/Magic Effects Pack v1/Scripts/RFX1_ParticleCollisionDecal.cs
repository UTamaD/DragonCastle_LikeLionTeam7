using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // 추가

[ExecuteInEditMode]
public class RFX1_ParticleCollisionDecal : MonoBehaviour
{
    public ParticleSystem DecalParticles;
    public bool IsBilboard;
    public bool InstantiateWhenZeroSpeed;
    public float MaxGroundAngleDeviation = 45;
    public float MinDistanceBetweenDecals = 0.1f;
    public float MinDistanceBetweenSurface = 0.03f;

    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    ParticleSystem.Particle[] particles;

    ParticleSystem initiatorPS;
    List<GameObject> collidedGameObjects = new List<GameObject>();
    private bool needUpdateCollisionDetect;

    void OnEnable()
    {
        collisionEvents.Clear();
        collidedGameObjects.Clear();
        initiatorPS = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[DecalParticles.main.maxParticles];
        if (InstantiateWhenZeroSpeed) needUpdateCollisionDetect = true;
    }

    void OnDisable()
    {
        if (InstantiateWhenZeroSpeed) needUpdateCollisionDetect = false;
    }

    void Update()
    {
        if(needUpdateCollisionDetect) CollisionDetect();
    }

    void CleanDestroyedObjects()
    {
        collidedGameObjects.RemoveAll(obj => obj == null);
    }

    void CollisionDetect()
    {
        // 파괴된 오브젝트 정리
        CleanDestroyedObjects();

        int aliveParticles = 0;
        if (InstantiateWhenZeroSpeed)
            aliveParticles = DecalParticles.GetParticles(particles);

        // 안전하게 리스트 복사하여 순회
        var objectsToProcess = collidedGameObjects.ToList();
        foreach (var collidedGameObject in objectsToProcess)
        {
            if (collidedGameObject != null)  // null 체크 추가
            {
                try
                {
                    OnParticleCollisionManual(collidedGameObject, aliveParticles);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error processing collision for object: {collidedGameObject.name}. Error: {e.Message}");
                }
            }
        }
    }

    private void OnParticleCollisionManual(GameObject other, int aliveParticles = -1)
    {
        if (other == null || initiatorPS == null) return;  // null 체크 추가

        collisionEvents.Clear();
        try
        {
            var aliveEvents = initiatorPS.GetCollisionEvents(other, collisionEvents);
            for (int i = 0; i < aliveEvents; i++)
            {
                var angle = Vector3.Angle(collisionEvents[i].normal, Vector3.up);
                if (angle > MaxGroundAngleDeviation) continue;
                if (InstantiateWhenZeroSpeed)
                {
                    if (collisionEvents[i].velocity.sqrMagnitude > 0.1f) continue;
                    var isNearDistance = false;
                    for (int j = 0; j < aliveParticles; j++)
                    {
                        var distance = Vector3.Distance(collisionEvents[i].intersection, particles[j].position);
                        if (distance < MinDistanceBetweenDecals) isNearDistance = true;
                    }
                    if (isNearDistance) continue;
                }

                if (DecalParticles != null)  // null 체크 추가
                {
                    var emiter = new ParticleSystem.EmitParams();
                    emiter.position = collisionEvents[i].intersection;
                    var rotation = Quaternion.LookRotation(-collisionEvents[i].normal).eulerAngles;
                    rotation.z = Random.Range(0, 360);
                    emiter.rotation3D = rotation;

                    DecalParticles.Emit(emiter, 1);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error in OnParticleCollisionManual: {e.Message}");
        }
    }

    void OnParticleCollision(GameObject other)
    {
        if (other == null) return;  // null 체크 추가

        if (InstantiateWhenZeroSpeed)
        {
            if (!collidedGameObjects.Contains(other))
                collidedGameObjects.Add(other);
        }
        else
        {
            OnParticleCollisionManual(other);
        }
    }
}
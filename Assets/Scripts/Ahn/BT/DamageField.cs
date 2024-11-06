using UnityEngine;
using System.Collections.Generic;

public class DamageField : MonoBehaviour
{
    public enum DamageType
    {
        Normal,
        Fire,
        Ice,
        Electric
    }

    [System.Serializable]
    public class DebuffEffect
    {
        public enum DebuffType
        {
            Slow,
            Stun,
            DoT,
            DefenseReduction
        }

        public DebuffType type;
        public float duration;
        public float intensity;
    }

    [Header("Damage Settings")]
    public float damageAmount = 10f;
    public DamageType damageType = DamageType.Normal;
    public float damageInterval = 0.5f;

    [Header("Knockback Settings")]
    public bool applyKnockback = false;
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.5f;

    [Header("Debuff Settings")]
    public List<DebuffEffect> debuffEffects = new List<DebuffEffect>();

    [Header("Target Settings")]
    public LayerMask targetLayers;

    private BoxCollider damageCollider;
    private float damageTimer;
    private List<IDamageable> damagedTargets = new List<IDamageable>();

    private void Awake()
    {
        damageCollider = GetComponent<BoxCollider>();
        if (damageCollider == null)
        {
            damageCollider = gameObject.AddComponent<BoxCollider>();
        }
        damageCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        damageTimer = 0f;
        damagedTargets.Clear();
    }

    private void Update()
    {
        damageTimer += Time.deltaTime;
        if (damageTimer >= damageInterval)
        {
            ApplyDamageToTargetsInField();
            damageTimer = 0f;
        }
    }

    private void ApplyDamageToTargetsInField()
    {
        Collider[] hitColliders = Physics.OverlapBox(damageCollider.bounds.center, damageCollider.bounds.extents, transform.rotation, targetLayers);

        foreach (var hitCollider in hitColliders)
        {
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                ApplyEffectsToTarget(damageable, hitCollider.transform);
            }
        }

        // Remove targets that are no longer in the damage field
        damagedTargets.RemoveAll(target => 
        {
            if (target is MonoBehaviour mb && mb != null)
            {
                return !damageCollider.bounds.Contains(mb.transform.position);
            }
            return true;
        });
    }

    private void ApplyEffectsToTarget(IDamageable target, Transform targetTransform)
    {
        // Apply damage
        target.TakeDamage(damageAmount, damageType);

        // Apply knockback
        if (applyKnockback)
        {
            IKnockbackable knockbackable = targetTransform.GetComponent<IKnockbackable>();
            if (knockbackable != null)
            {
                Vector3 knockbackDirection = (targetTransform.position - transform.position).normalized;
                knockbackable.ApplyKnockback(knockbackDirection, knockbackForce, knockbackDuration);
            }
        }

        // Apply debuffs
        IDebuffable debuffable = targetTransform.GetComponent<IDebuffable>();
        if (debuffable != null)
        {
            foreach (var debuff in debuffEffects)
            {
                debuffable.ApplyDebuff(debuff.type.ToString(), debuff.duration, debuff.intensity);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (damageCollider != null)
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(damageCollider.center, damageCollider.size);
        }
    }
}

// 필요한 인터페이스 정의
public interface IDamageable
{
    void TakeDamage(float amount, DamageField.DamageType damageType);
}

public interface IKnockbackable
{
    void ApplyKnockback(Vector3 direction, float force, float duration);
}

public interface IDebuffable
{
    void ApplyDebuff(string debuffType, float duration, float intensity);
}
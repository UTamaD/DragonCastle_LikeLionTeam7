using UnityEngine;
using BehaviorDesigner.Runtime;

public class PlayerAttack : MonoBehaviour
{
    public KeyCode attackKey = KeyCode.Q;
    public float attackDamage = 10f;
    public float attackCooldown = 0.5f;
    public Vector3 attackBoxSize = new Vector3(2f, 2f, 2f);
    public Vector3 attackBoxOffset = new Vector3(0f, 0f, 1f);

    private float lastAttackTime;

    void Update()
    {
        if (Input.GetKeyDown(attackKey) && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    void PerformAttack()
    {
        Debug.Log("PerformAttack");
        Vector3 attackPosition = transform.position + transform.TransformDirection(attackBoxOffset);
        Collider[] hitColliders = Physics.OverlapBox(attackPosition, attackBoxSize / 2, transform.rotation, LayerMask.GetMask("Enemy"));

        Debug.Log($"Number of colliders detected: {hitColliders.Length}");

        foreach (var hitCollider in hitColliders)
        {
            Debug.Log($"Collider detected: {hitCollider.name}, Tag: {hitCollider.tag}");
            
            if (hitCollider.CompareTag("Enemy"))
            {
                
                Debug.Log("PerformAttack 3");
                // 보스의 경우 (Behavior Designer 사용)
                BehaviorTree behaviorTree = hitCollider.GetComponent<BehaviorTree>();
                if (behaviorTree != null)
                {
                    SharedInt currentHp = behaviorTree.GetVariable("CurrentHp") as SharedInt;
                    if (currentHp != null)
                    {
                        currentHp.Value -= Mathf.RoundToInt(attackDamage);
                        Debug.Log($"Dealt {attackDamage} damage to boss. Boss HP: {currentHp.Value}");
                    }
                    else
                    {
                        Debug.LogWarning("CurrentHp variable not found in the behavior tree");
                    }
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 attackPosition = transform.position + transform.TransformDirection(attackBoxOffset);
        Gizmos.matrix = Matrix4x4.TRS(attackPosition, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, attackBoxSize);
    }
}
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;


public class SetHealth : EnemyAction
{
    public SharedInt Health;
    public SharedInt CurrentHealth;

    public override TaskStatus OnUpdate()
    {
        CurrentHealth = Health.Value;
        return TaskStatus.Success;
    }
}
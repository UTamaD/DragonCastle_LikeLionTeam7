using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;


    public class IsHealthUnder : EnemyConditional
    {
        public SharedInt HealthTreshold;
        
        public override TaskStatus OnUpdate()
        {
            return CurrentHealth.Value < HealthTreshold.Value ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
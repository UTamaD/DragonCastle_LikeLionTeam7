// Conditions/ActionCondition.cs
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

// 모든 컨디션의 기본 클래스
public abstract class BaseActionCondition : Conditional
{
    protected MonsterController monsterController;

    public override void OnAwake()
    {
        monsterController = GetComponent<MonsterController>();
        if (monsterController == null)
        {
            Debug.LogError($"{GetType().Name} requires MonsterController component");
        }
    }
}


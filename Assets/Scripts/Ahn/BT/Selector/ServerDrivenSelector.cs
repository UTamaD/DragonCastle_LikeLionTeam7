using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections.Generic;
using Game;
using UnityEngine;

public class ServerDrivenSelector : Composite
{
    private bool waitingForServer = true;
    private int currentActionIndex = -1;
    private BehaviorTree behaviorTree;

    public override void OnAwake()
    {
        Debug.Log("[ServerDrivenSelector] OnAwake - Initializing");
        behaviorTree = GetComponent<BehaviorTree>();
        
        if (behaviorTree != null)
        {
            try
            {
                var selector = behaviorTree.GetVariable("CurrentSelector") as SharedServerDrivenSelector;
                if (selector == null)
                {
                    selector = new SharedServerDrivenSelector();
                    behaviorTree.SetVariable("CurrentSelector", selector);
                }
                selector.Value = this;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ServerDrivenSelector] Unable to set CurrentSelector variable: {e.Message}");
            }
        }
    }

    public override TaskStatus OnUpdate()
    {
        Debug.Log($"[ServerDrivenSelector] OnUpdate - WaitingForServer: {waitingForServer}, CurrentAction: {currentActionIndex}");

        if (waitingForServer)
        {
            var waitTask = children[0];
            TaskStatus waitStatus = waitTask.OnUpdate();
            
            if (waitStatus == TaskStatus.Success)
            {
                Debug.Log("[ServerDrivenSelector] WaitForServer completed");
                waitingForServer = false;
                // 여기서 바로 리턴하지 않고 계속 진행
            }
            else
            {
                return waitStatus;
            }
        }

        // 현재 액션이 설정되어 있지 않으면 WaitForServer로 돌아감
        if (currentActionIndex == -1)
        {
            waitingForServer = true;
            return TaskStatus.Running;
        }

        // 현재 액션 실행
        if (currentActionIndex >= 1 && currentActionIndex < children.Count)
        {
            var sequence = children[currentActionIndex] as BehaviorDesigner.Runtime.Tasks.Sequence;
            if (sequence != null)
            {
                Debug.Log($"[ServerDrivenSelector] Executing sequence {currentActionIndex}");
                TaskStatus status = sequence.OnUpdate();
                
                if (status == TaskStatus.Running)
                {
                    return TaskStatus.Running;
                }
                
                // 시퀀스가 완료되면 다시 서버 대기로
                waitingForServer = true;
                currentActionIndex = -1;
                return status;
            }
        }

        // 적절한 액션을 찾지 못했으면 서버 대기로
        waitingForServer = true;
        return TaskStatus.Running;
    }

    public void SetCurrentAction(string actionType)
    {
        Debug.Log($"[ServerDrivenSelector] Setting action to: {actionType}");
        switch (actionType)
        {
            case "action_0":
                currentActionIndex = 1; // Attack sequence
                break;
            case "action_1":
                currentActionIndex = 2; // Chase sequence
                break;
            case "action_2":
                currentActionIndex = 3; // Patrol sequence
                break;
            default:
                currentActionIndex = -1;
                break;
        }
        waitingForServer = false;
    }
}
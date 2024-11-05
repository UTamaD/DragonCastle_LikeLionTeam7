using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

// WaitForServerAction.cs
public class WaitForServerAction : Action
{
	private bool hasRequestedAction = false;
	private bool hasReceivedResponse = false;

	public override void OnStart()
	{
		hasRequestedAction = false;
		hasReceivedResponse = false;
	}

	public override TaskStatus OnUpdate()
	{
		if (!hasRequestedAction)
		{
			// 서버에 액션 요청
			hasRequestedAction = true;
			return TaskStatus.Running;
		}

		if (hasReceivedResponse)
		{
			hasRequestedAction = false;  // 다음 요청을 위해 리셋
			hasReceivedResponse = false;
			return TaskStatus.Success;
		}

		return TaskStatus.Running;
	}

	public void OnServerResponse()
	{
		Debug.Log("[WaitForServerAction] Server response received");
		hasReceivedResponse = true;
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StateName
{
    Move = 0,
    IFrame,
    Melee,
}

public class StateMachine
{
    public BaseState CurrentState { get; private set; }
    
    private Dictionary<StateName, BaseState> _states = new Dictionary<StateName, BaseState>();
    
    public StateMachine(StateName stateName, BaseState state)
    {
        AddState(stateName, state);
        CurrentState = GetState(stateName);
    }

    public void AddState(StateName stateName, BaseState state)
    {
        if (!_states.ContainsKey(stateName))
        {
            _states.Add(stateName, state);
        }
    }

    public BaseState GetState(StateName stateName)
    {
        if (_states.TryGetValue(stateName, out BaseState state))
            return state;
        return null;
    }

    public void DeleteState(StateName stateName)
    {
        if (_states.ContainsKey(stateName))
        {
            _states.Remove(stateName);
        }
    }

    public void ChangeState(StateName stateName)
    {
        CurrentState?.OnExitState();
        if (_states.TryGetValue(stateName, out BaseState newState))
        {
            CurrentState = newState;
        }
        
        CurrentState?.OnEnterState();
    }

    public void UpdateState()
    {
        CurrentState?.OnUpdateState();
    }

    public void LateUpdateState()
    {
        CurrentState?.OnLateUpdateState();
    }

    public void FixedUpdateState()
    {
        CurrentState?.OnFixedUpdateState();
    }
}

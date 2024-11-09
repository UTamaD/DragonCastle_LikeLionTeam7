using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseState
{
    public PlayerController Owner { get; private set; }

    public BaseState(PlayerController owner)
    {
        Owner = owner;
    }

    public abstract void OnEnterState();
    public abstract void OnUpdateState();
    public abstract void OnLateUpdateState();
    public abstract void OnFixedUpdateState();
    public abstract void OnExitState();
}

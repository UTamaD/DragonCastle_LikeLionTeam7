using BehaviorDesigner.Runtime;

[System.Serializable]
public class SharedServerDrivenSelector : SharedVariable<ServerDrivenSelector>
{
    public static implicit operator SharedServerDrivenSelector(ServerDrivenSelector value) 
    { 
        var sharedVariable = new SharedServerDrivenSelector(); 
        sharedVariable.Value = value; 
        return sharedVariable; 
    }
}
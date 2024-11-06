using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ActionLibrary", menuName = "Monster/ActionLibrary")]
public class MonsterActionLibrary : ScriptableObject 
{
    public ActionConfig[] Actions;
    
    private Dictionary<string, ActionConfig> actionLookup;
    
    public ActionConfig GetActionConfig(string nodeType) 
    {
        if (actionLookup == null) 
        {
            actionLookup = Actions.ToDictionary(a => a.NodeType);
        }
        return actionLookup.GetValueOrDefault(nodeType);
    }
}
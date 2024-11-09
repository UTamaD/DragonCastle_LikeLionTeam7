using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Skill Info", menuName = "Scriptable Object/Damage Info", order = int.MaxValue)]
public class SkillInfo : ScriptableObject
{   
    public PlayerSkillName skillName;
    public float dist;
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerSkillName
{
    Default,
    Skill,
    Ultimate,
}

public class SkillManager : MonoBehaviour
{
    public SkillBase CurrentSkill { get; private set; }
    
    private Dictionary<PlayerSkillName, SkillBase> _skills = new Dictionary<PlayerSkillName, SkillBase>();

    public void SetCurrentSkill(PlayerSkillName skillName)
    {
        if (_skills.TryGetValue(skillName, out SkillBase newSkill))
        {
            CurrentSkill = newSkill;
        }
    }
    
    public void AddSkill(PlayerSkillName skillName, SkillBase skill)
    {
        if (!_skills.ContainsKey(skillName))
        {
            _skills.Add(skillName, skill);
        }
    }
    
    public SkillBase GetSkill(PlayerSkillName skillName)
    {
        if (_skills.TryGetValue(skillName, out SkillBase skill))
            return skill;

        return null;
    }
}

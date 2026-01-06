using System;
using UnityEngine;

public class SkillSystem : MonoBehaviour
{
    public Skill PassiveSkill;
    public Skill PassiveSkillInstance;
    public Skill ActiveSkill;
    public Skill ActiveSkillInstance;
    private float LastQTime = -Mathf.Infinity;

    void Start()
    {
        PassiveSkillInstance = Instantiate(PassiveSkill, transform);
        PassiveSkillInstance.transform.localPosition = Vector3.zero;
        PassiveSkillInstance.Init(transform);

        ActiveSkillInstance = Instantiate(ActiveSkill, transform);
        ActiveSkillInstance.transform.localPosition = Vector3.zero;
        ActiveSkillInstance.Init(transform);
    }

    public void UseActiveSkill()
    {
        if (!CanUseSkill(ActiveSkill))
            return;

        LastQTime = Time.time;
        ActiveSkillInstance.Active();
    }

    bool CanUseSkill(Skill skill)
    {
        return Time.time >= LastQTime + skill.Cooldown;
    }
}
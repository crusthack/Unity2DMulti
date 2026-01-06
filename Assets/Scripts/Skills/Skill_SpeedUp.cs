using UnityEngine;

public class Skill_SpeedUp : Skill
{
    public override string SkillName => "SpeedUp";
    public override bool IsPassive => false;
    [SerializeField] private float cooldown = 10f;
    public override float Cooldown => cooldown;
    public float SpeedIncrease = 10f;

    [SerializeField] private float activeDuration = 2f;
    public override float ActiveDuration => activeDuration;
    bool IsActive = false;
    void Update()
    {
        if (IsActive && Time.time >= LastActiveTime + ActiveDuration)
        {
            Owner.Speed -= SpeedIncrease;
            IsActive = false;
            Debug.Log("Speed Up Skill Ended!");
            IsEnabled = false;
        }
    }

    public override void Active()
    {
        Debug.Log("Speed Up Skill Activated!");
        Owner.Speed += SpeedIncrease;
        IsActive = true;
        LastActiveTime = Time.time;
        IsEnabled = true;
    }
}

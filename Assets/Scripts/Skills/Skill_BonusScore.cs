using UnityEngine;

public class Skill_DoubleScore : PassiveSkill
{
    public override string SkillName => "Double Score";
    public override bool IsPassive => true;
    public override float Cooldown => 20f;

    public float Duration = 5f;
    public float ScoreMultiplier = 2f;
    bool IsActive = false;
    public override float ActiveDuration => Duration;


    void Update()
    {
        if (IsActive && Time.time >= LastActiveTime + Duration)
        {
            IsActive = false;
            IsEnabled = false;
            Owner.BonusScoreMultiplier /= ScoreMultiplier;
            Debug.Log("Double Score skill has ended");
        }
        else if(!IsActive && Time.time >= LastActiveTime + Cooldown)
        {
            if (Owner == null)
            {
                Debug.LogError("Skill_DoubleScore: Owner is null");
                return;
            }
            Debug.Log("Double Score skill has enabled");
            Owner.BonusScoreMultiplier *= ScoreMultiplier;
            IsActive = true;
            IsEnabled = true;
            LastActiveTime = Time.time;
        }
    }
}

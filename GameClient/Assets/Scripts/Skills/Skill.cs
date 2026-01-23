using UnityEngine;

public abstract class Skill : MonoBehaviour
{
    public abstract string SkillName { get; }
    public abstract bool IsPassive { get; }
    public abstract float Cooldown { get; }
    public bool IsEnabled;
    public virtual float ActiveDuration => 0f;
    public abstract void Active();
    public float LastActiveTime = -Mathf.Infinity;

    protected Player Owner;
    public void Init(Transform owner)
    {
        Owner = owner.GetComponent<Player>();
    }
}

public abstract class PassiveSkill : Skill
{
    public override bool IsPassive => true;
    public override void Active()
    {
        Debug.Log("This is passive skill");
    }
}
using System.Collections.Generic;
using UnityEngine;

public class Skill_WhiteBox : Skill
{
    public override string SkillName => "WhiteBox";
    public override bool IsPassive => false;
    [SerializeField] private float cooldown = 2f;
    public override float Cooldown => cooldown;

    public WhiteBox BoxProb;
    public int Damage = 50;

    public override void Active()
    {
        Debug.Log("WhileBox Skill Activated!");

        Vector3 spawnPos = Owner.transform.position;
        if (Owner.IsLookingRight)
        {
            spawnPos += new Vector3(3f, 0f, 0f);
        }
        else
        {
            spawnPos += new Vector3(-3f, 0f, 0f);
        }

        var box = Instantiate(
            BoxProb,
            spawnPos,
            Quaternion.identity
        );
        box.SetOwner(Owner);
        box.Damage = Damage;

        Destroy(box.gameObject, .1f);
        LastActiveTime = Time.time;
    }
}
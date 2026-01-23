using UnityEngine;

public class Skill_BlackBullet : PassiveSkill
{
    public override string SkillName => "BlackBullet";
    public override bool IsPassive => true;
    public override float Cooldown => 1f;
    public float BulletSpeed = 5f;
    public float Duration = 5f;

    Transform target;
    public float detectRadius = 10f;
    public LayerMask enemyLayer;
    public BlackBullet BulletProb;
    public override void Active()
    {
        Debug.Log("This is passive skill");
    }

    void Update()
    {
        if (Time.time >= LastActiveTime + Cooldown)
        {
            DetectEnemy();
            if(target != null)
            {
                var dir = (target.position - transform.position).normalized;    
                var bullet = Instantiate(
                    BulletProb,
                    transform.position,
                    Quaternion.FromToRotation(Vector3.right, dir)
                );
                bullet.SetOwner(Owner.GetComponent<Player>());
                bullet.Shoot(transform, dir);
                Destroy(bullet.gameObject, Duration);
                LastActiveTime = Time.time;
            }
        }
    }

    void DetectEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            detectRadius,
            enemyLayer
        );

        if (hits.Length == 0)
        {
            target = null;
            return;
        }

        float closestDist = float.MaxValue;
        Transform closestTarget = null;

        foreach (var hit in hits)
        {
            float dist = Vector2.SqrMagnitude(
                hit.transform.position - transform.position
            );

            if (dist < closestDist)
            {
                closestDist = dist;
                closestTarget = hit.transform;
            }
        }

        target = closestTarget;
    }
}
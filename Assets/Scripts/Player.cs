using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    Vector2 MovDir;
    public float Speed = 5f;
    public bool IsLookingRight = true;
    public float Score = 0;
    SkillSystem Skill;

    void Start()
    {
        Skill = GetComponent<SkillSystem>();
    }

    public void AddScore(float value)
    {
        Score += value * BonusScoreMultiplier;
        Debug.Log("Score updated: " + Score);
    }

    public float BonusScoreMultiplier = 1.0f;

    void FixedUpdate()
    {
        var movement = new Vector3(MovDir.x, MovDir.y, 0) * Speed * Time.fixedDeltaTime;
        transform.Translate(movement, Space.World);
    }

    public void OnMove(InputValue value)
    {
        var movVec = value.Get<Vector2>();
        MovDir = movVec.normalized;
        if (MovDir.x < 0)
        {
            IsLookingRight = false;

        }
        else if (MovDir.x > 0)
        {
            IsLookingRight = true;
        }
    }

    public void OnAttack()
    {
        Skill.UseActiveSkill();
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        switch (collision.gameObject.tag)
        {
            case "Enemy":
                Debug.Log("Ouch!!");
                Score -= 5;
                if (Score < 0) Score = 0;
                break;
        }
    }
}

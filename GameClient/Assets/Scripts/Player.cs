using Protos;
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
    public string CurrentMap = "Map_A";
    public int PrefabID = 0;
    public string UserName;

    public PlayerInput Input;

    void Awake()
    {
        Skill = GetComponent<SkillSystem>();
        Input = GetComponent<PlayerInput>();
        DisableInput();
        PrefabID = GameManager.Instance.SelectedCharacterIndex;
    }

    public void AddScore(float value)
    {
        Score += value * BonusScoreMultiplier;
        Debug.Log("Score updated: " + Score);
    }

    public void Update()
    {
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

    public void DisableInput()
    {
        Input.DeactivateInput();
    }

    public void EnableInput()
    {
        Input.ActivateInput();
    }

    public SyncMessage GetSyncInfo()
    {
        var m = new SyncMessage
        {
            PlayerId = 0,
            PrefabId = PrefabID,
            PositionX = (int)transform.position.x,
            PositionY = (int)transform.position.y,
            MoveX = (int)MovDir.x,  
            MoveY = (int)MovDir.y,
            CurrentMap = CurrentMap,
            UserName = GameManager.Instance.Session.GetUsername(),
            Score = (int)Score,
        };

        return m;
    }

    public void Sync(SyncMessage m)
    {
        transform.position = new Vector3(m.PositionX, m.PositionY);
        MovDir = new Vector2(m.MoveX, m.MoveY);
        CurrentMap = m.CurrentMap;
        Score = m.Score;
    }
}

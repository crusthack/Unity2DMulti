using Protos;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public Vector2 MovDir;
    public float Speed = 5f;
    public bool IsLookingRight = true;
    public float Score = 0;
    SkillSystem Skill;
    public string CurrentMap = "Map_A";
    public int PrefabID = 0;
    public string UserName;

    public PlayerInput Input;

    private Vector3? targetPosition;
    private Rigidbody2D rb;

    Vector3 lastSyncPosition;

    void Awake()
    {
        Skill = GetComponent<SkillSystem>();
        Input = GetComponent<PlayerInput>();
        DisableInput();
        PrefabID = GameManager.Instance.SelectedCharacterIndex;

        rb = GetComponent<Rigidbody2D>();

        Skill = GetComponent<SkillSystem>();
        Input = GetComponent<PlayerInput>();
        targetPosition = null;
    }

    public void AddScore(float value)
    {
        Score += value * BonusScoreMultiplier;
    }

    public float BonusScoreMultiplier = 1.0f;

    void FixedUpdate()
    {
        var movement = new Vector3(MovDir.x, MovDir.y, 0) * Speed * Time.fixedDeltaTime;
        transform.Translate(movement, Space.World);

        if(targetPosition != null)
        {
            transform.position = Vector3.Lerp(transform.position, (Vector3)targetPosition, Time.fixedDeltaTime * 15f);
        }
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

        GameManager.Instance.NetworkCon?.RPC_Move(value);
    }

    public void OnAttack()
    {
        Skill.UseActiveSkill();

        GameManager.Instance.NetworkCon?.RPC_Attack();
    }

    public void Attack()
    {
        Skill.UseActiveSkill();
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        switch (collision.gameObject.tag)
        {
            case "Enemy":
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
            PositionX = transform.position.x,
            PositionY = transform.position.y,
            MoveX = (int)MovDir.x,  
            MoveY = (int)MovDir.y,
            CurrentMap = CurrentMap,
            UserName = UserName,
            Score = (int)Score,
        };

        return m;
    }

    public void Sync(SyncMessage m)
    {
        Vector3 receivedPos = new Vector3(m.PositionX, m.PositionY, 0);
        var sqrdistance = (transform.position - receivedPos).sqrMagnitude;

        if (sqrdistance> 6f) 
        {
            transform.position = receivedPos;
            targetPosition = receivedPos;
        }
        else if (sqrdistance < 1f)
        {
            targetPosition = null;
        }
        else
        {
            targetPosition = receivedPos;
        }

        CurrentMap = m.CurrentMap;
        Score = m.Score;
    }
}

using UnityEngine;

public class BlackBullet : MonoBehaviour
{
    Vector2 Dir = Vector2.zero;
    float Speed = 10f;
    bool HasHit = false;
    Rigidbody2D Rigid;
    Player Owner;

    void Awake()
    {
        Rigid = GetComponent<Rigidbody2D>();
    }

    public void SetOwner(Player owner)
    {
        Owner = owner;
    }

    private void FixedUpdate()
    {
        var movement = new Vector3(Dir.x, Dir.y, 0) * Speed * Time.fixedDeltaTime;
        transform.Translate(movement, Space.World);
    }

    public void Shoot(Transform owner, Vector2 dir)
    {
        this.Dir = dir;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (HasHit) return;

        switch(collision.tag)
        {
            case "Enemy":
                HasHit = true;
                var enemy = collision.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(Owner, 10);
                }
                Destroy(gameObject);
                break;
            case "Wall":
                HasHit = true;
                Destroy(gameObject);
                break;
        }
    }
}

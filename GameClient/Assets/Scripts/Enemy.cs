using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Detection")]
    public float detectRadius = 5f;
    public LayerMask playerLayer;

    [Header("Movement")]
    public float moveSpeed = 3f;

    private Transform target;

    public int HP = 30;
    public int Score = 10;

    private void OnEnable()
    {
        HP = 30;
    }

    public void TakeDamage(Player from, int damage)
    {
        HP -= damage;
        Debug.Log($"Enemy hit! HP: {HP}");

        if (HP <= 0)
        {
            Debug.Log("Enemy dead!");
            from.AddScore(Score);
            Die();
        }
    }

    void Die()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        DetectPlayer();
        ChasePlayer();
    }

    void DetectPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            detectRadius,
            playerLayer
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


    void ChasePlayer()
    {
        if (target == null) return;

        Vector2 dir = (target.position - transform.position).normalized;
        transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
    }


    // Scene 뷰에서 감지 반경 확인용
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            gameObject.SetActive(false);
        }
    }
}



public interface IDamageable
{
    void TakeDamage(Player from, int damage);
}

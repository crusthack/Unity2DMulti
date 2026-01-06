using System.Collections.Generic;
using UnityEngine;

public class WhiteBox : MonoBehaviour
{
    public int Damage = 10;

    private HashSet<IDamageable> HitTargets = new();
    Player Owner;

    public void SetOwner(Player owner)
    {
        Owner = owner;
    }

    void OnEnable()
    {
        HitTargets.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            if (HitTargets.Contains(damageable))
                return;

            HitTargets.Add(damageable);
            damageable.TakeDamage(Owner, Damage);
        }
    }
}
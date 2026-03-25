using UnityEngine;

public class HealPickup : ArenaPickup
{
    [SerializeField] private float healAmount = 30f;

    protected override bool TryApplyPickup(Collider2D other)
    {
        Health health;

        health = other.GetComponent<Health>();

        if (health == null)
        {
            return false;
        }

        health.Heal(healAmount);
        return true;
    }
}

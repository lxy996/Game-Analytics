using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class ArenaPickup : MonoBehaviour
{
    [Header("Pickup Rules")]
    [SerializeField] private bool allowPlayer = true;
    [SerializeField] private bool allowAlly = true;
    [SerializeField] private bool allowEnemy = false;
    [SerializeField] private bool destroyOnPickup = true;

    protected virtual void Reset()
    {
        Collider2D col;

        col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.isTrigger = true; // Automatically set collider as trigger
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Health health;

        if (!CanBePickedBy(other))
        {
            return;
        }

        health = other.GetComponent<Health>();

        if (health == null)
        {
            return;
        }

        if (health.GetIsDead())
        {
            return;
        }

        if (!TryApplyPickup(other))
        {
            return;
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }

    private bool CanBePickedBy(Collider2D other)
    {
        PlayerController playerController;
        AllyController allyController;
        EnemyController enemyController;

        playerController = other.GetComponent<PlayerController>();
        allyController = other.GetComponent<AllyController>();
        enemyController = other.GetComponent<EnemyController>();

        if (playerController != null && allowPlayer)
        {
            return true;
        }

        if (allyController != null && allowAlly)
        {
            return true;
        }

        if (enemyController != null && allowEnemy)
        {
            return true;
        }

        return false;
    }

    // The specific effects are implemented by the Child Class of this script.
    protected abstract bool TryApplyPickup(Collider2D other);
}

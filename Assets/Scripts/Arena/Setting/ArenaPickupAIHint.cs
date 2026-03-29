using UnityEngine;

public enum ArenaPickupAIHintType
{
    Heal,
    StatBuff,
    Weapon
}

public class ArenaPickupAIHint : MonoBehaviour
{
    [SerializeField] private ArenaPickupAIHintType hintType = ArenaPickupAIHintType.StatBuff;
    [SerializeField] private float priority = 1f;

    private ArenaPickup pickup;

    void Awake()
    {
        pickup = GetComponent<ArenaPickup>();
    }

    public ArenaPickupAIHintType GetHintType()
    {
        return hintType;
    }

    public float GetPriority()
    {
        return priority;
    }

    public bool CanBePickedByAlly()
    {
        if (pickup == null)
        {
            return true;
        }

        return pickup.CanBePickedByAlly();
    }

    public bool CanBePickedByEnemy()
    {
        if (pickup == null)
        {
            return true;
        }

        return pickup.CanBePickedByEnemy();
    }

    public bool CanBePickedByPlayer()
    {
        if (pickup == null)
        {
            return true;
        }

        return pickup.CanBePickedByPlayer();
    }
}

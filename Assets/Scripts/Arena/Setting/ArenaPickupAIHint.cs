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

    public ArenaPickupAIHintType GetHintType()
    {
        return hintType;
    }

    public float GetPriority()
    {
        return priority;
    }
}

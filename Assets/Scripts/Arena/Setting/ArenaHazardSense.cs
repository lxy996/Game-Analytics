using UnityEngine;

public class ArenaHazardSense : MonoBehaviour
{
    [SerializeField] private float dangerRadius = 2.5f;
    [SerializeField] private bool dangerousToPlayerSide = true;
    [SerializeField] private bool dangerousToEnemySide = true;

    public float GetDangerRadius()
    {
        return dangerRadius;
    }

    public bool IsDangerousFor(bool isPlayerSide)
    {
        if (isPlayerSide)
        {
            return dangerousToPlayerSide;
        }

        return dangerousToEnemySide;
    }
}

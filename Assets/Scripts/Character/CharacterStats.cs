using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeed = 4f;
    public float maxHealth = 100f;
    public float attackDamage = 20f;
    public float baseAttackCooldown = 0.6f;
    public float attackRange = 1.2f;
    public float attackSpeedMultiplier = 1f;

    public float GetAttackCooldown()
    {
        if (attackSpeedMultiplier <= 0f)
        {
            return baseAttackCooldown;
        }

        return baseAttackCooldown / attackSpeedMultiplier;
    }
}

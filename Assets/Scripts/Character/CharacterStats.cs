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

    [Header("Weapon")]
    public WeaponType weaponType = WeaponType.Melee;

    [Header("Melee / Polearm")]
    public float attackRadius = 0.5f;

    [Header("Ranged")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;
    public float projectileLifetime = 2f;

    [Header("Shield")]
    public bool hasShield = false;
    public float guardCooldown = 1.0f;

    [Header("AI")]
    public float dashUseDistance = 6f;
    public float meleeVerticalTolerance = 0.25f;

    [Header("Melee Hit Shape")]
    public int meleeHitSampleCount = 3;

    public float GetAttackCooldown()
    {
        if (attackSpeedMultiplier <= 0f)
        {
            return baseAttackCooldown;
        }

        return baseAttackCooldown / attackSpeedMultiplier;
    }
    public void ApplyWeaponLoadout(WeaponLoadoutData loadout)
    {
        if (loadout == null)
        {
            return;
        }

        weaponType = loadout.weaponType;
        attackDamage = loadout.attackDamage;
        baseAttackCooldown = loadout.baseAttackCooldown;
        attackRange = loadout.attackRange;
        attackRadius = loadout.attackRadius;

        projectilePrefab = loadout.projectilePrefab;
        projectileSpeed = loadout.projectileSpeed;
        projectileLifetime = loadout.projectileLifetime;

        hasShield = loadout.hasShield;
        guardCooldown = loadout.guardCooldown;

        dashUseDistance = loadout.dashUseDistance;
        meleeVerticalTolerance = loadout.meleeVerticalTolerance;
        meleeHitSampleCount = loadout.meleeHitSampleCount;
    }

}

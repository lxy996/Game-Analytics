using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public class WeaponVisualVariant
{
    public TeamVisualColor teamColor = TeamVisualColor.Blue;
    public RuntimeAnimatorController animatorController;
    public Sprite idleSprite;
}

[CreateAssetMenu(fileName = "WeaponLoadout", menuName = "Game/Weapon Loadout")]
public class WeaponLoadoutData : ScriptableObject
{
    public WeaponType weaponType;

    [Header("Visual Fallback")]
    public RuntimeAnimatorController animatorController;
    public Sprite idleSprite;

    [Header("Team Visual Variants")]
    [SerializeField] private List<WeaponVisualVariant> teamVisualVariants = new List<WeaponVisualVariant>();

    [Header("Pickup Visual")]
    public Sprite pickupIcon;

    [Header("Stats")]
    public float attackDamage = 20f;
    public float baseAttackCooldown = 0.6f;
    public float attackRange = 1.2f;
    public float attackRadius = 0.5f;

    [Header("Combat Timing")]
    public float attackWindup = 0.12f;
    public float attackRecovery = 0.05f;

    [Header("Ranged")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;
    public float projectileLifetime = 2f;

    [Header("Shield")]
    public bool hasShield = false;
    public float guardCooldown = 1f;

    [Header("AI")]
    public float stopDistance = 1.2f;
    public float dashUseDistance = 6f;
    public float meleeVerticalTolerance = 0.25f;

    [Header("Melee Hit Shape")]
    public int meleeHitSampleCount = 3;

    public RuntimeAnimatorController GetAnimatorControllerForTeam(TeamVisualColor teamColor)
    {
        int i;

        for (i = 0; i < teamVisualVariants.Count; i++)
        {
            if (teamVisualVariants[i] == null)
            {
                continue;
            }

            if (teamVisualVariants[i].teamColor != teamColor)
            {
                continue;
            }

            if (teamVisualVariants[i].animatorController != null)
            {
                return teamVisualVariants[i].animatorController;
            }
        }

        return animatorController;
    }

    public Sprite GetIdleSpriteForTeam(TeamVisualColor teamColor)
    {
        int i;

        for (i = 0; i < teamVisualVariants.Count; i++)
        {
            if (teamVisualVariants[i] == null)
            {
                continue;
            }

            if (teamVisualVariants[i].teamColor != teamColor)
            {
                continue;
            }

            if (teamVisualVariants[i].idleSprite != null)
            {
                return teamVisualVariants[i].idleSprite;
            }
        }

        return idleSprite;
    }
}

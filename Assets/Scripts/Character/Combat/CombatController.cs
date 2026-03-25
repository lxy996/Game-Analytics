using UnityEngine;
using System.Collections;

public class CombatController : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private Transform attackPoint;
    //[SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private float attackWindup = 0.12f;
    [SerializeField] private float attackRecovery = 0.05f;

    private CharacterStats stats;
    private CharacterMotor motor;
    private Animator animator;

    private float lastAttackTime = -999f;
    private bool isAttacking = false;
    private bool hasOverrideAttackDirection = false;
    private Vector2 overrideAttackDirection = Vector2.right;

    private bool isGuarding = false;
    private float lastGuardTime = -999f;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        motor = GetComponent<CharacterMotor>();
        animator = GetComponent<Animator>();
    }
    void LateUpdate()
    {
        UpdateAttackPointPosition();

    }

    public bool CanAttack()
    {
        if (isAttacking)
        {
            return false;
        }

        if (Time.time >= lastAttackTime + stats.GetAttackCooldown())
        {
            return true;
        }

        return false;
    }

    public void BasicAttack()
    {
        if (!CanAttack())
        {
            return;
        }

        if (attackPoint == null)
        {
            Debug.LogWarning("AttackPoint is missing on " + gameObject.name);
            return;
        }

        lastAttackTime = Time.time;
        StartCoroutine(AttackRoutine());
    }

    // Execute attack process
    private IEnumerator AttackRoutine()
    {
        float scaledWindup;
        float scaledRecovery;

        isAttacking = true;

        if (motor != null)
        {
            motor.SetMovementLocked(true); // Lock the movement when the character is attacking
        }

        // Control the speed of attack animation
        if (animator != null)
        {
            animator.SetFloat("AttackSpeed", stats.attackSpeedMultiplier);
            animator.SetTrigger("Attack");
        }

        scaledWindup = attackWindup;
        scaledRecovery = attackRecovery;

        if (stats.attackSpeedMultiplier > 0f)
        {
            scaledWindup = attackWindup / stats.attackSpeedMultiplier;
            scaledRecovery = attackRecovery / stats.attackSpeedMultiplier;
        }

        yield return new WaitForSeconds(scaledWindup);

        PerformAttackHit();

        yield return new WaitForSeconds(scaledRecovery);

        if (motor != null)
        {
            motor.SetMovementLocked(false);
        }

        isAttacking = false;
    }

    private void PerformAttackHit()
    {
        if (stats.weaponType == WeaponType.Ranged)
        {
            PerformRangedAttack();
        }
        else
        {
            PerformMeleeAttack();
        }

    }

    private void PerformMeleeAttack()
    {
        Vector2 direction;
        Vector2 origin;
        float radius;
        float range;
        float step;
        int sampleCount;
        int i;
        System.Collections.Generic.HashSet<Health> hitTargets;

        direction = GetAttackDirection();

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector2.right;
        }

        origin = transform.position;
        radius = stats.attackRadius;
        range = stats.attackRange;
        sampleCount = Mathf.Max(1, stats.meleeHitSampleCount); // Number of sample circles
        step = range / sampleCount; // Distance between the two circles

        hitTargets = new System.Collections.Generic.HashSet<Health>(); // Avoid repeated health deductions

        for (i = 1; i <= sampleCount; i++)
        {
            Vector2 samplePoint;
            Collider2D[] hits;
            int j;

            samplePoint = origin + direction.normalized * (step * i); // Current location of the sample point
            hits = Physics2D.OverlapCircleAll(samplePoint, radius, targetLayer);

            for (j = 0; j < hits.Length; j++)
            {
                Health targetHealth;

                if (hits[j].gameObject == gameObject)
                {
                    continue;
                }

                targetHealth = hits[j].GetComponent<Health>();

                if (targetHealth == null)
                {
                    continue;
                }

                if (targetHealth.GetIsDead())
                {
                    continue;
                }

                if (hitTargets.Contains(targetHealth))
                {
                    continue;
                }

                hitTargets.Add(targetHealth);
                targetHealth.TakeDamage(stats.attackDamage);
                Debug.Log(gameObject.name + " hit " + hits[j].gameObject.name + " for " + stats.attackDamage);
            }
        }
    }

    private void PerformRangedAttack()
    {
        GameObject projectileObject;
        Projectile projectile;
        Vector2 direction;

        if (stats.projectilePrefab == null)
        {
            Debug.LogWarning("Projectile prefab missing on " + gameObject.name);
            return;
        }

        UpdateAttackPointPosition();

        direction = GetAttackDirection();

        projectileObject = Instantiate(stats.projectilePrefab, attackPoint.position, Quaternion.identity);
        projectile = projectileObject.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Initialize(
                direction,
                stats.projectileSpeed,
                stats.attackDamage,
                stats.projectileLifetime,
                targetLayer
            );
        }
    }

    private void UpdateAttackPointPosition()
    {
        Vector2 facing;
        Vector2 localPos;

        if (attackPoint == null)
        {
            return;
        }

        facing = GetAttackDirection();

        if (facing.sqrMagnitude < 0.01f)
        {
            facing = Vector2.right;
        }

        localPos = facing.normalized * stats.attackRange * 0.5f;
        attackPoint.localPosition = localPos;
    }

    // Calculate the direction of the attack action
    public void PrepareAttackDirection(Transform target)
    {
        Vector2 directionToTarget;
        Vector2 facingDirection;

        if (target == null)
        {
            return;
        }

        directionToTarget = target.position - transform.position;

        if (directionToTarget.sqrMagnitude < 0.01f)
        {
            return;
        }

        if (stats.weaponType == WeaponType.Ranged)
        {
            facingDirection = directionToTarget.normalized;
            SetOverrideAttackDirection(facingDirection);
            if (motor != null)
            {
                motor.SetFacingDirection(facingDirection);
            }
        }
        else
        {
            if (directionToTarget.x >= 0f)
            {
                facingDirection = Vector2.right;
            }
            else
            {
                facingDirection = Vector2.left;
            }

            SetOverrideAttackDirection(facingDirection);

            if (motor != null)
            {
                motor.SetFacingDirection(facingDirection);
            }
        }
    }


    private Vector2 GetAttackDirection()
    {
        Vector2 direction;

        if (hasOverrideAttackDirection)
        {
            direction = overrideAttackDirection;
        }
        else
        {
            direction = motor.GetFacingDirection();
        }

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector2.right;
        }

        return direction.normalized;
    }

    // Used for player to set ranged attack direction
    public void SetOverrideAttackDirection(Vector2 newDirection)
    {
        if (newDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        overrideAttackDirection = newDirection.normalized;
        hasOverrideAttackDirection = true;
    }

    public void ClearOverrideAttackDirection()
    {
        hasOverrideAttackDirection = false;
    }

    public bool CanGuard()
    {
        if (!stats.hasShield)
        {
            return false;
        }


        if (Time.time >= lastGuardTime + stats.guardCooldown)
        {
            return true;
        }

        return false;
    }

    public void StartGuard()
    {
        if (!CanGuard())
        {
            return;
        }

        isGuarding = true;

        if (motor != null)
        {
            motor.SetMovementLocked(true);
        }

        if (animator != null)
        {
            animator.SetBool("IsGuarding", isGuarding);
        }
    }

    public void EndGuard()
    {
        if (!isGuarding)
        {
            return;
        }

        isGuarding = false;

        if (motor != null)
        {
            motor.SetMovementLocked(false);
        }

        if (animator != null)
        {
            animator.SetBool("IsGuarding", isGuarding);
        }
    }

    // Block the attack from enemy
    public bool TryBlockHit()
    {
        if (!isGuarding)
        {
            return false;
        }

        isGuarding = false;

        if (motor != null)
        {
            motor.SetMovementLocked(false);
        }

        if (animator != null)
        {
            animator.SetBool("IsGuarding", isGuarding);
        }

        lastGuardTime = Time.time;
        return true;
    }

    public bool GetIsGuarding()
    {
        return isGuarding;
    }
    public LayerMask GetTargetLayer()
    {
        return targetLayer;
    }
    public void ApplyWeaponLoadout(WeaponLoadoutData loadout)
    {
        if (loadout == null)
        {
            return;
        }

        attackWindup = loadout.attackWindup;
        attackRecovery = loadout.attackRecovery;
    }

    void OnDrawGizmosSelected()
    {
        CharacterStats debugStats;

        if (attackPoint == null)
        {
            return;
        }

        debugStats = GetComponent<CharacterStats>();

        Gizmos.color = Color.red;

        if (debugStats != null)
        {
            Gizmos.DrawWireSphere(attackPoint.position, debugStats.attackRadius);
        }
        else
        {
            Gizmos.DrawWireSphere(attackPoint.position, 0.5f);
        }
    }
}

using UnityEngine;
using System.Collections;

public class CombatController : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private float attackWindup = 0.12f;
    [SerializeField] private float attackRecovery = 0.05f;

    private CharacterStats stats;
    private CharacterMotor motor;
    private Animator animator;
    private float lastAttackTime = -999f;
    private bool isAttacking = false;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        motor = GetComponent<CharacterMotor>();
        animator = GetComponent<Animator>();
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
        Collider2D[] hits;
        int i;
        Collider2D hit;
        Health targetHealth;

        UpdateAttackPointPosition();

        hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRadius,
            targetLayer
        );

        for (i = 0; i < hits.Length; i++)
        {
            hit = hits[i];

            if (hit.gameObject == gameObject)
            {
                continue;
            }

            targetHealth = hit.GetComponent<Health>();

            if (targetHealth != null)
            {
                if (!targetHealth.GetIsDead())
                {
                    targetHealth.TakeDamage(stats.attackDamage);
                    Debug.Log(gameObject.name + " hit " + hit.gameObject.name + " for " + stats.attackDamage);
                }
            }
        }
    }

    void LateUpdate()
    {
        UpdateAttackPointPosition();

    }
    private void UpdateAttackPointPosition()
    {
        Vector2 facing;
        Vector2 localPos;

        if (attackPoint == null)
        {
            return;
        }

        facing = motor.GetFacingDirection();

        if (facing.sqrMagnitude < 0.01f)
        {
            facing = Vector2.right;
        }

        localPos = facing.normalized * stats.attackRange * 0.5f;
        attackPoint.localPosition = localPos;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}

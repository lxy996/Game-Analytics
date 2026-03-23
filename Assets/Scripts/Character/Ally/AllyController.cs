using UnityEngine;

[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CombatController))]
[RequireComponent(typeof(Health))]
public class AllyController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float targetSearchRadius = 20f;

    private CharacterMotor motor;
    private CombatController combat;
    private SkillController skills;
    private Health health;

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        combat = GetComponent<CombatController>();
        health = GetComponent<Health>();
        skills = GetComponent<SkillController>();
    }

    void Update()
    {
        Vector2 toTarget;
        float distance;
        Vector2 moveDirection;

        if (health.GetIsDead())
        {
            motor.SetMoveInput(Vector2.zero);
            return;
        }

        if (TargetIsInvalid())
        {
            target = FindNearestEnemy();
        }

        if (target == null)
        {
            motor.SetMoveInput(Vector2.zero);
            return;
        }

        toTarget = target.position - transform.position;
        distance = toTarget.magnitude;

        if (distance > stopDistance)
        {
            moveDirection = toTarget.normalized;
            motor.SetMoveInput(moveDirection);

            if (skills != null)
            {
                if (distance > 6f && skills.CanUseDash())
                {
                    skills.UseDash();
                }
            }

        }
        else
        {
            motor.SetMoveInput(Vector2.zero);
            combat.PrepareAttackDirection(target);

            if (combat.CanAttack())
            {
                combat.BasicAttack();
            }
        }
    }

    private bool TargetIsInvalid()
    {
        Health targetHealth;

        if (target == null)
        {
            return true;
        }

        targetHealth = target.GetComponent<Health>();

        if (targetHealth == null)
        {
            return true;
        }

        if (targetHealth.GetIsDead())
        {
            return true;
        }

        return false;
    }

    // The functions are the same as those in the Enemy controller.
    private Transform FindNearestEnemy()
    {
        GameObject[] enemies;
        Transform nearestTarget;
        float nearestDistance;
        float currentDistance;
        int i;

        nearestTarget = null;
        nearestDistance = Mathf.Infinity;

        enemies = GameObject.FindGameObjectsWithTag("Enemy");

        for (i = 0; i < enemies.Length; i++)
        {
            currentDistance = Vector2.Distance(transform.position, enemies[i].transform.position);

            if (currentDistance < nearestDistance && currentDistance <= targetSearchRadius)
            {
                nearestDistance = currentDistance;
                nearestTarget = enemies[i].transform;
            }
        }

        return nearestTarget;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    public Transform GetCurrentTarget()
    {
        return target;
    }

    public void ApplyWeaponLoadout(WeaponLoadoutData loadout)
    {
        if (loadout == null)
        {
            return;
        }

        stopDistance = loadout.stopDistance;
    }

}

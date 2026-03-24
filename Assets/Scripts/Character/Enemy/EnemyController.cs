using UnityEngine;

[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CombatController))]
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float targetSearchRadius = 20f;

    private CharacterMotor motor;
    private CombatController combat;
    private SkillController skills;
    private Health health;
    private CharacterStats stats;

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        combat = GetComponent<CombatController>();
        health = GetComponent<Health>();
        skills = GetComponent<SkillController>();
        stats = GetComponent<CharacterStats>();
    }

    void Update()
    {
        Vector2 toTarget;
        Vector2 moveDirection;

        if (health.GetIsDead())
        {
            motor.SetMoveInput(Vector2.zero);
            return;
        }

        if (TargetIsInvalid())
        {
            target = FindNearestTarget();
        }

        if (target == null)
        {
            motor.SetMoveInput(Vector2.zero);
            return;
        }

        toTarget = target.position - transform.position;
        if (ShouldMoveToTarget(toTarget))
        {
            moveDirection = toTarget.normalized;
            motor.SetMoveInput(moveDirection);

            if (skills != null)
            {
                if (toTarget.magnitude > stats.dashUseDistance && skills.CanUseDash())
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

    // Used to find nearest hostile target
    private Transform FindNearestTarget()
    {
        GameObject[] players;
        GameObject[] teammates;
        Transform nearestTarget;
        float nearestDistance;
        float currentDistance;
        int i;

        nearestTarget = null;
        nearestDistance = Mathf.Infinity;

        // Find the nearest hostile target
        players = GameObject.FindGameObjectsWithTag("Player");
        for (i = 0; i < players.Length; i++)
        {
            currentDistance = Vector2.Distance(transform.position, players[i].transform.position);

            if (currentDistance < nearestDistance && currentDistance <= targetSearchRadius)
            {
                nearestDistance = currentDistance;
                nearestTarget = players[i].transform;
            }
        }

        teammates = GameObject.FindGameObjectsWithTag("Teammate");
        for (i = 0; i < teammates.Length; i++)
        {
            currentDistance = Vector2.Distance(transform.position, teammates[i].transform.position);

            if (currentDistance < nearestDistance && currentDistance <= targetSearchRadius)
            {
                nearestDistance = currentDistance;
                nearestTarget = teammates[i].transform;
            }
        }

        return nearestTarget;
    }

    // Used to calculate whether the weapon can hit the target; if not, move towards the target.
    private bool ShouldMoveToTarget(Vector2 toTarget)
    {
        float absX;
        float absY;
        float attackReach;

        absX = Mathf.Abs(toTarget.x);
        absY = Mathf.Abs(toTarget.y);

        if (stats == null)
        {
            return true;
        }

        if (stats.weaponType == WeaponType.Ranged)
        {
            return toTarget.magnitude > stopDistance;
        }

        attackReach = stats.attackRange + stats.attackRadius;

        if (absX > attackReach)
        {
            return true;
        }

        if (absY > stats.meleeVerticalTolerance)
        {
            return true;
        }

        return false;
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

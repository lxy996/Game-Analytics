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
            target = FindNearestTarget();
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

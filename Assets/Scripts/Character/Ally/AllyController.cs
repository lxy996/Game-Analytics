using UnityEngine;

[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CombatController))]
[RequireComponent(typeof(Health))]
public class AllyController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform followTarget;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float targetSearchRadius = 20f;

    private CharacterMotor motor;
    private CombatController combat;
    private SkillController skills;
    private Health health;
    private CharacterStats stats;
    private AllyCommandMode commandMode = AllyCommandMode.AutoCombat;

    private float guardEndTime = -1f;

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
        if (health.GetIsDead())
        {
            motor.SetMoveInput(Vector2.zero);
            return;
        }

        if (HandleGuardState())
        {
            return;
        }

        if (HandleHazardAvoidance())
        {
            return;
        }

        if (HandlePickupSeek())
        {
            return;
        }

        if (commandMode == AllyCommandMode.FollowPlayer)
        {
            UpdateFollowPlayer();
            return;
        }

        if (commandMode == AllyCommandMode.FocusTarget)
        {
            UpdateFocusTarget();
            return;
        }

        UpdateAutoCombat();
    }

    private bool HandleGuardState()
    {
        if (combat == null)
        {
            return false;
        }

        if (guardEndTime < 0f)
        {
            return false;
        }

        if (Time.time <= guardEndTime)
        {
            motor.SetMoveInput(Vector2.zero);

            if (target != null)
            {
                combat.PrepareAttackDirection(target);
            }

            return true;
        }

        if (combat.GetIsGuarding())
        {
            combat.EndGuard();
        }

        guardEndTime = -1f;
        return false;
    }

    private bool HandleHazardAvoidance()
    {
        ArenaHazardSense[] hazards;
        ArenaHazardSense closestHazard;
        float closestDistance;
        int i;

        hazards = Object.FindObjectsByType<ArenaHazardSense>(FindObjectsSortMode.None);
        closestHazard = null;
        closestDistance = Mathf.Infinity;

        for (i = 0; i < hazards.Length; i++)
        {
            float distance;

            if (hazards[i] == null)
            {
                continue;
            }

            if (!hazards[i].IsDangerousFor(true))
            {
                continue;
            }

            distance = Vector2.Distance(transform.position, hazards[i].transform.position);

            if (distance > hazards[i].GetDangerRadius() + 2f)
            {
                continue;
            }

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHazard = hazards[i];
            }
        }

        if (closestHazard == null)
        {
            return false;
        }

        motor.SetMoveInput((transform.position - closestHazard.transform.position).normalized);
        return true;
    }

    private bool HandlePickupSeek()
    {
        ArenaPickupAIHint[] pickups;
        ArenaPickupAIHint bestPickup;
        float bestScore;
        int i;

        if (GetHealthRatio() >= 0.65f)
        {
            return false;
        }

        pickups = Object.FindObjectsByType<ArenaPickupAIHint>(FindObjectsSortMode.None);
        bestPickup = null;
        bestScore = -999f;

        for (i = 0; i < pickups.Length; i++)
        {
            float distance;
            float score;

            if (pickups[i] == null)
            {
                continue;
            }

            if (pickups[i].GetHintType() != ArenaPickupAIHintType.Heal)
            {
                continue;
            }

            distance = Vector2.Distance(transform.position, pickups[i].transform.position);

            if (distance > 6f)
            {
                continue;
            }

            score = pickups[i].GetPriority() - distance * 0.15f;

            if (score > bestScore)
            {
                bestScore = score;
                bestPickup = pickups[i];
            }
        }

        if (bestPickup == null)
        {
            return false;
        }

        motor.SetMoveInput((bestPickup.transform.position - transform.position).normalized);
        return true;
    }

    private float GetHealthRatio()
    {
        if (health == null || stats == null || stats.maxHealth <= 0f)
        {
            return 1f;
        }

        return health.GetCurrentHealth() / stats.maxHealth;
    }

    // Auto Combat
    private void UpdateAutoCombat()
    {
        Vector2 toTarget;
        Vector2 moveDirection;

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

        if (ShouldStartGuard(toTarget))
        {
            StartTimedGuard();
            return;
        }

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

    // Focus fire on the selected target
    private void UpdateFocusTarget()
    {
        Vector2 toTarget;
        Vector2 moveDirection;

        if (TargetIsInvalid())
        {
            commandMode = AllyCommandMode.AutoCombat;
            target = FindNearestEnemy();
            return;
        }

        toTarget = target.position - transform.position;

        if (ShouldStartGuard(toTarget))
        {
            StartTimedGuard();
            return;
        }

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

    // Follow the player
    private void UpdateFollowPlayer()
    {
        Vector2 toFollow;
        float followDistance = 1.5f;

        if (followTarget == null)
        {
            motor.SetMoveInput(Vector2.zero);
            return;
        }

        toFollow = followTarget.position - transform.position;

        if (toFollow.magnitude > followDistance)
        {
            motor.SetMoveInput(toFollow.normalized);
        }
        else
        {
            motor.SetMoveInput(Vector2.zero);
        }
    }

    private bool ShouldStartGuard(Vector2 toTarget)
    {
        if (stats == null || combat == null)
        {
            return false;
        }

        if (!stats.hasShield)
        {
            return false;
        }

        if (!combat.CanGuard())
        {
            return false;
        }

        if (toTarget.magnitude > 1.6f)
        {
            return false;
        }

        if (GetHealthRatio() <= 0.55f)
        {
            return true;
        }

        if (Random.value <= 0.25f)
        {
            return true;
        }

        return false;
    }

    private void StartTimedGuard()
    {
        if (combat == null)
        {
            return;
        }

        combat.StartGuard();
        guardEndTime = Time.time + 0.75f;
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

    public void SetFollowTarget(Transform newFollowTarget)
    {
        followTarget = newFollowTarget;
    }

    public void SetCommandMode(AllyCommandMode newMode)
    {
        commandMode = newMode;
    }

    public AllyCommandMode GetCommandMode()
    {
        return commandMode;
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


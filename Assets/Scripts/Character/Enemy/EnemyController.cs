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

    private EnemyAIProfileData aiProfile;
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
        Vector2 toTarget;
        Vector2 moveDirection;

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

        if (TargetIsInvalid())
        {
            target = FindBestTarget();
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

        if (ShouldRetreatFromTarget(toTarget))
        {
            moveDirection = (-toTarget).normalized;
            motor.SetMoveInput(moveDirection);
            combat.PrepareAttackDirection(target);
            return;
        }

        if (ShouldMoveToTarget(toTarget))
        {
            moveDirection = toTarget.normalized;
            motor.SetMoveInput(moveDirection);

            if (skills != null && stats != null)
            {
                if (toTarget.magnitude > GetDashDistanceThreshold() && skills.CanUseDash())
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

            if (!hazards[i].IsDangerousFor(false))
            {
                continue;
            }

            distance = Vector2.Distance(transform.position, hazards[i].transform.position);

            if (distance > hazards[i].GetDangerRadius() + GetHazardAvoidPadding())
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

        if (health == null || stats == null)
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

            distance = Vector2.Distance(transform.position, pickups[i].transform.position);

            if (distance > GetPickupSearchRadius())
            {
                continue;
            }

            score = pickups[i].GetPriority() - distance * 0.15f;

            if (pickups[i].GetHintType() == ArenaPickupAIHintType.Heal)
            {
                if (GetHealthRatio() < 0.6f)
                {
                    score = score + 5f;
                }
                else
                {
                    score = score - 3f;
                }
            }

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

    private bool ShouldStartGuard(Vector2 toTarget)
    {
        if (aiProfile == null || combat == null || stats == null)
        {
            return false;
        }

        if (!aiProfile.enableAutoGuard)
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

        if (toTarget.magnitude > aiProfile.guardEnterDistance)
        {
            return false;
        }

        if (GetHealthRatio() <= aiProfile.lowHealthGuardThreshold)
        {
            return true;
        }

        if (aiProfile.tacticStyle == EnemyTacticStyle.IronWall)
        {
            return true;
        }

        if (Random.value <= aiProfile.guardChance)
        {
            return true;
        }

        return false;
    }

    private void StartTimedGuard()
    {
        if (combat == null || aiProfile == null)
        {
            return;
        }

        combat.StartGuard();
        guardEndTime = Time.time + aiProfile.guardHoldTime;
    }

    private bool ShouldRetreatFromTarget(Vector2 toTarget)
    {
        if (aiProfile == null || stats == null)
        {
            return false;
        }

        if (stats.weaponType != WeaponType.Ranged)
        {
            return false;
        }

        if (aiProfile.tacticStyle != EnemyTacticStyle.Sharpshooter)
        {
            return false;
        }

        if (toTarget.magnitude < aiProfile.retreatDistance)
        {
            return true;
        }

        return false;
    }

    private float GetDashDistanceThreshold()
    {
        if (aiProfile == null || stats == null)
        {
            return stats.dashUseDistance;
        }

        return stats.dashUseDistance * aiProfile.dashAggressionMultiplier;
    }

    private float GetPickupSearchRadius()
    {
        if (aiProfile == null)
        {
            return 5f;
        }

        return aiProfile.pickupSearchRadius;
    }

    private float GetHazardAvoidPadding()
    {
        if (aiProfile == null)
        {
            return 2f;
        }

        return aiProfile.hazardAvoidDistance;
    }

    private float GetHealthRatio()
    {
        if (health == null || stats == null || stats.maxHealth <= 0f)
        {
            return 1f;
        }

        return health.GetCurrentHealth() / stats.maxHealth;
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

    private Transform FindBestTarget()
    {
        GameObject[] players;
        GameObject[] teammates;
        Transform nearestTarget;
        float nearestDistance;
        float currentDistance;
        int i;

        nearestTarget = null;
        nearestDistance = Mathf.Infinity;

        players = GameObject.FindGameObjectsWithTag("Player");

        if (aiProfile != null && aiProfile.prioritizePlayerCharacter)
        {
            for (i = 0; i < players.Length; i++)
            {
                currentDistance = Vector2.Distance(transform.position, players[i].transform.position);

                if (currentDistance < nearestDistance && currentDistance <= targetSearchRadius)
                {
                    nearestDistance = currentDistance;
                    nearestTarget = players[i].transform;
                }
            }

            if (nearestTarget != null)
            {
                return nearestTarget;
            }
        }

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
        float rangedStopDistance;

        absX = Mathf.Abs(toTarget.x);
        absY = Mathf.Abs(toTarget.y);

        if (stats == null)
        {
            return true;
        }

        if (stats.weaponType == WeaponType.Ranged)
        {
            rangedStopDistance = stopDistance;

            if (aiProfile != null && aiProfile.tacticStyle == EnemyTacticStyle.Sharpshooter)
            {
                rangedStopDistance = aiProfile.rangedPreferredDistance;
            }

            return toTarget.magnitude > rangedStopDistance;
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

    public void ApplyAIProfile(EnemyAIProfileData profile)
    {
        aiProfile = profile;
    }
}


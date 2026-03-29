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

    private float avoidHazardUntil = -1f;
    private Vector2 avoidHazardDirection = Vector2.zero;

    private float nextPickupSearchTime = 0f;
    private Transform cachedPickupTarget;

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
        ArenaHazardSense hazard;
        float distance;

        hazard = FindClosestDangerousHazard();

        if (hazard != null)
        {
            distance = Vector2.Distance(transform.position, hazard.transform.position);

            if (distance <= hazard.GetDangerRadius() + 2f)
            {
                avoidHazardDirection = ((Vector2)(transform.position - hazard.transform.position)).normalized;

                if (avoidHazardDirection.sqrMagnitude < 0.01f)
                {
                    avoidHazardDirection = Random.insideUnitCircle.normalized;
                }

                avoidHazardUntil = Time.time + 0.45f;
            }
        }

        if (Time.time < avoidHazardUntil)
        {
            motor.SetMoveInput(avoidHazardDirection);
            return true;
        }

        return false;
    }

    private bool HandlePickupSeek()
    {
        ArenaPickupAIHint bestPickup;
        Vector2 toPickup;

        if (HasImmediateCombatPressure())
        {
            cachedPickupTarget = null;
            return false;
        }

        if (GetHealthRatio() > 0.65f)
        {
            cachedPickupTarget = null;
            return false;
        }

        if (Time.time >= nextPickupSearchTime || cachedPickupTarget == null)
        {
            bestPickup = FindBestPickupForAlly();
            cachedPickupTarget = bestPickup != null ? bestPickup.transform : null;
            nextPickupSearchTime = Time.time + 0.35f;
        }

        if (cachedPickupTarget == null)
        {
            return false;
        }

        toPickup = cachedPickupTarget.position - transform.position;

        if (toPickup.sqrMagnitude < 0.04f)
        {
            return false;
        }

        motor.SetMoveInput(toPickup.normalized);
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
            moveDirection = GetApproachMoveDirection(toTarget);
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
            moveDirection = GetApproachMoveDirection(toTarget);
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

    private Vector2 GetApproachMoveDirection(Vector2 toTarget)
    {
        if (stats == null)
        {
            return toTarget.normalized;
        }

        if (stats.weaponType == WeaponType.Ranged)
        {
            return toTarget.normalized;
        }

        if (Mathf.Abs(toTarget.y) > stats.meleeVerticalTolerance)
        {
            return new Vector2(0f, Mathf.Sign(toTarget.y));
        }

        return new Vector2(Mathf.Sign(toTarget.x), 0f);
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

    private ArenaHazardSense FindClosestDangerousHazard()
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

        return closestHazard;
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

    private bool HasImmediateCombatPressure()
    {
        if (!TargetIsInvalid())
        {
            if (Vector2.Distance(transform.position, target.position) <= 2.2f)
            {
                return true;
            }
        }

        return false;
    }

    private ArenaPickupAIHint FindBestPickupForAlly()
    {
        ArenaPickupAIHint[] pickups;
        ArenaPickupAIHint bestPickup;
        float bestScore;
        int i;

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

            if (!pickups[i].CanBePickedByAlly())
            {
                continue;
            }

            distance = Vector2.Distance(transform.position, pickups[i].transform.position);

            if (distance > 6f)
            {
                continue;
            }

            score = pickups[i].GetPriority() - distance * 0.18f;

            if (pickups[i].GetHintType() == ArenaPickupAIHintType.Heal)
            {
                score = score + 6f;
            }
            else
            {
                score = score - 2f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPickup = pickups[i];
            }
        }

        return bestPickup;
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


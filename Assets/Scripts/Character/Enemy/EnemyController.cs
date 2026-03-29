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

    private float avoidHazardUntil = -1f;
    private Vector2 avoidHazardDirection = Vector2.zero;

    private float nextPickupSearchTime = 0f;
    private Transform cachedPickupTarget;

    private Vector3 spawnOrigin;

    void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        combat = GetComponent<CombatController>();
        health = GetComponent<Health>();
        skills = GetComponent<SkillController>();
        stats = GetComponent<CharacterStats>();
        spawnOrigin = transform.position;
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

        if (ShouldReturnToFormation())
        {
            motor.SetMoveInput((spawnOrigin - transform.position).normalized);
            return;
        }

        if (ShouldReturnNearShieldAnchor())
        {
            return;
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

            if (skills != null && skills.CanUseDash())
            {
                skills.UseDash();
            }

            return;
        }

        if (ShouldDashToBackline(toTarget))
        {
            combat.PrepareAttackDirection(target);

            if (skills != null && skills.CanUseDash())
            {
                skills.UseDash();
            }
        }

        if (ShouldMoveToTarget(toTarget))
        {
            moveDirection = GetApproachMoveDirection(toTarget);
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
        ArenaHazardSense hazard;
        float distance;

        hazard = FindClosestDangerousHazard();

        if (hazard != null)
        {
            distance = Vector2.Distance(transform.position, hazard.transform.position);

            if (distance <= hazard.GetDangerRadius() + GetHazardAvoidPadding())
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

        return closestHazard;
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

        if (Time.time >= nextPickupSearchTime || cachedPickupTarget == null)
        {
            bestPickup = FindBestPickupForEnemy();
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

    private bool HasImmediateCombatPressure()
    {
        if (!TargetIsInvalid())
        {
            if (Vector2.Distance(transform.position, target.position) <= 3.2f)
            {
                return true;
            }
        }

        return false;
    }

    private ArenaPickupAIHint FindBestPickupForEnemy()
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

            if (!pickups[i].CanBePickedByEnemy())
            {
                continue;
            }

            distance = Vector2.Distance(transform.position, pickups[i].transform.position);

            if (distance > GetPickupSearchRadius())
            {
                continue;
            }

            score = pickups[i].GetPriority() - distance * 0.18f;

            if (pickups[i].GetHintType() == ArenaPickupAIHintType.Heal)
            {
                if (GetHealthRatio() < 0.5f)
                {
                    score = score + 6f;
                }
                else
                {
                    score = score - 4f;
                }
            }
            else
            {
                if (!TargetIsInvalid())
                {
                    score = score - 2.5f;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPickup = pickups[i];
            }
        }

        return bestPickup;
    }

    private bool ShouldReturnToFormation()
    {
        if (aiProfile == null)
        {
            return false;
        }

        if (!aiProfile.stayNearSpawn)
        {
            return false;
        }

        if (Vector2.Distance(transform.position, spawnOrigin) > aiProfile.maxRoamDistanceFromSpawn)
        {
            return true;
        }

        return false;
    }

    private bool ShouldReturnNearShieldAnchor()
    {
        EnemyController anchorShield;
        float distance;

        if (aiProfile == null || stats == null)
        {
            return false;
        }

        if (!aiProfile.polearmStayNearShield)
        {
            return false;
        }

        if (stats.weaponType != WeaponType.Polearm)
        {
            return false;
        }

        anchorShield = FindNearestShieldAlly();

        if (anchorShield == null)
        {
            return false;
        }

        distance = Vector2.Distance(transform.position, anchorShield.transform.position);

        if (distance <= aiProfile.maxDistanceFromShieldAnchor)
        {
            return false;
        }

        motor.SetMoveInput((anchorShield.transform.position - transform.position).normalized);
        return true;
    }

    private EnemyController FindNearestShieldAlly()
    {
        EnemyController[] allies;
        EnemyController best;
        float bestDistance;
        int i;

        allies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        best = null;
        bestDistance = Mathf.Infinity;

        for (i = 0; i < allies.Length; i++)
        {
            CharacterStats allyStats;
            float distance;

            if (allies[i] == null || allies[i] == this)
            {
                continue;
            }

            allyStats = allies[i].GetComponent<CharacterStats>();

            if (allyStats == null)
            {
                continue;
            }

            if (allyStats.weaponType != WeaponType.Melee || !allyStats.hasShield)
            {
                continue;
            }

            distance = Vector2.Distance(transform.position, allies[i].transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = allies[i];
            }
        }

        return best;
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

    private bool ShouldDashToBackline(Vector2 toTarget)
    {
        if (aiProfile == null || stats == null || skills == null)
        {
            return false;
        }

        if (!aiProfile.aggressiveDashToBackline)
        {
            return false;
        }

        if (stats.weaponType != WeaponType.Melee || !stats.hasShield)
        {
            return false;
        }

        if (!skills.CanUseDash())
        {
            return false;
        }

        if (TargetIsBacklineTarget(target) && toTarget.magnitude > 2.2f)
        {
            return true;
        }

        return false;
    }

    private bool TargetIsBacklineTarget(Transform candidate)
    {
        CharacterStats targetStats;

        if (candidate == null)
        {
            return false;
        }

        targetStats = candidate.GetComponent<CharacterStats>();

        if (targetStats == null)
        {
            return false;
        }

        if (targetStats.weaponType == WeaponType.Ranged)
        {
            return true;
        }

        if (targetStats.weaponType == WeaponType.Polearm)
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
        if (health == null || stats == null || stats.GetEffectiveMaxHealth() <= 0f)
        {
            return 1f;
        }

        return health.GetCurrentHealth() / stats.GetEffectiveMaxHealth();
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
        Transform bestTarget;
        float bestScore;
        int i;

        players = GameObject.FindGameObjectsWithTag("Player");
        teammates = GameObject.FindGameObjectsWithTag("Teammate");

        bestTarget = null;
        bestScore = -999f;

        for (i = 0; i < players.Length; i++)
        {
            EvaluateTarget(players[i].transform, ref bestTarget, ref bestScore);
        }

        for (i = 0; i < teammates.Length; i++)
        {
            EvaluateTarget(teammates[i].transform, ref bestTarget, ref bestScore);
        }

        return bestTarget;
    }

    private void EvaluateTarget(Transform candidate, ref Transform bestTarget, ref float bestScore)
    {
        CharacterStats targetStats;
        Health targetHealth;
        float distance;
        float score;

        if (candidate == null)
        {
            return;
        }

        targetHealth = candidate.GetComponent<Health>();

        if (targetHealth == null || targetHealth.GetIsDead())
        {
            return;
        }

        distance = Vector2.Distance(transform.position, candidate.position);

        if (distance > targetSearchRadius)
        {
            return;
        }

        score = -distance;

        if (aiProfile != null)
        {
            if (aiProfile.prioritizePlayerCharacter && candidate.CompareTag("Player"))
            {
                score = score + 4f;
            }

            if (aiProfile.focusFireLowestHealth)
            {
                score = score + (1f - GetTargetHealthRatio(candidate)) * 5f;
            }

            if (aiProfile.tacticStyle == EnemyTacticStyle.Shadow)
            {
                targetStats = candidate.GetComponent<CharacterStats>();

                if (targetStats != null)
                {
                    if (targetStats.weaponType == WeaponType.Ranged)
                    {
                        score = score + 6f;
                    }
                    else if (targetStats.weaponType == WeaponType.Polearm)
                    {
                        score = score + 3f;
                    }
                }
            }
        }

        if (score > bestScore)
        {
            bestScore = score;
            bestTarget = candidate;
        }
    }

    private float GetTargetHealthRatio(Transform candidate)
    {
        Health h;
        CharacterStats s;

        h = candidate.GetComponent<Health>();
        s = candidate.GetComponent<CharacterStats>();

        if (h == null || s == null || s.GetEffectiveMaxHealth() <= 0f)
        {
            return 1f;
        }

        return h.GetCurrentHealth() / s.GetEffectiveMaxHealth();
    }

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

